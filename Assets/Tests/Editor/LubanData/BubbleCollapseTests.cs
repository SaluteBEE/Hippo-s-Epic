using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

class BubbleCollapseTests
{
    private cfg.Tables tables;

    [SetUp]
    public void Setup()
    {
        tables = TestDataLoader.LoadTables();
    }

    #region IsOptionAction — Param2 解析

    [Test]
    public void IsOptionAction_Param2_1_2_1_Index0_IsNotAction()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.IsFalse(IsOptionAction(dialog, 0), "Param2='1|2|1' index=0 应为非动作");
    }

    [Test]
    public void IsOptionAction_Param2_1_2_1_Index1_IsAction()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.IsTrue(IsOptionAction(dialog, 1), "Param2='1|2|1' index=1 应为动作");
    }

    [Test]
    public void IsOptionAction_Param2_1_2_1_Index2_IsNotAction()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.IsFalse(IsOptionAction(dialog, 2), "Param2='1|2|1' index=2 应为非动作");
    }

    [Test]
    public void IsOptionAction_EmptyParam2_IsNotAction()
    {
        var dialog = tables.TbDialog.Get(200);
        Assert.IsTrue(string.IsNullOrEmpty(dialog.Param2));
        Assert.IsFalse(IsOptionAction(dialog, 0), "空 Param2 任何选项都不是动作");
    }

    [Test]
    public void IsOptionAction_IndexOutOfRange_IsNotAction()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.IsFalse(IsOptionAction(dialog, 99), "越界 index 应为非动作");
    }

    [Test]
    public void IsOptionAction_NullDialog_IsNotAction()
    {
        Assert.IsFalse(IsOptionAction(null, 0), "null dialog 应为非动作");
    }

    [Test]
    public void AllOptionDialogs_Param2Format_Valid()
    {
        var optionDialogs = tables.TbDialog.DataList.Where(d => d.Type == 2).ToList();
        foreach (var dialog in optionDialogs)
        {
            if (string.IsNullOrEmpty(dialog.Param2)) continue;

            var parts = dialog.Param2.Split('|');
            Assert.AreEqual(dialog.Param1.Count, parts.Length,
                $"Dialog {dialog.Id} Param2 字段数({parts.Length}) 应与 Param1 选项数({dialog.Param1.Count}) 一致");

            foreach (var part in parts)
            {
                Assert.IsTrue(part.Trim() == "1" || part.Trim() == "2",
                    $"Dialog {dialog.Id} Param2 每项应为 '1' 或 '2'，实际为 '{part}'");
            }
        }
    }

    #endregion

    #region 场景1: 线性对话无选项 — 无折叠

    [Test]
    public void Scenario_LinearDialog100_NoOptions_NoCollapse()
    {
        var flow = SimulateDialogFlow(100, new int[0]);
        Assert.AreEqual(0, flow.CollapseCount, "线性对话 100 无选项，不应有折叠");
        Assert.IsTrue(flow.ReachedEnd, "应正常结束");
    }

    [Test]
    public void Scenario_LinearDialog400_NoOptions_NoCollapse()
    {
        var flow = SimulateDialogFlow(400, new int[0]);
        Assert.AreEqual(0, flow.CollapseCount, "纯旁白对话 400 无选项，不应有折叠");
        Assert.IsTrue(flow.ReachedEnd);
    }

    [Test]
    public void Scenario_LinearDialog500_NoOptions_NoCollapse()
    {
        var flow = SimulateDialogFlow(500, new int[0]);
        Assert.AreEqual(0, flow.CollapseCount, "对话 500 无选项，不应有折叠");
    }

    #endregion

    #region 场景2: 选项前最后气泡是 Right，非动作选项 — 折叠

    [Test]
    public void Scenario_Dialog200_DirectStart_NoPriorBubbles_NoCollapse()
    {
        var flow = SimulateDialogFlow(200, new[] { 1 });
        Assert.AreEqual(0, flow.CollapseCount,
            "Dialog 200 直接开始，无前置气泡，选项前没有 Right → 不折叠");
        Assert.IsTrue(flow.ReachedEnd);
    }

    [Test]
    public void Scenario_Dialog1001001_Then1001002_SelectNonAction_ShouldCollapse()
    {
        var contents1001 = GetSortedContents(1001001);
        Assert.AreEqual(2, contents1001[contents1001.Count - 1].Type,
            "1001001 最后一条是 Right(type=2)");

        var flow = SimulateDialogFlow(1001001, new[] { 0 });
        Assert.AreEqual(1, flow.CollapseCount,
            "1001001播放完→1001002选项前最后是Right→选0(非动作)→播放1001003内容时折叠");
    }

    [Test]
    public void Scenario_Dialog200_Option201_LoopBack_LastBubbleNotRight_NoCollapse()
    {
        var contents201 = GetSortedContents(201);
        Assert.AreEqual(0, contents201[contents201.Count - 1].Type,
            "Dialog 201 最后一条 content 应为旁白(type=0)");

        var flow = SimulateDialogFlow(200, new[] { 0 });
        Assert.AreEqual(0, flow.CollapseCount,
            "选201 → 播放后最后气泡是旁白 → 回到200时选项前不是Right → 不折叠");
    }

    #endregion

    #region 场景3: 直接开始的选项对话无前置气泡 — 无折叠

    [Test]
    public void Scenario_Dialog300_DirectStart_NoCollapse()
    {
        var flow = SimulateDialogFlow(300, new[] { 1 });
        Assert.AreEqual(0, flow.CollapseCount, "Dialog 300 直接开始，无前置气泡，不折叠");
    }

    [Test]
    public void Scenario_Dialog300_NestedOption301_NoCollapse()
    {
        var flow = SimulateDialogFlow(300, new[] { 0 });
        Assert.AreEqual(0, flow.CollapseCount,
            "选 301(嵌套选项)，无前置气泡，不折叠");
    }

    [Test]
    public void Scenario_Dialog300_NestedThenPick303_NoCollapse()
    {
        var flow = SimulateDialogFlow(300, new[] { 0, 0 });
        Assert.AreEqual(0, flow.CollapseCount,
            "无前置气泡 → 选 301(嵌套) → 选 303(有文字)，但因选项前无Right气泡不折叠");
    }

    [Test]
    public void Scenario_Dialog300_NestedThenPick304_NoCollapse()
    {
        var flow = SimulateDialogFlow(300, new[] { 0, 1 });
        Assert.AreEqual(0, flow.CollapseCount,
            "无前置气泡 → 选 301(嵌套) → 选 304(有文字)，但因选项前无Right气泡不折叠");
    }

    #endregion

    #region 场景4: 动作选项(Param2="2") — 不折叠

    [Test]
    public void Scenario_Dialog1001002_ActionOption_Index1_NoCollapse()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.IsTrue(IsOptionAction(dialog, 1), "index=1 应为动作");

        var flow = SimulateDialogFlow(1001001, new[] { 1 });
        Assert.AreEqual(0, flow.CollapseCount,
            "选动作选项(一拳打在他脸上)不应折叠");
    }

    [Test]
    public void Scenario_Dialog1001002_NonActionOption_Index0_ShouldCollapse()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.IsFalse(IsOptionAction(dialog, 0), "index=0 应为非动作");

        var flow = SimulateDialogFlow(1001001, new[] { 0 });
        Assert.AreEqual(1, flow.CollapseCount,
            "选非动作选项(跳舞) → 播放1001003内容 → 折叠1次 → 回到1001002但choices用完");
    }

    [Test]
    public void Scenario_Dialog1001002_NonActionOption_Index2_ShouldCollapse()
    {
        var dialog = tables.TbDialog.Get(1001002);
        Assert.IsFalse(IsOptionAction(dialog, 2), "index=2 应为非动作");

        var flow = SimulateDialogFlow(1001001, new[] { 2 });
        Assert.AreEqual(1, flow.CollapseCount,
            "选非动作选项(9之后是10) → 播放1001005内容 → 折叠1次 → 回到1001002但choices用完");
    }

    [Test]
    public void Scenario_Dialog1001002_NonActionThenAction()
    {
        var flow = SimulateDialogFlow(1001001, new[] { 0, 1 });
        Assert.AreEqual(1, flow.CollapseCount,
            "选0(非动作,折叠1次) → 播放1001003 → 回选项 → 选1(动作,不折叠) → 播放1001004。共1次折叠");
    }

    [Test]
    public void Scenario_Dialog1001002_Mixed_AllThreeOptions()
    {
        var flow = SimulateDialogFlow(1001001, new[] { 0, 2, 1 });
        Assert.AreEqual(2, flow.CollapseCount,
            "选0(非动作,折叠) → 回选项 → 选2(非动作,折叠) → 回选项 → 选1(动作,不折叠)。共2次折叠");
    }

    #endregion

    #region 场景5: 动作选项 — 不折叠

    [Test]
    public void Scenario_ActionOption_1001004_EndsImmediately_NoCollapse()
    {
        var dialog4 = tables.TbDialog.Get(1001004);
        Assert.AreEqual(0, dialog4.Param1.Count, "1001004 是终端节点");

        var contents4 = GetSortedContents(1001004);
        Assert.Greater(contents4.Count, 0, "1001004 有内容");

        var flow = SimulateDialogFlow(1001001, new[] { 1 });
        Assert.AreEqual(0, flow.CollapseCount,
            "选动作选项(一拳打脸) → 播放1001004内容 → 但因为是动作选项所以不折叠");
    }

    #endregion

    #region 场景6: 循环选项 — 子对话最后气泡非 Right 则不折叠

    [Test]
    public void Scenario_Dialog200_LoopThenEnd()
    {
        var contents201 = GetSortedContents(201);
        Assert.AreEqual(0, contents201[contents201.Count - 1].Type, "201 最后是旁白");

        var flow = SimulateDialogFlow(200, new[] { 0, 0, 1 });
        Assert.AreEqual(0, flow.CollapseCount,
            "选201两次后最后气泡都是旁白 → 回到200时选项前不是Right → 不折叠。最后选202进入203有文字但选项前也是旁白");
    }

    #endregion

    #region 辅助方法

    private bool IsOptionAction(cfg.cfg.dialog.Dialog dialog, int optionIndex)
    {
        if (dialog == null || string.IsNullOrEmpty(dialog.Param2)) return false;
        var parts = dialog.Param2.Split('|');
        return optionIndex < parts.Length && parts[optionIndex].Trim() == "2";
    }

    private List<cfg.cfg.dialogcontent.Dialogcontent> GetSortedContents(int dialogId)
    {
        return tables.TbDialogcontent.DataList
            .Where(c => c.Dialogid == dialogId)
            .OrderBy(c => c.Sortid)
            .ToList();
    }

    private FlowResult SimulateDialogFlow(int startId, int[] choices)
    {
        var result = new FlowResult();
        int currentId = startId;
        int choiceIdx = 0;
        int safety = 200;

        int lastBubbleTypeBeforeOption = -1;
        bool pendingCollapse = false;
        while (currentId > 0 && safety-- > 0)
        {
            var dialog = tables.TbDialog.GetOrDefault(currentId);
            if (dialog == null) break;

            if (dialog.Type == 2)
            {
                if (pendingCollapse)
                {
                    pendingCollapse = false;
                    lastBubbleTypeBeforeOption = -1;
                }

                if (choiceIdx >= choices.Length) break;

                lastBubbleTypeBeforeOption = result.LastBubbleType;

                int pick = choices[choiceIdx++];
                bool isAction = IsOptionAction(dialog, pick);

                if (lastBubbleTypeBeforeOption == 2 && !isAction)
                    pendingCollapse = true;
                else
                    pendingCollapse = false;

                currentId = dialog.Param1[pick];

                var childDialog = tables.TbDialog.GetOrDefault(currentId);
                if (childDialog != null && childDialog.Type == 2)
                {
                    continue;
                }

                continue;
            }

            var contents = GetSortedContents(currentId);
            if (contents.Count == 0)
            {
                if (dialog.Param1 == null || dialog.Param1.Count == 0)
                {
                    result.ReachedEnd = true;
                    break;
                }
                currentId = dialog.Param1[0];
                continue;
            }

            foreach (var dc in contents)
            {
                if (pendingCollapse)
                {
                    pendingCollapse = false;
                    result.CollapseCount++;
                }

                result.LastBubbleType = dc.Type;
            }

            if (dialog.Param1 == null || dialog.Param1.Count == 0)
            {
                result.ReachedEnd = true;
                break;
            }

            currentId = dialog.Param1[0];
        }

        if (safety <= 0) result.ReachedEnd = true;
        return result;
    }

    private class FlowResult
    {
        public int CollapseCount;
        public bool ReachedEnd;
        public int LastBubbleType = -1;
    }

    #endregion
}
