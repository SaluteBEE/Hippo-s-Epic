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
    public void Item_Count_IsTwo()
    {
        Assert.AreEqual(2, tables.TbItem.DataList.Count);
    }

    [Test]
    public void Item_Id1_AllFieldsMatch()
    {
        var item = tables.TbItem.Get(1);
        Assert.AreEqual(1, item.Id);
        Assert.AreEqual(1, item.Type);
        Assert.AreEqual("钱币", item.Name);
        Assert.AreEqual(1, item.Icon);
        Assert.IsTrue(item.Stackable);
        Assert.AreEqual("金闪闪", item.Tip);
        Assert.IsNotNull(item.Func);
        Assert.AreEqual(0, item.Func.Count);
        Assert.AreEqual("", item.Param1);
    }

    [Test]
    public void Item_Id2_AllFieldsMatch()
    {
        var item = tables.TbItem.Get(2);
        Assert.AreEqual(2, item.Id);
        Assert.AreEqual(2, item.Type);
        Assert.AreEqual("拳王的牙齿", item.Name);
        Assert.AreEqual(2, item.Icon);
        Assert.IsFalse(item.Stackable);
        Assert.AreEqual("一口好牙", item.Tip);
        Assert.IsNotNull(item.Func);
        Assert.AreEqual(0, item.Func.Count);
        Assert.AreEqual("", item.Param1);
    }

    [Test]
    public void Item_Stackable_OneTrueOneFalse()
    {
        Assert.IsTrue(tables.TbItem.Get(1).Stackable);
        Assert.IsFalse(tables.TbItem.Get(2).Stackable);
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
