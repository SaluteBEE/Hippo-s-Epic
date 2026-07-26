using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 咄咄逼人对话选择子面板
/// 显示固定的对话选项，玩家选择后触发对应效果
/// </summary>
public class BattleTalkPanel : BattleSubPanel
{
    [Header("对话选项")]
    [SerializeField] private Transform optionContainer;
    [SerializeField] private Button optionButtonPrefab;

    [Header("选项配置")]
    [SerializeField]
    private List<TalkOption> talkOptions = new List<TalkOption>
    {
        new TalkOption { text = "操你老冯", effectValue = 3 },
        new TalkOption { text = "真是先天性的狗杂种", effectValue = 2 },
        new TalkOption { text = "我特此对上层提出解散申请", effectValue = 5 },
    };

    private Action<int> _onTalkSelected;

    [Serializable]
    public class TalkOption
    {
        [Tooltip("对话文本")]
        public string text;
        [Tooltip("精神伤害值")]
        public int effectValue;
        [Tooltip("冷却回合数")]
        public int cooldown;
    }

    public void Open(BattleManager battleManager, Action<int> onTalkSelected)
    {
        base.Open(battleManager);
        _onTalkSelected = onTalkSelected;
        RefreshOptions();
    }

    private void RefreshOptions()
    {
        if (optionContainer == null || optionButtonPrefab == null) return;

        // 清空旧列表
        foreach (Transform child in optionContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < talkOptions.Count; i++)
        {
            var option = talkOptions[i];
            var go = Instantiate(optionButtonPrefab.gameObject, optionContainer);
            go.SetActive(true);

            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = $"{option.text}\n<size=12><color=#888888>精神伤害: {option.effectValue}</color></size>";

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                int capturedIndex = i;
                btn.onClick.AddListener(() => _onTalkSelected?.Invoke(capturedIndex));
            }
        }
    }

    /// <summary>
    /// 获取选中的对话选项
    /// </summary>
    public TalkOption GetOption(int index)
    {
        if (index >= 0 && index < talkOptions.Count)
            return talkOptions[index];
        return null;
    }
}
