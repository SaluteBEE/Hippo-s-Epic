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

    public void Open(BattleManager battleManager, Action<int> onSkillSelected)
    {
        base.Open(battleManager);
        _onSkillSelected = onSkillSelected;
        RefreshSkillList();
    }

    private void RefreshSkillList()
    {
        if (skillContainer == null || skillItemPrefab == null) return;
        if (_battleManager == null || _battleManager.CurrentUnit == null) return;

        // 清空旧列表
        foreach (Transform child in skillContainer)
            Destroy(child.gameObject);

        var unit = _battleManager.CurrentUnit;
        var tables = _battleManager.GetTables();

        // 添加普通攻击
        AddSkillItem(0, "普通攻击", false);

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

            AddSkillItem(skillId, name, onCooldown);
        }
    }

    private void AddSkillItem(int skillId, string name, bool disabled)
    {
        var go = Instantiate(skillItemPrefab.gameObject, skillContainer);
        go.SetActive(true);

        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = name;

        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = !disabled;
            int capturedId = skillId;
            btn.onClick.AddListener(() => _onSkillSelected?.Invoke(capturedId));
        }
    }
}
