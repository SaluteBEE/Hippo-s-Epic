using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗子面板基类
/// </summary>
public abstract class BattleSubPanel : MonoBehaviour
{
    [SerializeField] protected GameObject panelRoot;
    [SerializeField] protected Button btnClose;

    protected BattleManager _battleManager;

    /// <summary>
    /// 子面板初始化：绑定关闭按钮；panelRoot 为空时回退到自身
    /// </summary>
    protected virtual void Awake()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(Close);
        if (panelRoot == null) panelRoot = gameObject;
    }

    /// <summary>
    /// 打开面板：记录 BattleManager 引用并显示面板根节点
    /// </summary>
    public virtual void Open(BattleManager battleManager)
    {
        _battleManager = battleManager;
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    /// <summary>
    /// 关闭面板：隐藏面板根节点
    /// </summary>
    public virtual void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}

/// <summary>
/// 技能子面板（暴力行为）
/// 显示当前单位可用技能列表
/// </summary>
public class BattleSkillPanel : BattleSubPanel
{
    [Header("技能列表")]
    [SerializeField] private Transform skillContainer;
    [SerializeField] private Button skillItemPrefab;

    private Action<int> _onSkillSelected;
    private Func<int, bool> _canUseOnTarget;
    private BattleUnit _unit;  // 打开面板时的当前单位（由上层事件缓存传入，不主动读取）

    /// <summary> 已创建的技能项（非冷却项，供目标有效性重算时刷新置灰与 interactable） </summary>
    private readonly List<(int skillId, Button btn, Image img, TextMeshProUGUI tmp)> _items = new List<(int, Button, Image, TextMeshProUGUI)>();

    public void Open(BattleManager battleManager, BattleUnit unit, Action<int> onSkillSelected,
                     Func<int, bool> canUseOnTarget = null)
    {
        base.Open(battleManager);
        _unit = unit;
        _onSkillSelected = onSkillSelected;
        _canUseOnTarget = canUseOnTarget;
        RefreshSkillList();
    }

