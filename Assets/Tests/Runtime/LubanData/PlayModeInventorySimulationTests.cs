using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;

class PlayModeInventorySimulationTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject("DataTableManager_Test");
        var manager = go.AddComponent<DataTableManager>();
        manager.LoadTables();
        tables = manager.Tables;
    }

    [TearDown]
    public void TearDown()
    {
        var obj = GameObject.Find("DataTableManager_Test");
        if (obj != null) Object.DestroyImmediate(obj);
    }

    [Test]
    public void Bag_InitialItems_Correct()
    {
        var bag = tables.TbBag.Get(1);
        Assert.AreEqual(500, bag.Num);
    }

    [Test]
    public void Bag_ItemRef_NameCorrect()
    {
        var bag = tables.TbBag.Get(1);
        Assert.IsNotNull(bag.Itemid_Ref);
        Assert.AreEqual("钱币", bag.Itemid_Ref.Name);
    }

    [Test]
    public void Bag_ItemRef_StackableCorrect()
    {
        var bag = tables.TbBag.Get(1);
        Assert.IsTrue(bag.Itemid_Ref.Stackable);
    }

    [Test]
    public void Bag_OwnerRef_CorrectPerson()
    {
        var bag = tables.TbBag.Get(1);
        Assert.IsNotNull(bag.Ownerid_Ref);
        Assert.AreEqual("河马", bag.Ownerid_Ref.Name);
    }

    [Test]
    public void Bag_RefChain_Works()
    {
        var bag = tables.TbBag.Get(1);
        Assert.AreEqual("河马", bag.Ownerid_Ref.Name);
        Assert.AreEqual("钱币", bag.Itemid_Ref.Name);
        Assert.AreEqual(1, bag.Ownerid_Ref.BuffIds_Ref.Count);
        Assert.AreEqual("美汁汁儿", bag.Ownerid_Ref.BuffIds_Ref[0].Name);
    }
}
