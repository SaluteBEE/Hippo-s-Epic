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
    public void Dialog_Count_IsEighteen()
    {
        Assert.AreEqual(18, tables.TbDialog.DataList.Count);
    }

    [Test]
    public void Dialog_1001001_AllFieldsMatch()
    {
        var dialog = tables.TbDialog.Get(1001001);
        Assert.AreEqual(1001001, dialog.Id);
        Assert.AreEqual(1, dialog.Type);
        CollectionAssert.AreEqual(new List<int> { 1001002 }, dialog.Param1);
        Assert.AreEqual("", dialog.Param2);
        Assert.AreEqual("", dialog.SelectionName);
        Assert.AreEqual(1, dialog.Speakerid1);
        Assert.AreEqual(2, dialog.Speakerid2);
    }

    [Test]
    public void Dialog_1001002_BranchType_HasMultipleParams()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.AreEqual(2, dialog.Type);
        CollectionAssert.AreEqual(new List<int> { 1001003, 1001004, 1001005 }, dialog.Param1);
    }

    [Test]
    public void Dialog_1001003_SelectionName_ContainsChinese()
    {
        var dialog = tables.TbDialog.Get(1001003);
        Assert.AreEqual(1, dialog.Type);
        Assert.AreEqual("我是来患上一种病叫查尔斯顿狂<br>热，然后一直跳舞跳到屎尿横流为止", dialog.SelectionName);
    }

    [Test]
    public void Dialog_1001004_EmptyParam1()
    {
        var dialog = tables.TbDialog.Get(1001004);
        Assert.AreEqual(0, dialog.Param1.Count);
        Assert.AreEqual("一拳打在他脸上！", dialog.SelectionName);
    }

    [Test]
    public void Dialog_1001005_AllFieldsMatch()
    {
        var dialog = tables.TbDialog.Get(1001005);
        Assert.AreEqual(1, dialog.Type);
        CollectionAssert.AreEqual(new List<int> { 1001002 }, dialog.Param1);
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