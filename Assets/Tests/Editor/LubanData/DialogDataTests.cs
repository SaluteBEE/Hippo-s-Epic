using NUnit.Framework;
using System.Collections.Generic;

class DialogDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Dialog_Count_IsFive()
    {
        Assert.AreEqual(5, tables.TbDialog.DataList.Count);
    }

    [Test]
    public void Dialog_Id1_AllFieldsMatch()
    {
        var dialog = tables.TbDialog.Get(1);
        Assert.AreEqual(1, dialog.Id);
        Assert.AreEqual(1, dialog.Type);
        CollectionAssert.AreEqual(new List<int> { 2 }, dialog.Param1);
        Assert.AreEqual("", dialog.Param2);
        Assert.AreEqual("", dialog.SelectionName);
        Assert.AreEqual(1, dialog.Speakerid1);
        Assert.AreEqual(2, dialog.Speakerid2);
    }

    [Test]
    public void Dialog_Id2_BranchType_HasMultipleParams()
    {
        var dialog = tables.TbDialog.Get(2);
        Assert.AreEqual(2, dialog.Type);
        CollectionAssert.AreEqual(new List<int> { 3, 4, 5 }, dialog.Param1);
    }

    [Test]
    public void Dialog_Id3_SelectionName_ContainsChinese()
    {
        var dialog = tables.TbDialog.Get(3);
        Assert.AreEqual(1, dialog.Type);
        Assert.AreEqual("我是来患上一种病叫查尔斯顿狂<br>热，然后一直跳舞跳到屎尿横流为止", dialog.SelectionName);
    }

    [Test]
    public void Dialog_Id4_EmptyParam1()
    {
        var dialog = tables.TbDialog.Get(4);
        Assert.AreEqual(0, dialog.Param1.Count);
        Assert.AreEqual("一拳打在他脸上！", dialog.SelectionName);
    }

    [Test]
    public void Dialog_Id5_AllFieldsMatch()
    {
        var dialog = tables.TbDialog.Get(5);
        Assert.AreEqual(1, dialog.Type);
        CollectionAssert.AreEqual(new List<int> { 2 }, dialog.Param1);
        Assert.AreEqual("9之后的数字应该是10。", dialog.SelectionName);
    }

    [Test]
    public void Dialog_AllParam1_NotNull()
    {
        foreach (var dialog in tables.TbDialog.DataList)
        {
            Assert.IsNotNull(dialog.Param1, $"Dialog {dialog.Id} Param1 不应为 null");
        }
    }
}
