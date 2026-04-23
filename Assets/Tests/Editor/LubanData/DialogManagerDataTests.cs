using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

class DialogManagerDataTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    [Test]
    public void Dialog_Id1_NineContents_SortedCorrectly()
    {
        var contents = tables.TbDialogcontent.DataList
            .Where(c => c.Dialogid == 1)
            .OrderBy(c => c.Sortid).ToList();

        Assert.AreEqual(9, contents.Count);
        for (int i = 0; i < contents.Count; i++)
            Assert.AreEqual(i + 1, contents[i].Sortid);
    }

    [Test]
    public void Dialog_Id1_ContentTypes_MixOfNarrationAndDialogue()
    {
        var contents = tables.TbDialogcontent.DataList
            .Where(c => c.Dialogid == 1)
            .OrderBy(c => c.Sortid).ToList();

        int narration = contents.Count(c => c.Type == 0);
        int dialogue = contents.Count(c => c.Type == 2);

        Assert.Greater(narration, 0, "应有旁白内容");
        Assert.Greater(dialogue, 0, "应有对话内容");
    }

    [Test]
    public void Dialog_Id1_NormalType_NextDialogIs2()
    {
        var dialog = tables.TbDialog.Get(1);
        Assert.AreEqual(1, dialog.Type);
        Assert.AreEqual(new List<int> { 2 }, dialog.Param1);
    }

    [Test]
    public void Dialog_Id2_OptionType_ThreeBranches()
    {
        var dialog = tables.TbDialog.Get(2);
        Assert.AreEqual(2, dialog.Type);
        Assert.AreEqual(3, dialog.Param1.Count);

        foreach (int childId in dialog.Param1)
        {
            var child = tables.TbDialog.GetOrDefault(childId);
            Assert.IsNotNull(child, $"子 Dialog {childId} 应存在");
        }
    }

    [Test]
    public void Dialog_Branch3_SelectionNameNotEmpty()
    {
        var dialog = tables.TbDialog.Get(3);
        Assert.IsNotEmpty(dialog.SelectionName, "选项 Dialog 应有 SelectionName");
    }

    [Test]
    public void Dialog_Branch4_SelectionNameNotEmpty()
    {
        var dialog = tables.TbDialog.Get(4);
        Assert.IsNotEmpty(dialog.SelectionName);
    }

    [Test]
    public void Dialog_Branch5_SelectionNameNotEmpty()
    {
        var dialog = tables.TbDialog.Get(5);
        Assert.IsNotEmpty(dialog.SelectionName);
    }

    [Test]
    public void Dialog_Branch4_EmptyParam1_IsTerminal()
    {
        var dialog = tables.TbDialog.Get(4);
        Assert.AreEqual(0, dialog.Param1.Count, "Dialog 4 无跳转目标，应为终端节点");
    }

    [Test]
    public void Dialog_Branch3_Param1PointsTo2()
    {
        var dialog = tables.TbDialog.Get(3);
        Assert.AreEqual(new List<int> { 2 }, dialog.Param1);
    }

    [Test]
    public void Dialog_Branch5_Param1PointsTo2()
    {
        var dialog = tables.TbDialog.Get(5);
        Assert.AreEqual(new List<int> { 2 }, dialog.Param1);
    }

    [Test]
    public void Dialog_AllSpeakerRefs_Resolved()
    {
        foreach (var dialog in tables.TbDialog.DataList)
        {
            Assert.IsNotNull(dialog.Speakerid1_Ref,
                $"Dialog {dialog.Id} Speakerid1_Ref 应已解析");
            Assert.IsNotNull(dialog.Speakerid2_Ref,
                $"Dialog {dialog.Id} Speakerid2_Ref 应已解析");
        }
    }

    [Test]
    public void Dialog_Id1_Speaker1IsHippo_Speaker2IsCoach()
    {
        var dialog = tables.TbDialog.Get(1);
        Assert.AreEqual("河马", dialog.Speakerid1_Ref.Name);
        Assert.AreEqual("教练", dialog.Speakerid2_Ref.Name);
    }

    [Test]
    public void DialogContent_Type0_IsNarration()
    {
        var narrationContents = tables.TbDialogcontent.DataList
            .Where(c => c.Type == 0).ToList();
        Assert.Greater(narrationContents.Count, 0, "应有旁白内容");
    }

    [Test]
    public void DialogContent_Type2_IsDialogue()
    {
        var dialogueContents = tables.TbDialogcontent.DataList
            .Where(c => c.Type == 2).ToList();
        Assert.Greater(dialogueContents.Count, 0, "应有对话内容");
    }

    [Test]
    public void Dialog_FullFlow_LinearPath_DataIntegrity()
    {
        var dialog1 = tables.TbDialog.Get(1);
        var contents1 = tables.TbDialogcontent.DataList
            .Where(c => c.Dialogid == 1).OrderBy(c => c.Sortid).ToList();
        Assert.AreEqual(9, contents1.Count);

        int nextId = dialog1.Param1[0];
        var dialog2 = tables.TbDialog.Get(nextId);
        Assert.AreEqual(2, dialog2.Type);

        int branch1Id = dialog2.Param1[0];
        var dialog3 = tables.TbDialog.Get(branch1Id);
        Assert.AreEqual(1, dialog3.Type);
        Assert.IsNotEmpty(dialog3.SelectionName);

        var contents3 = tables.TbDialogcontent.DataList
            .Where(c => c.Dialogid == dialog3.Id).ToList();
        Assert.AreEqual(1, contents3.Count);
    }

    [Test]
    public void Dialog_AllBranchTargets_ExistInTable()
    {
        foreach (var dialog in tables.TbDialog.DataList)
        {
            if (dialog.Type == 1 || dialog.Type == 2)
            {
                foreach (int targetId in dialog.Param1)
                {
                    Assert.IsNotNull(tables.TbDialog.GetOrDefault(targetId),
                        $"Dialog {dialog.Id} Param1 中的目标 {targetId} 不存在");
                }
            }
        }
    }
}
