using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class PlayModeQuestSimulationTests
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
    public void Quest_InitialData_CorrectState()
    {
        var quest = tables.TbQuest.Get(1);
        Assert.AreEqual(4, quest.State);
    }

    [Test]
    public void Quest_ConditionRef_Resolved()
    {
        var quest = tables.TbQuest.Get(1);
        Assert.IsNotNull(quest.Conditionid_Ref);
        Assert.AreEqual(1, quest.Conditionid_Ref.Id);
    }

    [Test]
    public void Quest_HasRelatedQuestcontexts()
    {
        var contexts = tables.TbQuestcontext.DataList
            .Where(qc => qc.Questid == 1).ToList();
        Assert.AreEqual(4, contexts.Count);
    }

    [Test]
    public void Questcontext_AllQuestidRefs_Resolved()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            Assert.IsNotNull(qc.Questid_Ref);
        }
    }

    [Test]
    public void Questcontext_AllConditionidRefs_Resolved()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            Assert.AreEqual(qc.Conditionid.Count, qc.Conditionid_Ref.Count);
            foreach (var condRef in qc.Conditionid_Ref)
            {
                Assert.IsNotNull(condRef);
            }
        }
    }

    [Test]
    public void Questcontext_Reward_ValidItemIds()
    {
        foreach (var qc in tables.TbQuestcontext.DataList)
        {
            foreach (var reward in qc.Reward)
            {
                Assert.GreaterOrEqual(reward.Count, 2);
                var item = tables.TbItem.GetOrDefault(reward[0]);
                Assert.IsNotNull(item, $"Questcontext {qc.Id} Reward 引用了不存在的 Item {reward[0]}");
            }
        }
    }

    [Test]
    public void SimulateQuestFlow_StartToFinish()
    {
        var quest = tables.TbQuest.Get(1);
        Assert.IsNotNull(quest.Conditionid_Ref);

        var stages = tables.TbQuestcontext.DataList
            .Where(qc => qc.Questid == quest.Id)
            .OrderBy(qc => qc.Id).ToList();
        Assert.AreEqual(4, stages.Count);

        var stage1 = stages[0];
        Assert.AreEqual("去找拳王对话", stage1.Des);
        Assert.AreEqual(2, stage1.Conditionid_Ref.Count);
        Assert.IsNotNull(stage1.Conditionid_Ref[0]);
        Assert.IsNotNull(stage1.Conditionid_Ref[1]);

        var stage2 = stages[1];
        Assert.AreEqual("击败拳王", stage2.Des);
        Assert.AreEqual(1, stage2.Conditionid_Ref.Count);
        Assert.AreEqual(500, stage2.Reward[0][1]);
    }
}
