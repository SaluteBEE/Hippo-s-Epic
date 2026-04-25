using NUnit.Framework;
using System.Linq;

class DialogcontentDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Dialogcontent_Count_IsFortyEight()
    {
        Assert.AreEqual(48, tables.TbDialogcontent.DataList.Count);
    }

    [Test]
    public void Dialogcontent_Id1_AllFieldsMatch()
    {
        var dc = tables.TbDialogcontent.Get(1);
        Assert.AreEqual(1, dc.Id);
        Assert.AreEqual(1, dc.Dialogid);
        Assert.AreEqual(0, dc.Type);
        Assert.AreEqual("教练双手揣在口袋里，假装自己是洛奇·巴尔博亚的教练米奇，或者是米老鼠，米老头，米什么玩意儿。", dc.Content);
        Assert.AreEqual(1, dc.Sortid);
        Assert.AreEqual("", dc.Statename1);
        Assert.AreEqual("", dc.Statename2);
    }

    [Test]
    public void Dialogcontent_RichText_ContainsColorTag()
    {
        var dc = tables.TbDialogcontent.Get(6);
        Assert.IsTrue(dc.Content.Contains("<color=red>"), "Content 应包含富文本标签");
        Assert.IsTrue(dc.Content.Contains("</color>"), "Content 应包含闭合标签");
    }

    [Test]
    public void Dialogcontent_ByDialogId1_CorrectCount()
    {
        var contents = tables.TbDialogcontent.DataList.Where(d => d.Dialogid == 1).ToList();
        Assert.AreEqual(9, contents.Count);
    }

    [Test]
    public void Dialogcontent_ByDialogId4_CorrectCount()
    {
        var contents = tables.TbDialogcontent.DataList.Where(d => d.Dialogid == 4).ToList();
        Assert.AreEqual(6, contents.Count);
    }

    [Test]
    public void Dialogcontent_ByDialogId3_CorrectCount()
    {
        var contents = tables.TbDialogcontent.DataList.Where(d => d.Dialogid == 3).ToList();
        Assert.AreEqual(1, contents.Count);
    }

    [Test]
    public void Dialogcontent_ByDialogId5_CorrectCount()
    {
        var contents = tables.TbDialogcontent.DataList.Where(d => d.Dialogid == 5).ToList();
        Assert.AreEqual(1, contents.Count);
    }

    [Test]
    public void Dialogcontent_SortedBySortid_WithinSameDialog()
    {
        var dialogGroups = tables.TbDialogcontent.DataList
            .GroupBy(d => d.Dialogid);
        foreach (var group in dialogGroups)
        {
            var sorted = group.OrderBy(d => d.Sortid).ToList();
            var original = group.ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                Assert.AreEqual(sorted[i].Id, original[i].Id,
                    $"Dialogid {group.Key} 的 Sortid 排序顺序不正确");
            }
        }
    }

    [Test]
    public void Dialogcontent_AllContent_NotNull()
    {
        foreach (var dc in tables.TbDialogcontent.DataList)
        {
            Assert.IsNotNull(dc.Content, $"Dialogcontent {dc.Id} Content 不应为 null");
        }
    }
}
