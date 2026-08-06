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

    protected virtual void Awake()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(Close);
        if (panelRoot == null) panelRoot = gameObject;
    }

    public virtual void Open(BattleManager battleManager)
    {
        _battleManager = battleManager;
        if (panelRoot != null) panelRoot.SetActive(true);
    }

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

    public void Open(BattleManager battleManager, Action<int> onSkillSelected,
                     Func<int, bool> canUseOnTarget = null)
    {
        base.Open(battleManager);
        _onSkillSelected = onSkillSelected;
        _canUseOnTarget = canUseOnTarget;
        RefreshSkillList();
    }

    private void RefreshSkillList()
    {
        bool prefabIsAsset = skillItemPrefab != null && !skillItemPrefab.gameObject.scene.IsValid();
        Debug.Log($"[BattleSkillPanel] RefreshSkillList: " +
                  $"container={skillContainer} " +
                  $"prefab={skillItemPrefab} " +
                  $"prefabIsAsset={prefabIsAsset} (false=引用的是场景实例) " +
                  $"manager={_battleManager} unit={_battleManager?.CurrentUnit}");
        if (skillContainer == null || skillItemPrefab == null) return;
        if (_battleManager == null || _battleManager.CurrentUnit == null) return;

        // 清空旧列表
        foreach (Transform child in skillContainer)
            Destroy(child.gameObject);

        var unit = _battleManager.CurrentUnit;
        var tables = _battleManager.GetTables();

        // 添加普通攻击
        AddSkillItem(0, "普通攻击", false, IsInvalidForTarget(0));

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

            AddSkillItem(skillId, name, onCooldown, IsInvalidForTarget(skillId));
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
            btn.interactable = !disabled;
            int capturedId = skillId;
            btn.onClick.AddListener(() => _onSkillSelected?.Invoke(capturedId));
        }

        // 目标有效性 → 显式设置颜色（有效恢复全色，无效置灰；不依赖模板原色）
        // 注意: 模板根 Image 可能本身 alpha=0.45, 因此有效时必须显式设回 1.00
        var baseImg = go.GetComponent<Image>();
        var baseTmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (!disabled && baseImg != null)
        {
            float a = invalidForTarget ? 0.45f : 1.00f;
            baseImg.color = new Color(baseImg.color.r, baseImg.color.g, baseImg.color.b, a);
        }
        if (!disabled && baseTmp != null)
        {
            float a = invalidForTarget ? 0.45f : 1.00f;
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
