using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 战斗场景管理器：管理站位点、生成Spine角色、血条绑定
/// 挂在战斗场景的根节点上
/// </summary>
public class BattleStageManager : MonoBehaviour
{
    #region 单例

    private static BattleStageManager _instance;
    public static BattleStageManager Instance => _instance;

    #endregion

    #region 序列化字段

    [Header("站位点")]
    [Tooltip("玩家方站位点（最多9个）")]
    [SerializeField] private Transform[] playerSlots;

    [Tooltip("敌方站位点（最多9个）")]
    [SerializeField] private Transform[] enemySlots;

    [Header("血条")]
    [Tooltip("血条预制体")]
    [SerializeField] private GameObject healthBarPrefab;

    [Tooltip("血条在角色头顶的偏移")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0, 2.2f, 0);

    [Header("目标选中标签")]
    [Tooltip("角色下方的选中标签节点（不拖则自动查找子节点 TargetSelectionIndicator）")]
    [SerializeField] private GameObject targetIndicator;

    [Header("点击检测")]
    [Tooltip("角色点击检测预制体（含 Collider2D），不拖则运行时动态创建 HitArea 子节点")]
    [SerializeField] private GameObject hitAreaPrefab;

    #endregion

    #region 运行时数据

    /// <summary> 已生成的角色GameObject（key=槽位索引） </summary>
    private readonly Dictionary<int, GameObject> _playerUnits = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, GameObject> _enemyUnits = new Dictionary<int, GameObject>();

    /// <summary> 血条组件（key=槽位索引） </summary>
    private readonly Dictionary<int, BattleHealthBar> _playerHealthBars = new Dictionary<int, BattleHealthBar>();
    private readonly Dictionary<int, BattleHealthBar> _enemyHealthBars = new Dictionary<int, BattleHealthBar>();

    /// <summary> 当前行动单位指示箭头 </summary>
    private GameObject _currentIndicator;

    /// <summary> 目标选中标签（场景预置节点，运行时移动/显示/隐藏） </summary>
    private GameObject _targetIndicator;

    /// <summary> 资源加载句柄 </summary>
    private readonly List<AsyncOperationHandle> _loadHandles = new List<AsyncOperationHandle>();

    #endregion

    #region 生命周期

    private void Awake()
    {
        _instance = this;
        AutoFindSlots();

        // 表现层总控（技能释放演出序列）
        if (GetComponent<BattlePresenter>() == null)
            gameObject.AddComponent<BattlePresenter>();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        ReleaseAll();
    }

    #endregion

    #region 初始化

    /// <summary>
    /// 自动查找站位点（如果Inspector未手动赋值）
    /// </summary>
    private void AutoFindSlots()
    {
        if (playerSlots == null || playerSlots.Length == 0)
        {
            var root = transform.Find("PlayerSlots");
            if (root != null)
            {
                playerSlots = new Transform[root.childCount];
                for (int i = 0; i < root.childCount; i++)
                    playerSlots[i] = root.GetChild(i);
            }
        }

        if (enemySlots == null || enemySlots.Length == 0)
        {
            var root = transform.Find("EnemySlots");
            if (root != null)
            {
                enemySlots = new Transform[root.childCount];
                for (int i = 0; i < root.childCount; i++)
                    enemySlots[i] = root.GetChild(i);
            }
        }
    }

    /// <summary>
    /// 根据战斗单位列表生成角色（清空后重建）
    /// </summary>
    public void SpawnUnits(List<BattleUnit> units, System.Action onComplete = null)
    {
        ClearAllUnits();
        SpawnUnitsInternal(units, onComplete);
    }

    /// <summary>
    /// 新增角色（不清空现有角色，用于增援）
    /// </summary>
    public void SpawnNewUnits(List<BattleUnit> units, System.Action onComplete = null)
    {
        SpawnUnitsInternal(units, onComplete);
    }

