using NUnit.Framework;

class RefResolveTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Bag_Itemid_RefResolved()
    {
        var bag = tables.TbBag.Get(1);
        Assert.IsNotNull(bag.Itemid_Ref);
        Assert.AreEqual(1, bag.Itemid_Ref.Id);
        Assert.AreEqual("钱币", bag.Itemid_Ref.Name);
    }

    [Test]
    public void Bag_Ownerid_RefResolved()
    {
        var bag = tables.TbBag.Get(1);
        Assert.IsNotNull(bag.Ownerid_Ref);
        Assert.AreEqual(1, bag.Ownerid_Ref.Id);
        Assert.AreEqual("河马", bag.Ownerid_Ref.Name);
    }

    [Test]
    public void Dialog_Speakerid_RefResolved()
    {
        foreach (var dialog in tables.TbDialog.DataList)
        {
            Assert.IsNotNull(dialog.Speakerid1_Ref,
                $"Dialog {dialog.Id} Speakerid1_Ref 不应为 null");
            Assert.IsNotNull(dialog.Speakerid2_Ref,
                $"Dialog {dialog.Id} Speakerid2_Ref 不应为 null");
        }
        var d1 = tables.TbDialog.Get(1001001);
        Assert.AreEqual("河马", d1.Speakerid1_Ref.Name);
        Assert.AreEqual("教练", d1.Speakerid2_Ref.Name);
    }

    [Test]
    public void Dialogcontent_Dialogid_RefResolved()
    {
        foreach (var dc in tables.TbDialogcontent.DataList)
        {
            Assert.IsNotNull(dc.Dialogid_Ref,
                $"Dialogcontent {dc.Id} Dialogid_Ref 不应为 null");
            Assert.AreEqual(dc.Dialogid, dc.Dialogid_Ref.Id);
        }
    }

    [Test]
    public void Quest_Conditionid_RefResolved()
    {
        var quest = tables.TbQuest.Get(1);
        Assert.IsNotNull(quest.Conditionid_Ref);
        Assert.AreEqual(1, quest.Conditionid_Ref.Id);
    }

    [Test]
    public void Questcontext_Questid_RefResolved()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            Assert.IsNotNull(qc.Questid_Ref);
            Assert.AreEqual(qc.Questid, qc.Questid_Ref.Id);
        }
    }

    [Test]
    public void Questcontext_Conditionid_RefResolved()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            Assert.IsNotNull(qc.Conditionid_Ref);
            Assert.AreEqual(qc.Conditionid.Count, qc.Conditionid_Ref.Count);
            for (int i = 0; i < qc.Conditionid.Count; i++)
            {
                Assert.IsNotNull(qc.Conditionid_Ref[i]);
                Assert.AreEqual(qc.Conditionid[i], qc.Conditionid_Ref[i].Id);
            }
        }
    }

    [Test]
    public void Person_BuffIds_RefResolved()
    {
        var hippo = tables.TbPerson.Get(1);
        Assert.AreEqual(1, hippo.BuffIds_Ref.Count);
        Assert.AreEqual("美汁汁儿", hippo.BuffIds_Ref[0].Name);

        var coach = tables.TbPerson.Get(2);
        Assert.AreEqual(1, coach.BuffIds_Ref.Count);
        Assert.AreEqual("憋不住了！", coach.BuffIds_Ref[0].Name);
    }

    [Test]
    public void RefChain_BagToItemToPerson()
    {
        var bag = tables.TbBag.Get(1);
        Assert.AreEqual("钱币", bag.Itemid_Ref.Name);
        Assert.AreEqual("河马", bag.Ownerid_Ref.Name);
        Assert.AreEqual(500, bag.Num);
        Assert.IsTrue(bag.Itemid_Ref.Stackable);
    }
}
