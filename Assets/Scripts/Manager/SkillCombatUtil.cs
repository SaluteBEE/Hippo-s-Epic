using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能战斗工具：九宫格坐标、效果数值（不做敌方镜像，由场景节点布局负责）
/// </summary>
public static class SkillCombatUtil
{
    // 槽位 0-8 → 小键盘
    // 0 1 2 → 7 8 9
    // 3 4 5 → 4 5 6
    // 6 7 8 → 1 2 3
    private static readonly int[] SlotToKeypad = { 7, 8, 9, 4, 5, 6, 1, 2, 3 };

    // 小键盘 1-9 → 槽位
    private static readonly int[] KeypadToSlot = { -1, 6, 7, 8, 3, 4, 5, 0, 1, 2 };

    public static int SlotIndexToKeypad(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 8) return -1;
        return SlotToKeypad[slotIndex];
    }

    public static bool TryKeypadToSlot(int keypad, out int slotIndex)
    {
        if (keypad < 1 || keypad > 9)
        {
            slotIndex = -1;
            return false;
        }

        slotIndex = KeypadToSlot[keypad];
        return slotIndex >= 0;
    }

    public static void SlotToRowCol(int slotIndex, out int row, out int col)
    {
        row = slotIndex / 3;
        col = slotIndex % 3;
    }

    public static bool TryRowColToSlot(int row, int col, out int slotIndex)
    {
        if (row < 0 || row > 2 || col < 0 || col > 2)
        {
            slotIndex = -1;
            return false;
        }

        slotIndex = row * 3 + col;
        return true;
    }

    /// <summary>
    /// 小键盘格相对中心键5的行列偏移
    /// </summary>
    public static void KeypadToOffsetFromCenter(int keypad, out int dRow, out int dCol)
    {
        if (!TryKeypadToSlot(keypad, out int slot))
        {
            dRow = 0;
            dCol = 0;
            return;
        }

        SlotToRowCol(slot, out int row, out int col);
        // 键5对应槽4 → row1,col1
        dRow = row - 1;
        dCol = col - 1;
    }

    /// <summary>
    /// 以 selectedSlot 为键5中心，将 range 中的小键盘编号展开为实际槽位
    /// </summary>
    public static List<int> ExpandRangeSlots(int selectedSlot, IList<int> range)
    {
        var result = new List<int>();
        if (selectedSlot < 0 || selectedSlot > 8) return result;

        SlotToRowCol(selectedSlot, out int centerRow, out int centerCol);

        if (range == null || range.Count == 0)
        {
            result.Add(selectedSlot);
            return result;
        }

        var seen = new HashSet<int>();
        foreach (int keypad in range)
        {
            if (keypad < 1 || keypad > 9) continue;

            KeypadToOffsetFromCenter(keypad, out int dRow, out int dCol);
            if (TryRowColToSlot(centerRow + dRow, centerCol + dCol, out int slot) && seen.Add(slot))
                result.Add(slot);
        }

        return result;
    }

    /// <summary>
    /// 槽位是否允许作为技能落点（selectable 为空则全可）
    /// </summary>
    public static bool IsSlotSelectable(int slotIndex, IList<int> selectable)
    {
        if (selectable == null || selectable.Count == 0)
            return slotIndex >= 0 && slotIndex <= 8;

        int keypad = SlotIndexToKeypad(slotIndex);
        if (keypad < 0) return false;

        for (int i = 0; i < selectable.Count; i++)
        {
            if (selectable[i] == keypad)
                return true;
        }

        return false;
    }

    public static int GetEffectParam(IList<int> effectparam)
    {
        if (effectparam == null || effectparam.Count == 0)
            return 0;
        return effectparam[0];
    }

    /// <summary>
    /// 按 dmgfunc 计算技能数值（伤害或治疗量）
    /// </summary>
    public static int CalcSkillValue(CharacterStats stats, int dmgfunc, IList<int> effectparam)
    {
        if (stats == null) return 0;

        int param = GetEffectParam(effectparam);
        var func = (SkillDmgFunc)dmgfunc;

        switch (func)
        {
            case SkillDmgFunc.DestroyAdd:
                return Mathf.Max(0, stats.FinalDestroy + param);
            case SkillDmgFunc.CreatorAdd:
                return Mathf.Max(0, stats.FinalCreatorwilling + param);
            case SkillDmgFunc.SelfmatainAdd:
                return Mathf.Max(0, stats.FinalSelfmatain + param);
            case SkillDmgFunc.DestroyMul:
                return Mathf.Max(0, stats.FinalDestroy * param);
            case SkillDmgFunc.CreatorMul:
                return Mathf.Max(0, stats.FinalCreatorwilling * param);
            case SkillDmgFunc.SelfmatainMul:
                return Mathf.Max(0, stats.FinalSelfmatain * param);
            default:
                return Mathf.Max(0, param);
        }
    }
}