    private void SpawnUnitsInternal(List<BattleUnit> units, System.Action onComplete)
    {
        if (units.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(SpawnAllUnitsCoroutine(units, onComplete));
    }

    private IEnumerator SpawnAllUnitsCoroutine(List<BattleUnit> units, System.Action onComplete)
    {
        int remaining = units.Count;
        foreach (var unit in units)
        {
            if (unit == null || unit.PersonId <= 0)
            {
                remaining--;
                continue;
            }
            StartCoroutine(SpawnUnitCoroutine(unit, () =>
            {
                remaining--;
            }));
        }
        // 等待所有协程完成
        while (remaining > 0)
            yield return null;

        Debug.Log($"[BattleStageManager] 所有角色生成完成");
        onComplete?.Invoke();
    }

    private IEnumerator SpawnUnitCoroutine(BattleUnit unit, System.Action onComplete = null)
    {
        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null)
        {
            Debug.LogWarning("[BattleStageManager] Tables 未加载");
            onComplete?.Invoke();
            yield break;
        }
        var person = tables.TbPerson.Get(unit.PersonId);
        if (person == null)
        {
            Debug.LogWarning($"[BattleStageManager] Person不存在: {unit.PersonId}");
            onComplete?.Invoke();
            yield break;
        }

        // 获取站位点
        Transform slot = GetSlot(unit.IsPlayerSide, unit.SlotIndex);
        if (slot == null)
        {
            Debug.LogWarning($"[BattleStageManager] 站位点不存在: side={unit.IsPlayerSide}, slot={unit.SlotIndex}");
            onComplete?.Invoke();
            yield break;
        }

        // 加载Spine预制体
        string prefabPath = person.Prefab2;
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning($"[BattleStageManager] Person {person.Name} 没有战斗预制体(Prefab2)");
            onComplete?.Invoke();
            yield break;
        }

        GameObject prefab = null;
        bool loaded = false;

        // 尝试Addressables加载
        var handle = Addressables.LoadAssetAsync<GameObject>(prefabPath);
        _loadHandles.Add(handle);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            prefab = handle.Result;
            loaded = true;
        }
        else
        {
#if UNITY_EDITOR
            string assetPath = prefabPath.StartsWith("Assets/") ? prefabPath : "Assets/" + prefabPath;
            prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null) loaded = true;
