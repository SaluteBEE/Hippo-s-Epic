using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ApplySlotStateByIdPrefabTypeTests
{
    [Test]
    public void ApplySlotStateById_PrefabTypeZero_AppliesToAll()
    {
        var go = new GameObject("Mgr_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        var mgr = go.AddComponent<AnimationStateManager>();

        try
        {
            var ctrlGo = new GameObject("Ctrl");
            var ctrl = ctrlGo.AddComponent<AnimationController>();
            var slotGo = new GameObject("Slot");
            var slotMgr = slotGo.AddComponent<SlotManager>();

            mgr.RegisterCharacter(1, 1, ctrl, slotMgr);
            mgr.RegisterCharacter(1, 2, ctrl, slotMgr);

            var slotManagers = GetFieldValue<Dictionary<(int, int), SlotManager>>(mgr, "_slotManagers");
            Assert.AreEqual(2, slotManagers.Count);

            UnityEngine.Object.DestroyImmediate(ctrlGo);
            UnityEngine.Object.DestroyImmediate(slotGo);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void IterationSafe_ModifyDuringBroadcast()
    {
        var go = new GameObject("Mgr_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        var mgr = go.AddComponent<AnimationStateManager>();

        try
        {
            var ctrl1Go = new GameObject("Ctrl1");
            var ctrl1 = ctrl1Go.AddComponent<AnimationController>();
            var ctrl2Go = new GameObject("Ctrl2");
            var ctrl2 = ctrl2Go.AddComponent<AnimationController>();

            mgr.RegisterCharacter(1, 1, ctrl1, null);
            mgr.RegisterCharacter(1, 2, ctrl2, null);

            Assert.DoesNotThrow(() =>
            {
                mgr.ApplyState(1, "nonexistent");
            }, "即使查不到状态也不应因遍历字典而抛异常");

            UnityEngine.Object.DestroyImmediate(ctrl1Go);
            UnityEngine.Object.DestroyImmediate(ctrl2Go);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void UnregisterDuringIteration_DoesNotCrash()
    {
        var go = new GameObject("Mgr_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        var mgr = go.AddComponent<AnimationStateManager>();

        try
        {
            for (int i = 1; i <= 3; i++)
            {
                var ctrlGo = new GameObject($"Ctrl{i}");
                var ctrl = ctrlGo.AddComponent<AnimationController>();
                mgr.RegisterCharacter(1, i, ctrl, null);
            }

            mgr.UnregisterCharacter(1, 2);

            Assert.DoesNotThrow(() =>
            {
                mgr.ApplyState(1, "nonexistent");
            });

            var controllers = GetFieldValue<Dictionary<(int, int), AnimationController>>(mgr, "_animControllers");
            Assert.AreEqual(2, controllers.Count);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static T GetFieldValue<T>(object obj, string fieldName)
    {
        var field = typeof(AnimationStateManager).GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"字段 {fieldName} 应存在");
        return (T)field.GetValue(obj);
    }
}
