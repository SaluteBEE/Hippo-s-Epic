#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EntitySystemTestRunner
{
    private const string TestSceneName = "EntitySystemTest";

    [MenuItem("Tools/EntityTest/LoadLubanData")]
    public static void TestLoadLubanData()
    {
        Debug.Log("[Test] LoadLubanData START");
        var go = new GameObject("[Test] DataTableManager");
        var dtm = go.AddComponent<DataTableManager>();
        dtm.LoadTables();
        ManagerRegistry.Register(dtm);

        var tables = dtm.Tables;
        Debug.Log($"[Test] TbScene 记录数: {tables.TbScene.DataList.Count}");
        foreach (var s in tables.TbScene.DataList)
            Debug.Log($"  Scene: id={s.Id} name={s.Name} display={s.DisplayName}");

        Debug.Log($"[Test] TbEntity 记录数: {tables.TbEntity.DataList.Count}");
        foreach (var e in tables.TbEntity.DataList)
            Debug.Log($"  Entity: id={e.Id} sceneId={e.SceneId} type={e.Type} dataId={e.DataId} initialState={e.InitialState}");

        Debug.Log("[Test] Luban 数据加载 ✓");
    }

    [MenuItem("Tools/EntityTest/TestSaveManager")]
    public static void TestSaveManager()
    {
        Debug.Log("[Test] TestSaveManager START");
        SaveManager.Instance.Load();
        Debug.Log($"[Test] 加载存档完成，记录数: {SaveManager.Instance.EntityStates.Count}");

        SaveManager.Instance.SetEntityState("test_entity_01", 2);
        SaveManager.Instance.SetEntityState("test_entity_02", 1);
        SaveManager.Instance.Save();
        Debug.Log("[Test] 写入 2 条测试状态");

        SaveManager.Instance.Load();
        int s1 = SaveManager.Instance.GetEntityState("test_entity_01");
        int s2 = SaveManager.Instance.GetEntityState("test_entity_02");
        Debug.Log($"[Test] 读取验证: test_entity_01={s1}, test_entity_02={s2}");

        if (s1 == 2 && s2 == 1)
            Debug.Log("[Test] SaveManager 存读 ✓");
        else
            Debug.LogError("[Test] SaveManager 验证失败!");

        SaveManager.Instance.ClearSave();
    }

    [MenuItem("Tools/EntityTest/CreateAndInit")]
    public static void TestCreateSceneObjectsAndInit()
    {
        var dtm = ManagerRegistry.Get<DataTableManager>();
        if (dtm == null || !dtm.IsLoaded)
        {
            TestLoadLubanData();
        }

        CleanupTestObjects();

        var parent = new GameObject("[Test] Entities");

        CreateTestInteractable("TestNPC", "gym_main_npc_01", parent.transform);
        CreateTestInteractable("TestItem", "lounge_item_01", parent.transform);
        CreateTestInteractable("TestNPC2", "lounge_npc_01", parent.transform);
        CreateTestInteractable("TestDoor", "gym_door_locked", parent.transform);
        CreateTestInteractable("TestNoConfig", "", parent.transform);

        Debug.Log("[Test] 已创建 5 个测试交互物体");

        SaveManager.Instance.ClearSave();
        EntityConfigLoader.InitializeScene(SceneManager.GetActiveScene().name);

        Debug.Log("[Test] EntityConfigLoader 初始化完成，查看上方日志验证匹配结果");
    }

    [MenuItem("Tools/EntityTest/InteractionSave")]
    public static void TestInteractionAndSave()
    {
        EntityConfigLoader.OnEntityInteracted("gym_main_npc_01", 1);
        EntityConfigLoader.OnEntityInteracted("lounge_item_01", 2);
        SaveManager.Instance.Save();

        Debug.Log("[Test] 模拟交互完成并保存，查看 persistentDataPath/save.json");
        Debug.Log($"[Test] 存档路径: {Application.persistentDataPath}/save.json");
    }

    [MenuItem("Tools/EntityTest/StateRestore")]
    public static void TestStateRestore()
    {
        var dtm = ManagerRegistry.Get<DataTableManager>();
        if (dtm == null || !dtm.IsLoaded)
        {
            TestLoadLubanData();
        }

        CleanupTestObjects();

        var parent = new GameObject("[Test] Entities");
        CreateTestInteractable("TestNPC", "gym_main_npc_01", parent.transform);
        CreateTestInteractable("TestItem", "lounge_item_01", parent.transform);
        CreateTestInteractable("TestNPC2", "lounge_npc_01", parent.transform);
        CreateTestInteractable("TestDoor", "gym_door_locked", parent.transform);

        EntityConfigLoader.InitializeScene(SceneManager.GetActiveScene().name);

        Debug.Log("[Test] 如果之前执行了步骤 4，gym_main_npc_01 应为 StateDisabled(1)，lounge_item_01 应为 StateHidden(2)");
        Debug.Log("[Test] 状态恢复测试完成 ✓");
    }

    [MenuItem("Tools/EntityTest/Cleanup")]
    public static void CleanupAll()
    {
        CleanupTestObjects();
        SaveManager.Instance.ClearSave();

        var dtm = ManagerRegistry.Get<DataTableManager>();
        if (dtm != null)
        {
            ManagerRegistry.Unregister<DataTableManager>();
            Object.DestroyImmediate(dtm.gameObject);
        }

        Debug.Log("[Test] 清理完成");
    }

    private static void CreateTestInteractable(string name, string entityId, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.parent = parent;
        go.AddComponent<BoxCollider2D>();
        var pickup = go.AddComponent<ItemPickup>();
        if (!string.IsNullOrEmpty(entityId))
        {
            var so = new SerializedObject(pickup);
            var prop = so.FindProperty("entityId");
            if (prop != null)
            {
                prop.stringValue = entityId;
                so.ApplyModifiedProperties();
            }
        }
    }

    private static void CleanupTestObjects()
    {
        var obj = GameObject.Find("[Test] Entities");
        if (obj != null) Object.DestroyImmediate(obj);
    }
}
#endif
