using System.Collections.Generic;
using NUnit.Framework;

class ItemDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Item_Count_IsEleven()
    {
        Assert.AreEqual(11, tables.TbItem.DataList.Count);
    }

    [Test]
    public void Item_Id1_AllFieldsMatch()
    {
        var item = tables.TbItem.Get(1);
        Assert.AreEqual(1, item.Id);
        Assert.AreEqual(1, item.Type);
        Assert.AreEqual("牙齿", item.Name);
        Assert.AreEqual(1, item.Icon);
        Assert.IsTrue(item.Stackable);
        Assert.AreEqual("金闪闪", item.Tip);
        Assert.IsNotNull(item.Func);
        Assert.AreEqual(0, item.Func.Count);
        Assert.IsNotNull(item.Param1);
        Assert.AreEqual(0, item.Param1.Count);
    }

    [Test]
    public void Item_Id12_Battlefirecracker_FieldsMatch()
    {
        var item = tables.TbItem.Get(12);
        Assert.AreEqual("鞭炮", item.Name);
        // 战斗功能：2=对敌方使用
        Assert.AreEqual(new List<int> { 2 }, item.Func);
        // 效果：2=物理伤害 + 3=加BUFF
        Assert.AreEqual(new List<int> { 2, 3 }, item.Param1);
        // 范围：5=本体
        Assert.AreEqual(new List<int> { 5 }, item.Param2);
        // 数值：5 伤害 + buff 29
        Assert.AreEqual(new List<int> { 5, 29 }, item.Param3);
        StringAssert.Contains("炸毁一切", item.Battletip);
    }

    [Test]
    public void Item_Id13_Bandage_FieldsMatch()
    {
        var item = tables.TbItem.Get(13);
        Assert.AreEqual("绷带", item.Name);
        // 战斗功能：1=对己方使用
        Assert.AreEqual(new List<int> { 1 }, item.Func);
        // 效果：1=治疗
        Assert.AreEqual(new List<int> { 1 }, item.Param1);
        // 范围：2,5,8 = 以本体为中心的中列
        Assert.AreEqual(new List<int> { 2, 5, 8 }, item.Param2);
        // 数值：10 治疗量
        Assert.AreEqual(new List<int> { 10 }, item.Param3);
        StringAssert.Contains("包扎伤口", item.Battletip);
    }

    [Test]
    public void Item_BattleItems_AllHaveFuncAndParams()
    {
        foreach (var item in tables.TbItem.DataList)
        {
            if (item.Func != null && (item.Func.Contains(1) || item.Func.Contains(2)))
            {
                Assert.IsNotNull(item.Param1, $"战斗道具 {item.Id} Param1 不应为 null");
                Assert.IsNotNull(item.Param3, $"战斗道具 {item.Id} Param3 不应为 null");
                Assert.Greater(item.Param1.Count, 0, $"战斗道具 {item.Id} 应配置效果(param1)");
                Assert.Greater(item.Param3.Count, 0, $"战斗道具 {item.Id} 应配置数值(param3)");
            }
        }
    }

    [Test]
    public void Item_Func_IsNotNull()
    {
        foreach (var item in tables.TbItem.DataList)
        {
            Assert.IsNotNull(item.Func, $"Item {item.Id} Func 不应为 null");
        }
    }
}
