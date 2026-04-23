using NUnit.Framework;
using System.Linq;

class CrossReferenceTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Bag_Itemid_ReferencesValidItem()
    {
        foreach (var bag in tables.TbBag.DataList)
        {
            Assert.IsNotNull(bag.Itemid_Ref,
                $"Bag {bag.Id} Itemid_Ref 不应为 null");
        }
    }

    [Test]
    public void Bag_Ownerid_ReferencesValidPerson()
    {
        foreach (var bag in tables.TbBag.DataList)
        {
            Assert.IsNotNull(bag.Ownerid_Ref,
                $"Bag {bag.Id} Ownerid_Ref 不应为 null");
        }
    }

    [Test]
    public void Dialog_Speakerid_ReferencesValidPerson()
    {
        foreach (var dialog in tables.TbDialog.DataList)
        {
            Assert.IsNotNull(dialog.Speakerid1_Ref,
                $"Dialog {dialog.Id} Speakerid1_Ref 不应为 null");
            Assert.IsNotNull(dialog.Speakerid2_Ref,
                $"Dialog {dialog.Id} Speakerid2_Ref 不应为 null");
        }
    }

    [Test]
    public void Dialogcontent_Dialogid_ReferencesValidDialog()
    {
        foreach (var dc in tables.TbDialogcontent.DataList)
        {
            Assert.IsNotNull(dc.Dialogid_Ref,
                $"Dialogcontent {dc.Id} Dialogid_Ref 不应为 null");
        }
    }

    [Test]
    public void Questcontext_Questid_ReferencesValidQuest()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            Assert.IsNotNull(qc.Questid_Ref,
                $"Questcontext {qc.Id} Questid_Ref 不应为 null");
        }
    }

    [Test]
    public void Questcontext_Conditionid_AllValid()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            foreach (var condRef in qc.Conditionid_Ref)
            {
                Assert.IsNotNull(condRef,
                    $"Questcontext {qc.Id} 的 Conditionid_Ref 中存在 null");
            }
        }
    }

    [Test]
    public void Person_BuffIds_ReferencesValidBuff()
    {
        foreach (var person in tables.TbPerson.DataList)
        {
            foreach (var buffRef in person.BuffIds_Ref)
            {
                Assert.IsNotNull(buffRef,
                    $"Person {person.Id} 的 BuffIds_Ref 中存在 null");
            }
        }
    }
}
