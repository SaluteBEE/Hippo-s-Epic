using NUnit.Framework;

class TableQueryTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Get_ExistingId_ReturnsEntity()
    {
        var bag = tables.TbBag.Get(1);
        Assert.AreEqual(1, bag.Id);
    }

    [Test]
    public void GetOrDefault_ExistingId_ReturnsEntity()
    {
        var bag = tables.TbBag.GetOrDefault(1);
        Assert.IsNotNull(bag);
        Assert.AreEqual(1, bag.Id);
    }

    [Test]
    public void GetOrDefault_NonexistentId_ReturnsNull()
    {
        var bag = tables.TbBag.GetOrDefault(99999);
        Assert.IsNull(bag);
    }

    [Test]
    public void Indexer_ExistingId_ReturnsEntity()
    {
        var bag = tables.TbBag[1];
        Assert.AreEqual(1, bag.Id);
    }

    [Test]
    public void DataMap_ContainsAllExpectedIds()
    {
        Assert.IsTrue(tables.TbBag.DataMap.ContainsKey(1));
        Assert.IsTrue(tables.TbItem.DataMap.ContainsKey(1));
        Assert.IsTrue(tables.TbItem.DataMap.ContainsKey(2));
        Assert.IsTrue(tables.TbBuff.DataMap.ContainsKey(1));
        Assert.IsTrue(tables.TbBuff.DataMap.ContainsKey(2));
        Assert.IsTrue(tables.TbBuff.DataMap.ContainsKey(3));
        Assert.IsTrue(tables.TbPerson.DataMap.ContainsKey(1));
        Assert.IsTrue(tables.TbPerson.DataMap.ContainsKey(2));
    }

    [Test]
    public void DataList_MatchesExcelOrder()
    {
        var ids = new int[tables.TbBuff.DataList.Count];
        for (int i = 0; i < ids.Length; i++)
            ids[i] = tables.TbBuff.DataList[i].Id;
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, ids);
    }

    [Test]
    public void DataMap_IsReadOnly()
    {
        var map = tables.TbBag.DataMap;
        Assert.IsNotNull(map);
        Assert.IsInstanceOf<System.Collections.IEnumerable>(map);
    }

    [Test]
    public void DataList_IsReadOnly()
    {
        var list = tables.TbBag.DataList;
        Assert.IsNotNull(list);
        Assert.IsInstanceOf<System.Collections.IEnumerable>(list);
    }
}