    /// <summary>
    /// 目标切换/目标状态变化后刷新置灰（事件驱动，由上层订阅触发；只重算 alpha，不重建列表）
    /// </summary>
    public void RefreshInvalidState()
    {
        if (_battleManager == null || _unit == null) return;
        if (panelRoot != null && !panelRoot.activeSelf) return;

        int remainingAp = _unit.CurrentActionPoints;

        foreach (var (skillId, btn, img, tmp) in _items)
        {
            bool invalid = IsInvalidForTarget(skillId);

            // 行动点不足同样置灰（技能 cost / 普攻 1 点）
            if (!invalid)
            {
                if (skillId == 0)
                    invalid = remainingAp < 1;
                else
                {
                    var cfg = _battleManager.GetTables()?.TbSkill.GetOrDefault(skillId);
                    invalid = cfg != null && remainingAp < cfg.Cost;
                }
            }

            float a = invalid ? 0.45f : 1.00f;
            if (img != null) img.color = new Color(img.color.r, img.color.g, img.color.b, a);
            if (tmp != null) tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, a);
        }
    }

    private void RefreshSkillList()
    {
        bool prefabIsAsset = skillItemPrefab != null && !skillItemPrefab.gameObject.scene.IsValid();
        Debug.Log($"[BattleSkillPanel] RefreshSkillList: " +
                  $"container={skillContainer} " +
                  $"prefab={skillItemPrefab} " +
                  $"prefabIsAsset={prefabIsAsset} (false=引用的是场景实例) " +
                  $"manager={_battleManager} unit={_unit}");
        if (skillContainer == null || skillItemPrefab == null) return;
        if (_battleManager == null || _unit == null) return;

        // 清空旧列表
        foreach (Transform child in skillContainer)
            Destroy(child.gameObject);
        _items.Clear();

        var unit = _unit;
        var tables = _battleManager.GetTables();

        // 行动点信息（置灰判断依据：cost > 剩余行动点）
        int remainingAp = unit.CurrentActionPoints;

        // 添加普通攻击（固定消耗 1 行动点）
        AddSkillItem(0, "普通攻击", false, IsInvalidForTarget(0) || remainingAp < 1);

        // 添加技能
        foreach (int skillId in unit.AvailableSkills)
        {
            bool onCooldown = unit.IsSkillOnCooldown(skillId);
            var skillCfg = tables?.TbSkill.GetOrDefault(skillId);
            string name = skillCfg != null ? skillCfg.Name : $"技能{skillId}";

            if (onCooldown)
            {
                unit.SkillCooldowns.TryGetValue(skillId, out int cd);
                name += $" (冷却{cd})";
            }

            bool apNotEnough = skillCfg != null && remainingAp < skillCfg.Cost;
            AddSkillItem(skillId, name, onCooldown,
                IsInvalidForTarget(skillId) || apNotEnough);
        }
    }

    /// <summary> 当前选中目标对该技能是否无效（用于置灰提示，点击时由上层给出提示） </summary>
    private bool IsInvalidForTarget(int skillId)
    {
        return _canUseOnTarget != null && !_canUseOnTarget(skillId);
    }

    private void AddSkillItem(int skillId, string name, bool disabled, bool invalidForTarget = false)
    {
        var srcBtn = skillItemPrefab;
        bool srcIsScene = srcBtn != null && srcBtn.gameObject.scene.IsValid();
        Debug.Log($"[BattleSkillPanel][{name}] 源模板: " +
                  $"skillItemPrefab={(srcBtn ? "非空" : "NULL")} " +
                  $"srcIsSceneInstance={srcIsScene} " +
                  $"btnEnabled={srcBtn?.enabled} btnInteractable={srcBtn?.interactable} " +
                  $"goActive={srcBtn != null && srcBtn.gameObject.activeSelf}");

        var go = Instantiate(skillItemPrefab.gameObject, skillContainer);
        go.SetActive(true);

        // skillItemPrefab 作为模板时组件可能是 disabled，克隆后需显式启用，
        // 否则 SetActive 只会激活 GameObject，disable 的组件仍不工作
        var imgMore = go.GetComponentInChildren<Image>();
        if (imgMore != null) imgMore.enabled = true;

        var btnMore = go.GetComponent<Button>();
        if (btnMore != null) btnMore.enabled = true;

        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.enabled = true;
        if (tmp != null) tmp.text = name;

        var btn = go.GetComponent<Button>();
        var grid = go.GetComponentInParent<GridLayoutGroup>();
        var contentFitter = go.GetComponentInParent<ContentSizeFitter>();

        Debug.Log($"[BattleSkillPanel][{name}] 实例化即时: " +
                  $"goActiveSelf={go.activeSelf} goActiveInHierarchy={go.activeInHierarchy} " +
                  $"btn={btn} btnEnabled={btn?.enabled} btnInteractable={btn?.interactable} " +
                  $"tmp={tmp} tmpEnabled={tmp?.enabled} tmpActiveInHierarchy={(tmp != null ? tmp.gameObject.activeInHierarchy : false)} " +
                  $"iconImage={go.GetComponentInChildren<Image>()?.gameObject.name} " +
                  $"grid={grid} gridEnabled={grid?.enabled} gridActive={(grid != null && grid.gameObject.activeInHierarchy)} " +
                  $"fitter={contentFitter} fitterEnabled={contentFitter?.enabled}");

        if (isActiveAndEnabled)
            StartCoroutine(LogFinalState(skillId, name, go));
        else
            LogItemState(skillId, name, go, "自检(宿主未激活,无下一帧)");

        if (btn != null)
        {
            // 仅冷却禁用；行动点不足只置灰颜色仍可点，点击后由 OnSkillSelected 提示原因
            btn.interactable = !disabled;
            int capturedId = skillId;
            btn.onClick.AddListener(() => _onSkillSelected?.Invoke(capturedId));
        }

        // 目标有效性 → 显式设置颜色（有效恢复全色，无效置灰；不依赖模板原色）
        // 注意: 模板根 Image 可能本身 alpha=0.45, 因此有效时必须显式设回 1.00
        var baseImg = go.GetComponent<Image>();
        var baseTmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (!disabled)
        {
            // 记录非冷却项，供目标切换后 RefreshInvalidState 重算置灰与 interactable
            _items.Add((skillId, btn, baseImg, baseTmp));

            float a = invalidForTarget ? 0.45f : 1.00f;
            if (baseImg != null)
                baseImg.color = new Color(baseImg.color.r, baseImg.color.g, baseImg.color.b, a);
            if (baseTmp != null)
                baseTmp.color = new Color(baseTmp.color.r, baseTmp.color.g, baseTmp.color.b, a);
        }
    }

    /// <summary>
    /// 等一帧再打印一次终态，暴露是否有组件在实例化后的帧里被禁用
    /// </summary>
    private System.Collections.IEnumerator LogFinalState(int skillId, string name, GameObject go)
    {
        yield return null;
        if (go == null) yield break;
        LogItemState(skillId, name, go, "下一帧终态");
    }

    /// <summary>
    /// 打印技能项的当前组件/激活状态
    /// </summary>
    private void LogItemState(int skillId, string name, GameObject go, string stage)
    {
        if (go == null) return;

        var btn = go.GetComponent<Button>();
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        var image = go.GetComponentInChildren<Image>();
        var grid = go.GetComponentInParent<GridLayoutGroup>();
        Debug.Log($"[BattleSkillPanel][{name}] {stage}: " +
                  $"goActiveInHierarchy={go.activeInHierarchy} " +
                  $"btnEnabled={btn?.enabled} btnInteractable={btn?.interactable} " +
                  $"tmpEnabled={tmp?.enabled} tmpActive={(tmp != null && tmp.gameObject.activeInHierarchy)} " +
                  $"imageEnabled={image?.enabled} imageActive={(image != null && image.gameObject.activeInHierarchy)} " +
                  $"gridEnabled={grid?.enabled} gridActive={(grid != null && grid.gameObject.activeInHierarchy)}");
    }
}
