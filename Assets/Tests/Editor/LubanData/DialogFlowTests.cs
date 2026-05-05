using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

class DialogFlowTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void StartDialog_1001001_PlaysNineContents_InSortOrder()
    {
        var contents = GetSortedContents(1001001);
        Assert.AreEqual(9, contents.Count);

        for (int i = 0; i < contents.Count; i++)
            Assert.AreEqual(i + 1, contents[i].Sortid);
    }

    [Test]
    public void StartDialog_1001001_ContentTypes_NarrationAndDialogue()
    {
        var contents = GetSortedContents(1001001);

        Assert.AreEqual(SpeakerSide.Middle, ResolveSide(contents[0], tables.TbDialog.Get(1001001)), "sortid=1 应为旁白");
        Assert.AreEqual(SpeakerSide.Middle, ResolveSide(contents[1], tables.TbDialog.Get(1001001)), "sortid=2 应为旁白");
        Assert.AreEqual(SpeakerSide.Middle, ResolveSide(contents[2], tables.TbDialog.Get(1001001)), "sortid=3 应为旁白");
    }

    [Test]
    public void Dialog_1001001_AfterContents_NextDialogIs1001002()
    {
        var dialog = tables.TbDialog.Get(1001001);
        Assert.AreEqual(1, dialog.Type);
        Assert.AreEqual(1001002, dialog.Param1[0]);
    }

    [Test]
    public void Dialog_1001002_IsOptionType_WithThreeBranches()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.AreEqual(2, dialog.Type);
        Assert.AreEqual(3, dialog.Param1.Count);
    }

    [Test]
    public void Dialog_1001002_Options_HaveSelectionNames()
    {
        var dialog = tables.TbDialog.Get(1001002);

        foreach (int childId in dialog.Param1)
        {
            var child = tables.TbDialog.Get(childId);
            Assert.IsNotEmpty(child.SelectionName,
                $"Dialog {childId} 作为选项应有 SelectionName");
        }
    }

    [Test]
    public void Dialog_ChooseBranch1001003_PlaysContentThenReturnsTo1001002()
    {
        var dialog3 = tables.TbDialog.Get(1001003);
        Assert.AreEqual(1, dialog3.Type);
        Assert.AreEqual(1, dialog3.Param1.Count);
        Assert.AreEqual(1001002, dialog3.Param1[0], "分支1001003播放完后应回到Dialog 1001002");

        var contents = GetSortedContents(1001003);
        Assert.AreEqual(2, contents.Count, "Dialog 1001003 应有2条副表内容");
    }

    [Test]
    public void Dialog_ChooseBranch1001004_IsTerminal()
    {
        var dialog4 = tables.TbDialog.Get(1001004);
        Assert.AreEqual(0, dialog4.Param1.Count, "Dialog 1001004 无跳转，应为终端");

        var contents = GetSortedContents(1001004);
        Assert.AreEqual(7, contents.Count, "Dialog 1001004 应有7条副表内容");
    }

    [Test]
    public void Dialog_ChooseBranch1001005_PlaysContentThenReturnsTo1001002()
    {
        var dialog5 = tables.TbDialog.Get(1001005);
        Assert.AreEqual(1001002, dialog5.Param1[0], "分支1001005播放完后应回到Dialog 1001002");

        var contents = GetSortedContents(1001005);
        Assert.AreEqual(2, contents.Count);
    }

    [Test]
    public void ResolveSide_Type0_ReturnsMiddle()
    {
        var dc = tables.TbDialogcontent.Get(1);
        var dialog = tables.TbDialog.Get(dc.Dialogid);
        Assert.AreEqual(SpeakerSide.Middle, ResolveSide(dc, dialog));
    }

    [Test]
    public void ResolveSide_Type2_ReturnsRight()
    {
        var dc = tables.TbDialogcontent.Get(4);
        var dialog = tables.TbDialog.Get(dc.Dialogid);
        Assert.AreEqual(SpeakerSide.Right, ResolveSide(dc, dialog));
    }

    [Test]
    public void ResolveSide_SpeakerName_Type2UsesSpeakerid2()
    {
        var dc = tables.TbDialogcontent.Get(4);
        var dialog = tables.TbDialog.Get(dc.Dialogid);
        Assert.AreEqual(2, dc.Type);
        Assert.AreEqual("教练", dialog.Speakerid2_Ref.Name);
    }

    [Test]
    public void DialogFlow_FullSimulation_LinearPath()
    {
        var contents1 = GetSortedContents(1001001);
        Assert.AreEqual(9, contents1.Count);

        var dialog1 = tables.TbDialog.Get(1001001);
        Assert.AreEqual(1001002, dialog1.Param1[0]);

        var dialog2 = tables.TbDialog.Get(1001002);
        Assert.AreEqual(2, dialog2.Type);

        int branchId = dialog2.Param1[0];
        var branchDialog = tables.TbDialog.Get(branchId);
        var branchContents = GetSortedContents(branchId);
        Assert.Greater(branchContents.Count, 0);

        if (branchDialog.Param1.Count > 0)
        {
            int returnId = branchDialog.Param1[0];
            var returnDialog = tables.TbDialog.GetOrDefault(returnId);
            Assert.IsNotNull(returnDialog, $"返回目标 Dialog {returnId} 应存在");
        }
    }

    [Test]
    public void DialogFlow_AllContentsHaveValidDialogRef()
    {
        foreach (var dc in tables.TbDialogcontent.DataList)
        {
            var parentDialog = tables.TbDialog.GetOrDefault(dc.Dialogid);
            Assert.IsNotNull(parentDialog,
                $"Dialogcontent {dc.Id} 的 Dialogid {dc.Dialogid} 在主表中不存在");
        }
    }

    [Test]
    public void DialogFlow_AllParamTargets_HaveContents()
    {
        foreach (var dialog in tables.TbDialog.DataList)
        {
            foreach (int targetId in dialog.Param1)
            {
                var target = tables.TbDialog.GetOrDefault(targetId);
                Assert.IsNotNull(target,
                    $"Dialog {dialog.Id} Param1 目标 {targetId} 不存在");
            }
        }
    }

    [Test]
    public void Dialog_1001004_FirstContentIsNarration()
    {
        var contents = GetSortedContents(1001004);
        Assert.AreEqual(7, contents.Count);
        Assert.AreEqual(0, contents[0].Type, "第一条应为旁白(type=0)");
        for (int i = 1; i < contents.Count; i++)
            Assert.AreEqual(2, contents[i].Type, $"Dialog1001004 副表 id={contents[i].Id} 应为对话(type=2)");
    }

    [Test]
    public void Dialog_1001004_Contents_SortedBySortid()
    {
        var contents = GetSortedContents(1001004);
        for (int i = 0; i < contents.Count - 1; i++)
            Assert.Less(contents[i].Sortid, contents[i + 1].Sortid);
    }

    [Test]
    public void Dialog_InvalidId_ReturnsNull()
    {
        var dialog = tables.TbDialog.GetOrDefault(999);
        Assert.IsNull(dialog);
    }

    [Test]
    public void DialogContent_NoContentForDialog1001002()
    {
        var contents = GetSortedContents(1001002);
        Assert.AreEqual(0, contents.Count, "Dialog 1001002 是选项类型，不应有副表内容");
    }

    private List<cfg.cfg.dialogcontent.Dialogcontent> GetSortedContents(int dialogId)
    {
        return tables.TbDialogcontent.DataList
            .Where(c => c.Dialogid == dialogId)
            .OrderBy(c => c.Sortid)
            .ToList();
    }

    private static SpeakerSide ResolveSide(cfg.cfg.dialogcontent.Dialogcontent content, cfg.cfg.dialog.Dialog dialog)
    {
        switch (content.Type)
        {
            case 0: return SpeakerSide.Middle;
            case 1: return SpeakerSide.Left;
            case 2: return SpeakerSide.Right;
            default: return SpeakerSide.Middle;
        }
    }
}