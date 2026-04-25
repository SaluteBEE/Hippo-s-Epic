using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using cfg.cfg.animationstate;
using cfg.cfg.slotstate;

public class AnimationStateManagerIndexTests
{
    private AnimationStateManager CreateManagerWithTestTables()
    {
        var go = new UnityEngine.GameObject("TestMgr_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        var mgr = go.AddComponent<AnimationStateManager>();
        var tables = CreateTestTables();
        mgr.SetTables(tables);
        return mgr;
    }

    [Test]
    public void SetTables_BuildsAnimStateIndex()
    {
        var mgr = CreateManagerWithTestTables();
        try
        {
            var index = GetFieldValue<Dictionary<(int, string, int), Animationstate>>(mgr, "_animStateIndex");
            Assert.IsNotNull(index);
            Assert.Greater(index.Count, 0, "animStateIndex 不应为空");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void SetTables_BuildsSlotStateByIdIndex()
    {
        var mgr = CreateManagerWithTestTables();
        try
        {
            var index = GetFieldValue<Dictionary<int, Slotstate>>(mgr, "_slotStateByIdIndex");
            Assert.IsNotNull(index);
            Assert.Greater(index.Count, 0, "slotStateByIdIndex 不应为空");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void FindAnimState_ReturnsCorrectEntry()
    {
        var mgr = CreateManagerWithTestTables();
        try
        {
            var animStateIndex = GetFieldValue<Dictionary<(int, string, int), Animationstate>>(mgr, "_animStateIndex");
            Assert.Greater(animStateIndex.Count, 0, "animStateIndex 应有数据");

            var firstKey = new List<(int, string, int)>(animStateIndex.Keys)[0];
            var result = InvokeFindAnimState(mgr, firstKey.Item1, firstKey.Item2, firstKey.Item3);
            Assert.IsNotNull(result, $"key=({firstKey}) 应存在");
            Assert.AreEqual(animStateIndex[firstKey], result);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void FindAnimState_ReturnsNullForMissing()
    {
        var mgr = CreateManagerWithTestTables();
        try
        {
            var result = InvokeFindAnimState(mgr, 999, "nonexistent", 999);
            Assert.IsNull(result, "不存在的 key 应返回 null");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void FindAnimState_FallbackToPrefabTypeZero()
    {
        var mgr = CreateManagerWithTestTables();
        try
        {
            var result = InvokeFindAnimState(mgr, 1, "idle", 0);
            Assert.IsNull(result, "测试数据中 prefabType=0 不存在");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void SetTables_NullTables_ClearsIndices()
    {
        var mgr = CreateManagerWithTestTables();
        try
        {
            mgr.SetTables(null);
            var animIndex = GetFieldValue<Dictionary<(int, string, int), Animationstate>>(mgr, "_animStateIndex");
            var byIdIndex = GetFieldValue<Dictionary<int, Slotstate>>(mgr, "_slotStateByIdIndex");

            Assert.AreEqual(0, animIndex.Count);
            Assert.AreEqual(0, byIdIndex.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    private static T GetFieldValue<T>(object obj, string fieldName)
    {
        var field = typeof(AnimationStateManager).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"字段 {fieldName} 应存在");
        return (T)field.GetValue(obj);
    }

    private static Animationstate InvokeFindAnimState(AnimationStateManager mgr, int personId, string stateName, int prefabType)
    {
        var method = typeof(AnimationStateManager).GetMethod("FindAnimState", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method);
        return (Animationstate)method.Invoke(mgr, new object[] { personId, stateName, prefabType });
    }

    private static cfg.Tables CreateTestTables()
    {
        var dtObj = new UnityEngine.GameObject("TestDataTable");
        var dt = dtObj.AddComponent<DataTableManager>();
        dt.LoadTables();
        UnityEngine.Object.DestroyImmediate(dtObj);
        return dt.Tables;
    }
}
