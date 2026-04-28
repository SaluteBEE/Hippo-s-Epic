using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

class ManagerRegistryTests
{
    private List<GameObject> _cleanup = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        ManagerRegistry.Clear();
        foreach (var go in _cleanup)
        {
            if (go != null)
                Object.DestroyImmediate(go);
        }
        _cleanup.Clear();
    }

    [Test]
    public void GetOrCreate_NotRegistered_CreatesNewInstance()
    {
        var mgr = ManagerRegistry.GetOrCreate<DialogManager>();
        _cleanup.Add(mgr.gameObject);

        Assert.IsNotNull(mgr);
        Assert.AreEqual($"[{nameof(DialogManager)}]", mgr.gameObject.name);
    }

    [Test]
    public void GetOrCreate_AlreadyRegistered_ReturnsSameInstance()
    {
        var go = new GameObject("Existing");
        _cleanup.Add(go);
        var existing = go.AddComponent<DialogManager>();
        ManagerRegistry.Register(existing);

        var result = ManagerRegistry.GetOrCreate<DialogManager>();

        Assert.AreSame(existing, result);
    }

    [Test]
    public void GetOrCreate_ThenGet_ReturnsSameInstance()
    {
        var created = ManagerRegistry.GetOrCreate<DialogManager>();
        _cleanup.Add(created.gameObject);
        var fetched = ManagerRegistry.Get<DialogManager>();

        Assert.AreSame(created, fetched);
    }

    [Test]
    public void GetOrCreate_Twice_ReturnsSameInstance()
    {
        var first = ManagerRegistry.GetOrCreate<DialogManager>();
        _cleanup.Add(first.gameObject);
        var second = ManagerRegistry.GetOrCreate<DialogManager>();

        Assert.AreSame(first, second);
    }

    [Test]
    public void Register_Null_DoesNotCrash()
    {
        Assert.DoesNotThrow(() => ManagerRegistry.Register<DialogManager>(null));
        Assert.IsNull(ManagerRegistry.Get<DialogManager>());
    }

    [Test]
    public void Unregister_RemovesEntry()
    {
        var mgr = ManagerRegistry.GetOrCreate<DialogManager>();
        _cleanup.Add(mgr.gameObject);
        ManagerRegistry.Unregister<DialogManager>();

        Assert.IsNull(ManagerRegistry.Get<DialogManager>());
    }

    [Test]
    public void Clear_RemovesAll()
    {
        var mgr = ManagerRegistry.GetOrCreate<DialogManager>();
        _cleanup.Add(mgr.gameObject);

        ManagerRegistry.Clear();

        Assert.IsNull(ManagerRegistry.Get<DialogManager>());
    }

    [Test]
    public void TryGet_Registered_ReturnsTrue()
    {
        var mgr = ManagerRegistry.GetOrCreate<DialogManager>();
        _cleanup.Add(mgr.gameObject);

        bool found = ManagerRegistry.TryGet(out DialogManager result);

        Assert.IsTrue(found);
        Assert.AreSame(mgr, result);
    }

    [Test]
    public void TryGet_NotRegistered_ReturnsFalse()
    {
        bool found = ManagerRegistry.TryGet(out DialogManager result);

        Assert.IsFalse(found);
        Assert.IsNull(result);
    }

    [Test]
    public void Register_OverwritesExisting()
    {
        var go1 = new GameObject("First");
        _cleanup.Add(go1);
        var first = go1.AddComponent<DialogManager>();
        ManagerRegistry.Register(first);

        var go2 = new GameObject("Second");
        _cleanup.Add(go2);
        var second = go2.AddComponent<DialogManager>();
        ManagerRegistry.Register(second);

        Assert.AreSame(second, ManagerRegistry.Get<DialogManager>(),
            "Register 同一类型应覆盖");
    }
}
