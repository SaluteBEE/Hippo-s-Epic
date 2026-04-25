using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class AnimationStateManagerRegistryTests
{
    private AnimationStateManager CreateManager()
    {
        var go = new GameObject("TestMgr_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        return go.AddComponent<AnimationStateManager>();
    }

    [Test]
    public void RegisterCharacter_StoresController()
    {
        var mgr = CreateManager();
        try
        {
            var ctrlGo = new GameObject("TestCtrl");
            var ctrl = ctrlGo.AddComponent<AnimationController>();
            mgr.RegisterCharacter(1, 1, ctrl, null);

            var controllers = GetFieldValue<Dictionary<(int, int), AnimationController>>(mgr, "_animControllers");
            Assert.IsTrue(controllers.ContainsKey((1, 1)));
            Assert.AreEqual(ctrl, controllers[(1, 1)]);

            UnityEngine.Object.DestroyImmediate(ctrlGo);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void RegisterCharacter_SameKeyOverwrites()
    {
        var mgr = CreateManager();
        try
        {
            var ctrl1Go = new GameObject("Ctrl1");
            var ctrl1 = ctrl1Go.AddComponent<AnimationController>();
            var ctrl2Go = new GameObject("Ctrl2");
            var ctrl2 = ctrl2Go.AddComponent<AnimationController>();

            mgr.RegisterCharacter(1, 1, ctrl1, null);
            mgr.RegisterCharacter(1, 1, ctrl2, null);

            var controllers = GetFieldValue<Dictionary<(int, int), AnimationController>>(mgr, "_animControllers");
            Assert.AreEqual(1, controllers.Count);
            Assert.AreEqual(ctrl2, controllers[(1, 1)]);

            UnityEngine.Object.DestroyImmediate(ctrl1Go);
            UnityEngine.Object.DestroyImmediate(ctrl2Go);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void RegisterCharacter_DifferentPrefabTypes_Coexist()
    {
        var mgr = CreateManager();
        try
        {
            var ctrl1Go = new GameObject("CtrlDialog");
            var ctrl1 = ctrl1Go.AddComponent<AnimationController>();
            var ctrl2Go = new GameObject("CtrlBattle");
            var ctrl2 = ctrl2Go.AddComponent<AnimationController>();

            mgr.RegisterCharacter(1, 1, ctrl1, null);
            mgr.RegisterCharacter(1, 2, ctrl2, null);

            var controllers = GetFieldValue<Dictionary<(int, int), AnimationController>>(mgr, "_animControllers");
            Assert.AreEqual(2, controllers.Count);
            Assert.IsTrue(controllers.ContainsKey((1, 1)));
            Assert.IsTrue(controllers.ContainsKey((1, 2)));

            UnityEngine.Object.DestroyImmediate(ctrl1Go);
            UnityEngine.Object.DestroyImmediate(ctrl2Go);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void UnregisterCharacter_RemovesSpecificPrefabType()
    {
        var mgr = CreateManager();
        try
        {
            var ctrl1Go = new GameObject("Ctrl1");
            var ctrl1 = ctrl1Go.AddComponent<AnimationController>();
            var ctrl2Go = new GameObject("Ctrl2");
            var ctrl2 = ctrl2Go.AddComponent<AnimationController>();

            mgr.RegisterCharacter(1, 1, ctrl1, null);
            mgr.RegisterCharacter(1, 2, ctrl2, null);
            mgr.UnregisterCharacter(1, 1);

            var controllers = GetFieldValue<Dictionary<(int, int), AnimationController>>(mgr, "_animControllers");
            Assert.IsFalse(controllers.ContainsKey((1, 1)));
            Assert.IsTrue(controllers.ContainsKey((1, 2)));

            UnityEngine.Object.DestroyImmediate(ctrl1Go);
            UnityEngine.Object.DestroyImmediate(ctrl2Go);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void UnregisterCharacterAll_RemovesAllPrefabTypes()
    {
        var mgr = CreateManager();
        try
        {
            var ctrl1Go = new GameObject("Ctrl1");
            var ctrl1 = ctrl1Go.AddComponent<AnimationController>();
            var ctrl2Go = new GameObject("Ctrl2");
            var ctrl2 = ctrl2Go.AddComponent<AnimationController>();

            mgr.RegisterCharacter(1, 1, ctrl1, null);
            mgr.RegisterCharacter(1, 2, ctrl2, null);
            mgr.UnregisterCharacterAll(1);

            var controllers = GetFieldValue<Dictionary<(int, int), AnimationController>>(mgr, "_animControllers");
            Assert.AreEqual(0, controllers.Count);

            UnityEngine.Object.DestroyImmediate(ctrl1Go);
            UnityEngine.Object.DestroyImmediate(ctrl2Go);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void RegisterCharacter_NullController_DoesNotAdd()
    {
        var mgr = CreateManager();
        try
        {
            mgr.RegisterCharacter(1, 1, null, null);
            var controllers = GetFieldValue<Dictionary<(int, int), AnimationController>>(mgr, "_animControllers");
            Assert.AreEqual(0, controllers.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void ApplyState_NullTables_DoesNotThrow()
    {
        var mgr = CreateManager();
        try
        {
            Assert.DoesNotThrow(() => mgr.ApplyState(1, "idle"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void ApplyState_PrefabTypeOverload_NullTables_DoesNotThrow()
    {
        var mgr = CreateManager();
        try
        {
            Assert.DoesNotThrow(() => mgr.ApplyState(1, 1, "idle"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void ApplyState_EmptyStateName_DoesNotApply()
    {
        var mgr = CreateManager();
        try
        {
            Assert.DoesNotThrow(() => mgr.ApplyState(1, ""));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    [Test]
    public void ApplyState_NoMatch_DoesNotThrow()
    {
        var mgr = CreateManager();
        try
        {
            Assert.DoesNotThrow(() => mgr.ApplyState(1, "nonexistent"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mgr.gameObject);
        }
    }

    private static T GetFieldValue<T>(object obj, string fieldName)
    {
        var field = typeof(AnimationStateManager).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(field, $"字段 {fieldName} 应存在");
        return (T)field.GetValue(obj);
    }
}
