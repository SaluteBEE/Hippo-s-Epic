using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TryAutoInitTests
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

    private GameObject MakeGo(string name)
    {
        var go = new GameObject(name);
        _cleanup.Add(go);
        return go;
    }

    private static cfg.Tables GetTables(object mgr)
    {
        var field = mgr.GetType().GetField("_tables",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field?.GetValue(mgr) as cfg.Tables;
    }

    private static void InvokeTryAutoInit(object mgr)
    {
        var method = mgr.GetType().GetMethod("TryAutoInit",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, $"{mgr.GetType().Name} 应有 TryAutoInit 方法");
        method.Invoke(mgr, null);
    }

    #region ManagerRegistry 基础

    [Test]
    public void ManagerRegistry_RegisterGet_RoundTrip()
    {
        ManagerRegistry.Clear();
        var go = MakeGo("Test");
        var dtm = go.AddComponent<DataTableManager>();
        ManagerRegistry.Register(dtm);

        var found = ManagerRegistry.Get<DataTableManager>();
        Assert.IsNotNull(found);
        Assert.AreSame(dtm, found);
    }

    [Test]
    public void ManagerRegistry_ManualRegister_CrossType()
    {
        ManagerRegistry.Clear();
        var go = MakeGo("ASM");
        var mgr = go.AddComponent<AnimationStateManager>();
        ManagerRegistry.Register(mgr);

        Assert.IsNotNull(ManagerRegistry.Get<AnimationStateManager>());
    }

    #endregion

    #region AnimationStateManager TryAutoInit 逻辑

    [Test]
    public void AnimStateMgr_TryAutoInit_DtmReady_InitializesTables()
    {
        ManagerRegistry.Clear();
        var dtGo = MakeGo("DTM");
        var dtm = dtGo.AddComponent<DataTableManager>();
        dtm.LoadTables();
        ManagerRegistry.Register(dtm);

        var mgrGo = MakeGo("ASM");
        var mgr = mgrGo.AddComponent<AnimationStateManager>();

        InvokeTryAutoInit(mgr);

        Assert.IsNotNull(GetTables(mgr),
            "DtmReady 时 TryAutoInit 应设置 _tables");
    }

    [Test]
    public void AnimStateMgr_TryAutoInit_NoDtm_TablesNull()
    {
        ManagerRegistry.Clear();
        var mgrGo = MakeGo("ASM");
        var mgr = mgrGo.AddComponent<AnimationStateManager>();

        InvokeTryAutoInit(mgr);

        Assert.IsNull(GetTables(mgr),
            "无 DataTableManager 时 _tables 应为 null");
    }

    [Test]
    public void AnimStateMgr_TryAutoInit_DtmNotLoaded_TablesNull()
    {
        ManagerRegistry.Clear();
        var dtGo = MakeGo("DTM");
        var dtm = dtGo.AddComponent<DataTableManager>();
        ManagerRegistry.Register(dtm);

        var mgrGo = MakeGo("ASM");
        var mgr = mgrGo.AddComponent<AnimationStateManager>();

        InvokeTryAutoInit(mgr);

        Assert.IsNull(GetTables(mgr),
            "DataTableManager 未加载时 _tables 应为 null");
    }

    [Test]
    public void AnimStateMgr_TryAutoInit_AlreadyInitialized_Skips()
    {
        ManagerRegistry.Clear();
        var dtGo = MakeGo("DTM");
        var dtm = dtGo.AddComponent<DataTableManager>();
        dtm.LoadTables();
        ManagerRegistry.Register(dtm);

        var mgrGo = MakeGo("ASM");
        var mgr = mgrGo.AddComponent<AnimationStateManager>();

        InvokeTryAutoInit(mgr);
        var firstTables = GetTables(mgr);

        ManagerRegistry.Unregister<DataTableManager>();
        InvokeTryAutoInit(mgr);
        var secondTables = GetTables(mgr);

        Assert.AreSame(firstTables, secondTables,
            "已初始化后再次 TryAutoInit 应跳过");
    }

    [Test]
    public void AnimStateMgr_SetTables_WorksIndependently()
    {
        ManagerRegistry.Clear();
        var mgrGo = MakeGo("ASM");
        var mgr = mgrGo.AddComponent<AnimationStateManager>();

        var dtGo = MakeGo("DTM");
        var dtm = dtGo.AddComponent<DataTableManager>();
        dtm.LoadTables();

        Assert.IsNull(GetTables(mgr));
        mgr.SetTables(dtm.Tables);
        Assert.IsNotNull(GetTables(mgr));
    }

    #endregion

    #region DialogCharacterManager TryAutoInit 逻辑

    [Test]
    public void CharMgr_TryAutoInit_DtmReady_InitializesTables()
    {
        ManagerRegistry.Clear();
        var dtGo = MakeGo("DTM");
        var dtm = dtGo.AddComponent<DataTableManager>();
        dtm.LoadTables();
        ManagerRegistry.Register(dtm);

        var mgrGo = MakeGo("DCM");
        var mgr = mgrGo.AddComponent<DialogCharacterManager>();

        InvokeTryAutoInit(mgr);

        Assert.IsNotNull(GetTables(mgr),
            "DtmReady 时 TryAutoInit 应设置 _tables");
    }

    [Test]
    public void CharMgr_TryAutoInit_NoDtm_TablesNull()
    {
        ManagerRegistry.Clear();
        var mgrGo = MakeGo("DCM");
        var mgr = mgrGo.AddComponent<DialogCharacterManager>();

        InvokeTryAutoInit(mgr);

        Assert.IsNull(GetTables(mgr));
    }

    [Test]
    public void CharMgr_TryAutoInit_DtmNotLoaded_TablesNull()
    {
        ManagerRegistry.Clear();
        var dtGo = MakeGo("DTM");
        var dtm = dtGo.AddComponent<DataTableManager>();
        ManagerRegistry.Register(dtm);

        var mgrGo = MakeGo("DCM");
        var mgr = mgrGo.AddComponent<DialogCharacterManager>();

        InvokeTryAutoInit(mgr);

        Assert.IsNull(GetTables(mgr));
    }

    #endregion

    #region GetOrCreate 路径

    [Test]
    public void GetOrCreate_DtmReady_ASM_AutoInitsViaReflection()
    {
        ManagerRegistry.Clear();
        var dtGo = MakeGo("DTM");
        var dtm = dtGo.AddComponent<DataTableManager>();
        dtm.LoadTables();
        ManagerRegistry.Register(dtm);

        var asm = ManagerRegistry.GetOrCreate<AnimationStateManager>();
        _cleanup.Add(asm.gameObject);

        InvokeTryAutoInit(asm);

        Assert.IsNotNull(GetTables(asm),
            "GetOrCreate + TryAutoInit 应初始化 _tables");
    }

    [Test]
    public void GetOrCreate_DtmReady_DCM_AutoInitsViaReflection()
    {
        ManagerRegistry.Clear();
        var dtGo = MakeGo("DTM");
        var dtm = dtGo.AddComponent<DataTableManager>();
        dtm.LoadTables();
        ManagerRegistry.Register(dtm);

        var dcm = ManagerRegistry.GetOrCreate<DialogCharacterManager>();
        _cleanup.Add(dcm.gameObject);

        InvokeTryAutoInit(dcm);

        Assert.IsNotNull(GetTables(dcm));
    }

    [Test]
    public void GetOrCreate_DialogMgr_TablesNull_NoTryAutoInit()
    {
        ManagerRegistry.Clear();
        var dtGo = MakeGo("DTM");
        var dtm = dtGo.AddComponent<DataTableManager>();
        dtm.LoadTables();
        ManagerRegistry.Register(dtm);

        var dm = ManagerRegistry.GetOrCreate<DialogManager>();
        _cleanup.Add(dm.gameObject);

        Assert.IsNull(GetTables(dm),
            "DialogManager 没有 TryAutoInit");
    }

    [Test]
    public void GetOrCreate_Twice_SameInstance()
    {
        ManagerRegistry.Clear();
        var first = ManagerRegistry.GetOrCreate<DialogManager>();
        _cleanup.Add(first.gameObject);
        var second = ManagerRegistry.GetOrCreate<DialogManager>();

        Assert.AreSame(first, second);
    }

    #endregion

    #region DialogManager 安全性

    [Test]
    public void DialogManager_StartDialog_NoTables_StateStaysIdle()
    {
        ManagerRegistry.Clear();
        var dm = ManagerRegistry.GetOrCreate<DialogManager>();
        _cleanup.Add(dm.gameObject);

        UnityEngine.TestTools.LogAssert.Expect(LogType.Error, "[DialogManager] Tables 未初始化");
        dm.StartDialog(1);
        Assert.AreEqual(DialogState.Idle, dm.State,
            "_tables 为 null 时 State 应保持 Idle");
    }

    #endregion
}
