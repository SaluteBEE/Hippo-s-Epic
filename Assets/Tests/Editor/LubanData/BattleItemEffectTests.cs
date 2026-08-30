using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// 物品战斗参数与范围展开测试（param2 小键盘形状 → 实际命中槽位）
/// </summary>
class BattleItemEffectTests
{
    [Test]
    public void Range_SelfOnly_Key5()
    {
        // 鞭炮 param2=[5]：只命中落点本体
        var slots = SkillCombatUtil.ExpandRangeSlots(4, new List<int> { 5 });
        Assert.AreEqual(new List<int> { 4 }, slots);
    }

    [Test]
    public void Range_MidColumn_Key258()
    {
        // 绷带 param2=[2,5,8]：以落点为中心的竖排（上中-中心-下中）
        // 中心槽位 1 (row0,col1)：8→上, 5→中, 2→下
        var slots = SkillCombatUtil.ExpandRangeSlots(1, new List<int> { 2, 5, 8 });
        slots.Sort();
        // 槽位 1 的上方(row-1)=槽? 越界则忽略
        Assert.IsTrue(slots.Count >= 1);
        Assert.Contains(1, slots);  // 本体必然命中
    }

    [Test]
    public void Range_Expansion_OutOfGrid_IsSkipped()
    {
        // 中心槽位 0 (左上角)：2→下方, 8→上方越界, 5→本体 → 只命中本体和下方
        var slots = SkillCombatUtil.ExpandRangeSlots(0, new List<int> { 2, 5, 8 });
        Assert.Contains(0, slots);
        Assert.Contains(3, slots);  // 槽位0下方一格
        Assert.AreEqual(2, slots.Count);
    }

    [Test]
    public void Item_CenterSelf_MatchesExpansion()
    {
        var tables = TestDataLoader.LoadTables();
        var item = tables.TbItem.Get(12); // 鞭炮
        // 以槽位4为落点，范围[5]=本体
        var slots = SkillCombatUtil.ExpandRangeSlots(4, item.Param2);
        Assert.AreEqual(1, slots.Count);
        Assert.AreEqual(4, slots[0]);
    }
}
