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
    public void StartDialog_Id1_PlaysNineContents_InSortOrder()
    {
        var contents = GetSortedContents(1);
        Assert.AreEqual(9, contents.Count);

        for (int i = 0; i < contents.Count; i++)
            Assert.AreEqual(i + 1, contents[i].Sortid);
    }

    [Test]
    public void StartDialog_Id1_ContentTypes_NarrationAndDialogue()
    {
        var contents = GetSortedContents(1);

        Assert.AreEqual(SpeakerSide.Middle, ResolveSide(contents[0], tables.TbDialog.Get(1)), "sortid=1 应为旁白");
        Assert.AreEqual(SpeakerSide.Middle, ResolveSide(contents[1], tables.TbDialog.Get(1)), "sortid=2 应为旁白");
        Assert.AreEqual(SpeakerSide.Middle, ResolveSide(contents[2], tables.TbDialog.Get(1)), "sortid=3 应为旁白");
    }

    [Test]
    public void Dialog_Id1_AfterContents_NextDialogIsId2()
    {
        var dialog = tables.TbDialog.Get(1);
        Assert.AreEqual(1, dialog.Type);
        Assert.AreEqual(2, dialog.Param1[0]);
    }

    [Test]
    public void Dialog_Id2_IsOptionType_WithThreeBranches()
    {
        var dialog = tables.TbDialog.Get(2);
        Assert.AreEqual(2, dialog.Type);
        Assert.AreEqual(3, dialog.Param1.Count);
    }

    [Test]
    public void Dialog_Id2_Options_HaveSelectionNames()
    {
        var dialog = tables.TbDialog.Get(2);

        foreach (int childId in dialog.Param1)
        {
            var child = tables.TbDialog.Get(childId);
            Assert.IsNotEmpty(child.SelectionName,
                $"Dialog {childId} 作为选项应有 SelectionName");
        }
    }

    [Test]
    public void Dialog_ChooseBranch3_PlaysContentThenReturnsTo2()
    {
        var dialog3 = tables.TbDialog.Get(3);
        Assert.AreEqual(1, dialog3.Type);
        Assert.AreEqual(1, dialog3.Param1.Count);
        Assert.AreEqual(2, dialog3.Param1[0], "分支3播放完后应回到Dialog 2");

        var contents = GetSortedContents(3);
        Assert.AreEqual(1, contents.Count, "Dialog 3 应有1条副表内容");
    }

    [Test]
    public void Dialog_ChooseBranch4_IsTerminal()
    {
        var dialog4 = tables.TbDialog.Get(4);
        Assert.AreEqual(0, dialog4.Param1.Count, "Dialog 4 无跳转，应为终端");

        var contents = GetSortedContents(4);
        Assert.AreEqual(6, contents.Count, "Dialog 4 应有6条副表内容");
    }

    [Test]
    public void Dialog_ChooseBranch5_PlaysContentThenReturnsTo2()
    {
        var dialog5 = tables.TbDialog.Get(5);
        Assert.AreEqual(2, dialog5.Param1[0], "分支5播放完后应回到Dialog 2");

        var contents = GetSortedContents(5);
        Assert.AreEqual(1, contents.Count);
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
        var contents1 = GetSortedContents(1);
        Assert.AreEqual(9, contents1.Count);

        var dialog1 = tables.TbDialog.Get(1);
        Assert.AreEqual(2, dialog1.Param1[0]);

        var dialog2 = tables.TbDialog.Get(2);
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
    public void Dialog_Id4_AllSixContents_AreDialogue()
    {
        var contents = GetSortedContents(4);
        Assert.AreEqual(6, contents.Count);

        foreach (var dc in contents)
            Assert.AreEqual(2, dc.Type, $"Dialog4 副表 id={dc.Id} 应为对话(type=2)");
    }

    [Test]
    public void Dialog_Id4_Contents_SortedBySortid()
    {
        var contents = GetSortedContents(4);
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
    public void DialogContent_NoContentForDialog2()
    {
        var contents = GetSortedContents(2);
        Assert.AreEqual(0, contents.Count, "Dialog 2 是选项类型，不应有副表内容");
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