#endif
        }

        if (!loaded || prefab == null)
        {
            Debug.LogWarning($"[BattleStageManager] 无法加载预制体: {prefabPath}");
            onComplete?.Invoke();
            yield break;
        }

        // 实例化角色
        var go = Instantiate(prefab, slot.position, Quaternion.identity, slot);
        go.name = $"Unit_{unit.PersonId}_{person.Name}";

        // 归零Z坐标，确保在2D平面上
        var pos = go.transform.position;
        pos.z = 0;
        go.transform.position = pos;

        // 统一缩放为合理大小（可根据预制体调整）
        // go.transform.localScale = Vector3.one;

        // 敌方面向左边（翻转）
        if (!unit.IsPlayerSide)
        {
            var scale = go.transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            go.transform.localScale = scale;
        }
        else
        {
            var scale = go.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            go.transform.localScale = scale;
        }

        // 添加点击检测子节点（HitArea），方便单独调整点击区域
        if (hitAreaPrefab != null)
        {
            var hitArea = Instantiate(hitAreaPrefab, go.transform);
            hitArea.name = "HitArea";
        }
        else
        {
            var hitArea = new GameObject("HitArea");
            hitArea.transform.SetParent(go.transform, false);
            var col = hitArea.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 2f);
            col.offset = new Vector2(0, 1f);
        }

        // 移除角色根节点上旧的整体碰撞体，统一交给 HitArea 子节点
        var oldCollider = go.GetComponent<Collider2D>();
        if (oldCollider != null)
            Destroy(oldCollider);

        // 记录
        var dict = unit.IsPlayerSide ? _playerUnits : _enemyUnits;
        dict[unit.SlotIndex] = go;

        // 创建血条
        CreateHealthBar(unit, go.transform);

        Debug.Log($"[BattleStageManager] 生成角色: {person.Name} at slot {unit.SlotIndex} ({(unit.IsPlayerSide ? "Player" : "Enemy")})");
        onComplete?.Invoke();
    }

    #endregion

    #region 血条

    private void CreateHealthBar(BattleUnit unit, Transform unitTransform)
    {
        if (healthBarPrefab == null)
        {
            // 动态创建简单血条
            CreateSimpleHealthBar(unit, unitTransform);
            return;
        }

        var barGo = Instantiate(healthBarPrefab, unitTransform.position + healthBarOffset, Quaternion.identity);
        var bar = barGo.GetComponent<BattleHealthBar>();
        if (bar == null) bar = barGo.AddComponent<BattleHealthBar>();

        bar.Setup(unit, unitTransform, healthBarOffset);

        var dict = unit.IsPlayerSide ? _playerHealthBars : _enemyHealthBars;
        dict[unit.SlotIndex] = bar;
    }

    /// <summary>
    /// 动态创建简单血条（无需预制体）
    /// </summary>
    private void CreateSimpleHealthBar(BattleUnit unit, Transform unitTransform)
    {
        // 创建World Space Canvas
        var canvasGo = new GameObject($"HealthBar_P{unit.PersonId}_S{unit.SlotIndex}");
        canvasGo.transform.SetParent(unitTransform);
        canvasGo.transform.localPosition = healthBarOffset;

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        var canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(120, 15);
        canvasRect.localScale = Vector3.one * 0.01f; // World Space缩放

        // 背景
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // 填充条
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);
        var fillImage = fillGo.AddComponent<UnityEngine.UI.Image>();
        fillImage.color = unit.IsPlayerSide ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.8f, 0.2f, 0.2f);
        fillImage.type = UnityEngine.UI.Image.Type.Filled;
        fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

        // 添加BattleHealthBar组件
        var bar = canvasGo.AddComponent<BattleHealthBar>();
        bar.SetupSimple(unit, fillImage, unitTransform, healthBarOffset);

        var dict = unit.IsPlayerSide ? _playerHealthBars : _enemyHealthBars;
        dict[unit.SlotIndex] = bar;
    }

    /// <summary>
    /// 更新指定单位的血条
    /// </summary>
    public void UpdateHealthBar(BattleUnit unit)
    {
        var dict = unit.IsPlayerSide ? _playerHealthBars : _enemyHealthBars;
        if (dict.TryGetValue(unit.SlotIndex, out var bar))
            bar.UpdateHealth();
    }

    /// <summary>
    /// 更新所有血条
    /// </summary>
    public void UpdateAllHealthBars()
    {
        foreach (var bar in _playerHealthBars.Values) bar?.UpdateHealth();
        foreach (var bar in _enemyHealthBars.Values) bar?.UpdateHealth();
    }

    #endregion

    #region 指示器

    /// <summary>
    /// 高亮当前行动单位
    /// </summary>
    public void HighlightCurrentUnit(BattleUnit unit)
    {
        ClearHighlight();
        if (unit == null) return;

        var dict = unit.IsPlayerSide ? _playerUnits : _enemyUnits;
        if (!dict.TryGetValue(unit.SlotIndex, out var go)) return;

        // 创建简单指示箭头（红色三角形）
        _currentIndicator = new GameObject("CurrentIndicator");
        _currentIndicator.transform.SetParent(go.transform);
        _currentIndicator.transform.localPosition = new Vector3(0, 2f, 0);

        // 用SpriteRenderer画一个三角形
        var sr = _currentIndicator.AddComponent<SpriteRenderer>();
        sr.sprite = CreateTriangleSprite();
        sr.color = Color.red;
        sr.sortingOrder = 20;

        // 添加简单的上下浮动动画
        var rb = _currentIndicator.AddComponent<BattleIndicatorFloat>();
    }

    /// <summary>
    /// 清除高亮
    /// </summary>
    public void ClearHighlight()
    {
        if (_currentIndicator != null)
        {
            Destroy(_currentIndicator);
            _currentIndicator = null;
        }
    }

    /// <summary>
    /// 显示目标选中标签（角色下方绿色标记），用于目标选择反馈
    /// </summary>
    public void ShowTargetSelected(BattleUnit unit)
    {
        ClearTargetSelection();
        if (unit == null) return;

        var dict = unit.IsPlayerSide ? _playerUnits : _enemyUnits;
        if (!dict.TryGetValue(unit.SlotIndex, out var go)) return;

        EnsureTargetIndicator();
        if (_targetIndicator == null) return;

        _targetIndicator.transform.position = go.transform.position + new Vector3(0, -2.4f, 0);

        var sr = _targetIndicator.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (sr.sprite == null)
                sr.sprite = CreateTriangleSprite();
            sr.color = Color.green;
        }

        _targetIndicator.SetActive(true);
    }

    /// <summary>
    /// 清除目标选中标签（仅隐藏，保留场景节点）
    /// </summary>
    public void ClearTargetSelection()
    {
        if (_targetIndicator != null)
            _targetIndicator.SetActive(false);
    }

    /// <summary>
    /// 获取目标选中标签节点：优先序列化字段，其次场景子节点 TargetSelectionIndicator
    /// </summary>
    private void EnsureTargetIndicator()
    {
        if (_targetIndicator != null) return;

        if (targetIndicator != null)
        {
            _targetIndicator = targetIndicator;
            return;
        }

        var found = transform.Find("TargetSelectionIndicator");
        if (found != null)
            _targetIndicator = found.gameObject;
    }

    private Sprite CreateTriangleSprite()
    {
        // 创建简单的三角形纹理
        int size = 32;
        var tex = new Texture2D(size, size);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 倒三角形
                float halfWidth = (float)(size - y) / size * size / 2;
                float center = size / 2f;
                if (x >= center - halfWidth && x <= center + halfWidth)
                    pixels[y * size + x] = Color.white;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0f));
    }

    #endregion

    #region 查询

    public Transform GetSlot(bool isPlayerSide, int slotIndex)
    {
        var slots = isPlayerSide ? playerSlots : enemySlots;
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return null;
        return slots[slotIndex];
    }

    public GameObject GetUnitObject(bool isPlayerSide, int slotIndex)
    {
        var dict = isPlayerSide ? _playerUnits : _enemyUnits;
        return dict.TryGetValue(slotIndex, out var go) ? go : null;
    }

    public BattleHealthBar GetHealthBar(bool isPlayerSide, int slotIndex)
    {
        var dict = isPlayerSide ? _playerHealthBars : _enemyHealthBars;
        return dict.TryGetValue(slotIndex, out var bar) ? bar : null;
    }

    #endregion

    #region 清理

    public void ClearAllUnits()
    {
        foreach (var go in _playerUnits.Values) if (go != null) Destroy(go);
        foreach (var go in _enemyUnits.Values) if (go != null) Destroy(go);
        _playerUnits.Clear();
        _enemyUnits.Clear();
        _playerHealthBars.Clear();
        _enemyHealthBars.Clear();
        ClearHighlight();
        ClearTargetSelection();
    }

    private void ReleaseAll()
    {
        foreach (var handle in _loadHandles)
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
        _loadHandles.Clear();
    }

    #endregion
}

/// <summary>
/// 指示器浮动动画
/// </summary>
public class BattleIndicatorFloat : MonoBehaviour
{
    private float _timer;
    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.localPosition;
    }

    private void Update()
    {
        _timer += Time.deltaTime * 3f;
        float offset = Mathf.Sin(_timer) * 0.15f;
        transform.localPosition = _startPos + new Vector3(0, offset, 0);
    }
}
