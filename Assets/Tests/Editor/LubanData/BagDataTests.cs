using NUnit.Framework;

class BagDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Bag_Count_IsOne()
    {
        Assert.AreEqual(1, tables.TbBag.DataList.Count);
    }

    [Test]
    public void Bag_GetById_ReturnsEntity()
    {
        var bag = tables.TbBag.Get(1);
        Assert.IsNotNull(bag);
        Assert.AreEqual(1, bag.Id);
    }

    [Test]
    public void Bag_Id1_AllFieldsMatch()
    {
        var bag = tables.TbBag.Get(1);
        Assert.AreEqual(1, bag.Id);
        Assert.AreEqual(1, bag.Itemid);
        Assert.AreEqual(500, bag.Num);
        Assert.AreEqual(1, bag.Ownerid);
    }
}
