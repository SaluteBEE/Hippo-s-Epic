using NUnit.Framework;

class TableLoadingTests
{
    [Test]
    public void LoadTables_NoException()
    {
        Assert.DoesNotThrow(() => TestDataLoader.LoadTables());
    }

    [Test]
    public void LoadTables_AllTablesNotNull()
    {
        var tables = TestDataLoader.LoadTables();
        Assert.IsNotNull(tables.TbBag);
        Assert.IsNotNull(tables.TbBuff);
        Assert.IsNotNull(tables.TbPerson);
        Assert.IsNotNull(tables.TbCondition);
        Assert.IsNotNull(tables.TbDialog);
        Assert.IsNotNull(tables.TbDialogcontent);
        Assert.IsNotNull(tables.TbItem);
        Assert.IsNotNull(tables.TbQuest);
        Assert.IsNotNull(tables.TbQuestcontext);
    }

    [Test]
    public void LoadTables_AllDataMapNotEmpty()
    {
        var tables = TestDataLoader.LoadTables();
        Assert.Greater(tables.TbBag.DataMap.Count, 0);
        Assert.Greater(tables.TbBuff.DataMap.Count, 0);
        Assert.Greater(tables.TbPerson.DataMap.Count, 0);
        Assert.Greater(tables.TbCondition.DataMap.Count, 0);
        Assert.Greater(tables.TbDialog.DataMap.Count, 0);
        Assert.Greater(tables.TbDialogcontent.DataMap.Count, 0);
        Assert.Greater(tables.TbItem.DataMap.Count, 0);
        Assert.Greater(tables.TbQuest.DataMap.Count, 0);
        Assert.Greater(tables.TbQuestcontext.DataMap.Count, 0);
    }

    [Test]
    public void LoadTables_DataListMatchesDataMap()
    {
        var tables = TestDataLoader.LoadTables();
        Assert.AreEqual(tables.TbBag.DataList.Count, tables.TbBag.DataMap.Count);
        Assert.AreEqual(tables.TbBuff.DataList.Count, tables.TbBuff.DataMap.Count);
        Assert.AreEqual(tables.TbPerson.DataList.Count, tables.TbPerson.DataMap.Count);
        Assert.AreEqual(tables.TbCondition.DataList.Count, tables.TbCondition.DataMap.Count);
        Assert.AreEqual(tables.TbDialog.DataList.Count, tables.TbDialog.DataMap.Count);
        Assert.AreEqual(tables.TbDialogcontent.DataList.Count, tables.TbDialogcontent.DataMap.Count);
        Assert.AreEqual(tables.TbItem.DataList.Count, tables.TbItem.DataMap.Count);
        Assert.AreEqual(tables.TbQuest.DataList.Count, tables.TbQuest.DataMap.Count);
        Assert.AreEqual(tables.TbQuestcontext.DataList.Count, tables.TbQuestcontext.DataMap.Count);
    }

    [Test]
    public void LoadTables_Twice_NoException()
    {
        Assert.DoesNotThrow(() =>
        {
            TestDataLoader.LoadTables();
            TestDataLoader.LoadTables();
        });
    }

    [Test]
    public void LoadPerformance_CompletesQuickly()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        TestDataLoader.LoadTables();
        sw.Stop();
        Assert.Less(sw.ElapsedMilliseconds, 1000, "Binary 加载应在 1 秒内完成");
    }
}
