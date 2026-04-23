using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.IO;
using Luban;

class PlayModeLoadingTests
{
    private cfg.Tables tables;

    [UnityTest]
    public IEnumerator LoadFromStreamingAssets_NoException()
    {
        bool success = false;
        string error = null;
        yield return null;

        try
        {
            tables = new cfg.Tables(tableName =>
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Gen/bin", tableName + ".bytes");
                byte[] bytes = File.ReadAllBytes(path);
                return new ByteBuf(bytes);
            });
            success = true;
        }
        catch (System.Exception e)
        {
            error = e.Message;
        }

        Assert.IsTrue(success, $"加载失败: {error}");
        yield return null;
    }

    [UnityTest]
    public IEnumerator DataTableManager_LoadTables_NoException()
    {
        var go = new GameObject("DataTableManager_Test");
        var manager = go.AddComponent<DataTableManager>();
        yield return null;

        Assert.DoesNotThrow(() => manager.LoadTables());
        Assert.IsTrue(manager.IsLoaded);
        Assert.IsNotNull(manager.Tables);

        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator DataTableManager_AllTablesAccessible()
    {
        var go = new GameObject("DataTableManager_Test");
        var manager = go.AddComponent<DataTableManager>();
        yield return null;

        manager.LoadTables();
        var t = manager.Tables;
        Assert.IsNotNull(t.TbBag);
        Assert.IsNotNull(t.TbBuff);
        Assert.IsNotNull(t.TbPerson);
        Assert.IsNotNull(t.TbCondition);
        Assert.IsNotNull(t.TbDialog);
        Assert.IsNotNull(t.TbDialogcontent);
        Assert.IsNotNull(t.TbItem);
        Assert.IsNotNull(t.TbQuest);
        Assert.IsNotNull(t.TbQuestcontext);

        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator LoadPerformance_CompletesWithinTimeLimit()
    {
        var go = new GameObject("DataTableManager_Test");
        var manager = go.AddComponent<DataTableManager>();
        yield return null;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        manager.LoadTables();
        sw.Stop();

        Assert.Less(sw.ElapsedMilliseconds, 500, "运行时加载应在 500ms 内完成");

        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator ReloadTables_SecondLoadSucceeds()
    {
        var go = new GameObject("DataTableManager_Test");
        var manager = go.AddComponent<DataTableManager>();
        yield return null;

        manager.LoadTables();
        Assert.IsTrue(manager.IsLoaded);
        int firstBagCount = manager.Tables.TbBag.DataList.Count;

        manager.LoadTables();
        Assert.IsTrue(manager.IsLoaded);
        Assert.AreEqual(firstBagCount, manager.Tables.TbBag.DataList.Count);

        Object.DestroyImmediate(go);
    }
}
