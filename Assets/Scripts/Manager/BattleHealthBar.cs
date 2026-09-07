using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗头顶血条组件（World Space）
/// 用牙齿数量表现血量，跟随角色头顶
/// </summary>
public class BattleHealthBar : MonoBehaviour
{
    private BattleUnit _unit;
    private Transform _followTarget;
    private Vector3 _offset;

    #region 牙齿血条字段

    [Header("牙齿血条")]
    [Tooltip("一颗牙齿代表多少血量")]
    [SerializeField] private int bloodPerYachi = 4;

    [Tooltip("牙齿排布的目标总宽度")]
    [SerializeField] private float rootWidth = 320f;

    [Tooltip("牙齿容器（放实例化牙齿的 root 节点，不拖则自动查找子节点 root）")]
    [SerializeField] private RectTransform yachiRoot;

    [Tooltip("牙齿模板（不拖则自动查找子节点 yachi）")]
    [SerializeField] private GameObject yachiTemplate;

    /// <summary> 已实例化的牙齿列表 </summary>
    private readonly List<GameObject> _teeth = new List<GameObject>();

    /// <summary> 牙齿容器上的水平布局组件 </summary>
    private HorizontalLayoutGroup _layout;

    /// <summary> 牙齿容器上的内容尺寸适配器 </summary>
    private ContentSizeFitter _fitter;

    /// <summary> 单颗牙齿宽度（取自模板 RectTransform） </summary>
    private float _toothWidth = 40f;

    #endregion

    /// <summary>
    /// 初始化：绑定单位、查找牙齿容器与模板
    /// </summary>
    public void Setup(BattleUnit unit, Transform followTarget, Vector3 offset)
    {
        _unit = unit;
        _followTarget = followTarget;
        _offset = offset;

        InitTeethBar();
        UpdateHealth();
    }

    private void LateUpdate()
    {
        // 跟随目标头顶
        if (_followTarget != null && transform.IsChildOf(_followTarget))
        {
            // 已经是目标的后代（含嵌套预制体），自动跟随，保持本地位置即可
            return;
        }

        if (_followTarget != null)
        {
            transform.position = _followTarget.position + _offset;
        }
    }

    /// <summary>
    /// 更新血量显示：按血量生成牙齿
    /// </summary>
    public void UpdateHealth()
    {
        if (_unit == null) return;

        UpdateTeethBar();

        // 阵亡时隐藏
        if (_unit.Stats.Hp <= 0)
            gameObject.SetActive(false);
    }

    #region 牙齿血条

    /// <summary>
    /// 初始化牙齿血条：查找 root/模板、隐藏模板、禁用水平自适应以便固定总宽
    /// </summary>
    private void InitTeethBar()
    {
        if (yachiRoot == null)
        {
            var root = transform.Find("root");
            if (root != null) yachiRoot = root as RectTransform;
        }

        if (yachiTemplate == null)
        {
            var tpl = transform.Find("yachi");
            if (tpl != null) yachiTemplate = tpl.gameObject;
        }

        if (yachiRoot == null || yachiTemplate == null) return;

        _layout = yachiRoot.GetComponent<HorizontalLayoutGroup>();
        _fitter = yachiRoot.GetComponent<ContentSizeFitter>();

        var rt = yachiTemplate.GetComponent<RectTransform>();
        if (rt != null)
        {
            // 模板锚点与预览牙齿保持一致（左下角），确保 HorizontalLayoutGroup 布局不偏移
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            _toothWidth = rt.rect.width;
        }

        // 模板仅作克隆源，隐藏不显示
        yachiTemplate.SetActive(false);

        // 禁用水平自适应，改为固定 rootWidth
        if (_fitter != null)
            _fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    /// <summary>
    /// 更新牙齿血条：按血量生成对应数量牙齿，并用 spacing 把总宽补/压到 rootWidth
    /// </summary>
    private void UpdateTeethBar()
    {
        if (yachiRoot == null || yachiTemplate == null) return;

        int hp = _unit.Stats.Hp;
        int toothCount = bloodPerYachi > 0
            ? Mathf.CeilToInt((float)hp / bloodPerYachi)
            : (hp > 0 ? 1 : 0);
        if (toothCount < 0) toothCount = 0;

        // 同步牙齿数量（不足实例化，多余销毁）
        while (_teeth.Count < toothCount)
        {
            var go = Instantiate(yachiTemplate, yachiRoot);
            go.SetActive(true);
            _teeth.Add(go);
        }
        while (_teeth.Count > toothCount)
        {
            var go = _teeth[_teeth.Count - 1];
            _teeth.RemoveAt(_teeth.Count - 1);
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        // 固定总宽到 rootWidth
        yachiRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rootWidth);

        // 用 spacing 补齐/压缩牙齿到 rootWidth：spacing = (rootWidth - n*牙宽) / (n-1)
        if (_layout != null)
        {
            int n = _teeth.Count;
            _layout.spacing = n > 1 ? (rootWidth - n * _toothWidth) / (n - 1) : 0f;
        }
    }

    #endregion
}
