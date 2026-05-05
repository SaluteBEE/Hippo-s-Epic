using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class PlayModeDialogSimulationTests
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
    public void Dialog_SpeakerNames_Correct()
    {
        var dialog = tables.TbDialog.Get(1001001);
        var speaker1 = tables.TbPerson.Get(dialog.Speakerid1);
        var speaker2 = tables.TbPerson.Get(dialog.Speakerid2);
        Assert.AreEqual("河马", speaker1.Name);
        Assert.AreEqual("教练", speaker2.Name);
    }

    [Test]
    public void DialogContent_ByDialogId_SortedBySortid()
    {
        var contents = tables.TbDialogcontent.DataList
            .Where(d => d.Dialogid == 1001001)
            .OrderBy(d => d.Sortid).ToList();
        Assert.AreEqual(9, contents.Count);
        Assert.AreEqual(1, contents[0].Sortid);
        Assert.AreEqual(9, contents[8].Sortid);
    }

    [Test]
    public void DialogContent_RichText_Correct()
    {
        var dc = tables.TbDialogcontent.Get(6);
        Assert.IsTrue(dc.Content.Contains("<color=red>满地找牙</color>"));
    }

    [Test]
    public void Dialog_BranchType_HasMultipleParams()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.AreEqual(2, dialog.Type);
        Assert.AreEqual(3, dialog.Param1.Count);
        CollectionAssert.AreEqual(new List<int> { 1001003, 1001004, 1001005 }, dialog.Param1);
    }

    [Test]
    public void Dialog_LinearType_SingleParam()
    {
        var dialog = tables.TbDialog.Get(1001001);
        Assert.AreEqual(1, dialog.Type);
        Assert.AreEqual(1, dialog.Param1.Count);
    }

    [Test]
    public void SimulateDialogFlow_StartToEnd()
    {
        var dialog1 = tables.TbDialog.Get(1001001);
        Assert.AreEqual(1, dialog1.Type);
        Assert.AreEqual(1001002, dialog1.Param1[0]);

        var contents1 = tables.TbDialogcontent.DataList
            .Where(d => d.Dialogid == 1001001)
            .OrderBy(d => d.Sortid).ToList();
        Assert.AreEqual(9, contents1.Count);

        var nextDialogId = dialog1.Param1[0];
        Assert.AreEqual(1001002, nextDialogId);

        var dialog2 = tables.TbDialog.Get(nextDialogId);
        Assert.AreEqual(2, dialog2.Type);
        Assert.AreEqual(3, dialog2.Param1.Count);

        var branch1Id = dialog2.Param1[0];
        var dialog3 = tables.TbDialog.Get(branch1Id);
        Assert.IsNotNull(dialog3);

        var contents3 = tables.TbDialogcontent.DataList
            .Where(d => d.Dialogid == dialog3.Id).ToList();
        Assert.AreEqual(2, contents3.Count);
    }
}