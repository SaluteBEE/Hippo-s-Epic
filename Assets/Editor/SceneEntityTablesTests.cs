#if UNITY_EDITOR
using System.Collections.Generic;
using cfg.cfg.entity;
using cfg.cfg.scene;
using cfg.cfg.item;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

[TestFixture]
public class SceneEntityTablesTests
{
    private DataTableManager _dtm;

    [SetUp]
    public void SetUp()
    {
        CleanupExisting();

        var go = new GameObject("[Test] DataTableManager");
        go.hideFlags = HideFlags.HideAndDontSave;
        _dtm = go.AddComponent<DataTableManager>();
        _dtm.LoadTables();
        ManagerRegistry.Register(_dtm);
    }

    [TearDown]
    public void TearDown()
    {
        SaveManager.Instance.ClearSave();

        if (_dtm != null)
        {
            ManagerRegistry.Unregister<DataTableManager>();
            Object.DestroyImmediate(_dtm.gameObject);
        }

        CleanupTestGameObjects();
    }

    #region TbScene 测试

    [Test]
    public void TbScene_HasExpectedRecordCount()
    {
        Assert.AreEqual(3, _dtm.Tables.TbScene.DataList.Count,
            "场景表应有 3 条记录");
    }

    [Test]
    public void TbScene_GetById_ReturnsCorrectScene()
    {
        var lounge = _dtm.Tables.TbScene[1];
        Assert.IsNotNull(lounge);
        Assert.AreEqual("Scene_Staff_Lounge", lounge.Name);
        Assert.AreEqual("员工休息室", lounge.DisplayName);

        var gym = _dtm.Tables.TbScene[2];
        Assert.IsNotNull(gym);
        Assert.AreEqual("Scene_Underground_Boxing_Gym", gym.Name);
        Assert.AreEqual("地下拳击馆", gym.DisplayName);

        var dialogue = _dtm.Tables.TbScene[3];
        Assert.IsNotNull(dialogue);
        Assert.AreEqual("Scene_Dialogue", dialogue.Name);
        Assert.AreEqual("对话场景", dialogue.DisplayName);
    }

    [Test]
    public void TbScene_GetOrDefault_InvalidId_ReturnsNull()
    {
        var result = _dtm.Tables.TbScene.GetOrDefault(999);
        Assert.IsNull(result, "不存在的 id 应返回 null");
    }

    [Test]
    public void TbScene_AllRecordsHaveNonEmptyRequiredFields()
    {
        foreach (var scene in _dtm.Tables.TbScene.DataList)
        {
            Assert.IsFalse(string.IsNullOrEmpty(scene.Name),
                $"Scene id={scene.Id} 的 Name 不应为空");
            Assert.IsFalse(string.IsNullOrEmpty(scene.DisplayName),
                $"Scene id={scene.Id} 的 DisplayName 不应为空");
            Assert.IsTrue(scene.Id > 0,
                $"Scene Name={scene.Name} 的 Id 应为正整数");
        }
    }

    [Test]
    public void TbScene_DataMapAndDataListConsistent()
    {
        var map = _dtm.Tables.TbScene.DataMap;
        var list = _dtm.Tables.TbScene.DataList;

        Assert.AreEqual(map.Count, list.Count,
            "DataMap 和 DataList 的记录数应一致");

        foreach (var scene in list)
        {
            Assert.IsTrue(map.ContainsKey(scene.Id),
                $"DataList 中的 scene id={scene.Id} 应存在于 DataMap");
            Assert.AreSame(scene, map[scene.Id],
                $"DataMap 和 DataList 中同一 id 的对象应是同一引用");
        }
    }

    #endregion

    #region TbEntity 测试

    [Test]
    public void TbEntity_HasExpectedRecordCount()
    {
        Assert.AreEqual(4, _dtm.Tables.TbEntity.DataList.Count,
            "实体表应有 4 条记录");
    }

    [Test]
    public void TbEntity_GetById_ReturnsCorrectEntity()
    {
        var npc = _dtm.Tables.TbEntity["gym_main_npc_01"];
        Assert.IsNotNull(npc);
        Assert.AreEqual(2, npc.SceneId);
        Assert.AreEqual(0, npc.Type);
        Assert.AreEqual(1, npc.DataId);
        Assert.AreEqual(0, npc.InitialState);

        var item = _dtm.Tables.TbEntity["lounge_item_01"];
        Assert.IsNotNull(item);
        Assert.AreEqual(1, item.SceneId);
        Assert.AreEqual(1, item.Type);
        Assert.AreEqual(1, item.DataId);
        Assert.AreEqual(0, item.InitialState);

        var npc2 = _dtm.Tables.TbEntity["lounge_npc_01"];
        Assert.IsNotNull(npc2);
        Assert.AreEqual(1, npc2.SceneId);
        Assert.AreEqual(0, npc2.Type);
        Assert.AreEqual(2, npc2.DataId);
        Assert.AreEqual(0, npc2.InitialState);

        var door = _dtm.Tables.TbEntity["gym_door_locked"];
        Assert.IsNotNull(door);
        Assert.AreEqual(2, door.SceneId);
        Assert.AreEqual(3, door.Type);
        Assert.AreEqual(0, door.DataId);
        Assert.AreEqual(1, door.InitialState);
    }

    [Test]
    public void TbEntity_GetOrDefault_InvalidId_ReturnsNull()
    {
        var result = _dtm.Tables.TbEntity.GetOrDefault("nonexistent_entity");
        Assert.IsNull(result, "不存在的 id 应返回 null");
    }

    [Test]
    public void TbEntity_AllRecordsHaveValidType()
    {
        foreach (var entity in _dtm.Tables.TbEntity.DataList)
        {
            Assert.IsTrue(entity.Type >= 0 && entity.Type <= 3,
                $"Entity id={entity.Id} 的 Type={entity.Type} 应在 [0,3] 范围内");
        }
    }

    [Test]
    public void TbEntity_AllRecordsHaveValidInitialState()
    {
        foreach (var entity in _dtm.Tables.TbEntity.DataList)
        {
            Assert.IsTrue(entity.InitialState >= 0 && entity.InitialState <= 2,
                $"Entity id={entity.Id} 的 InitialState={entity.InitialState} 应在 [0,2] 范围内");
        }
    }

    [Test]
    public void TbEntity_AllRecordsHaveNonEmptyId()
    {
        foreach (var entity in _dtm.Tables.TbEntity.DataList)
        {
            Assert.IsFalse(string.IsNullOrEmpty(entity.Id),
                "Entity 的 Id 不应为空");
        }
    }

    [Test]
    public void TbEntity_DataMapAndDataListConsistent()
    {
        var map = _dtm.Tables.TbEntity.DataMap;
        var list = _dtm.Tables.TbEntity.DataList;

        Assert.AreEqual(map.Count, list.Count,
            "DataMap 和 DataList 的记录数应一致");

        foreach (var entity in list)
        {
            Assert.IsTrue(map.ContainsKey(entity.Id),
                $"DataList 中的 entity id={entity.Id} 应存在于 DataMap");
            Assert.AreSame(entity, map[entity.Id],
                $"DataMap 和 DataList 中同一 id 的对象应是同一引用");
        }
    }

    #endregion

    #region 跨表引用测试

    [Test]
    public void TbEntity_SceneIdRef_IsResolved()
    {
        foreach (var entity in _dtm.Tables.TbEntity.DataList)
        {
            Assert.IsNotNull(entity.SceneId_Ref,
                $"Entity id={entity.Id} 的 SceneId_Ref 应已解析 (sceneId={entity.SceneId})");
            Assert.AreEqual(entity.SceneId, entity.SceneId_Ref.Id,
                $"Entity id={entity.Id} 的 SceneId_Ref.Id 应等于 SceneId");
        }
    }

    [Test]
    public void TbEntity_GymEntitiesReferenceGymScene()
    {
        var npc = _dtm.Tables.TbEntity["gym_main_npc_01"];
        Assert.AreEqual("Scene_Underground_Boxing_Gym", npc.SceneId_Ref.Name);

        var door = _dtm.Tables.TbEntity["gym_door_locked"];
        Assert.AreEqual("Scene_Underground_Boxing_Gym", door.SceneId_Ref.Name);
    }

    [Test]
    public void TbEntity_LoungeEntitiesReferenceLoungeScene()
    {
        var item = _dtm.Tables.TbEntity["lounge_item_01"];
        Assert.AreEqual("Scene_Staff_Lounge", item.SceneId_Ref.Name);

        var npc = _dtm.Tables.TbEntity["lounge_npc_01"];
        Assert.AreEqual("Scene_Staff_Lounge", npc.SceneId_Ref.Name);
    }

    #endregion

    #region EntityConfigLoader 集成测试

    [Test]
    public void GetFinalState_NoSavedState_ReturnsInitialState()
    {
        SaveManager.Instance.ClearSave();

        var entity = _dtm.Tables.TbEntity["gym_main_npc_01"];
        int state = EntityConfigLoader.GetFinalState(entity);
        Assert.AreEqual(entity.InitialState, state,
            "无存档时应返回 InitialState");
    }

    [Test]
    public void GetFinalState_HasSavedState_ReturnsSavedState()
    {
        SaveManager.Instance.ClearSave();
        SaveManager.Instance.SetEntityState("gym_main_npc_01", 1);

        var entity = _dtm.Tables.TbEntity["gym_main_npc_01"];
        int state = EntityConfigLoader.GetFinalState(entity);
        Assert.AreEqual(1, state,
            "有存档时应返回存档状态而非 InitialState");
    }

    [Test]
    public void GetFinalState_SavedStateOverridesNonZeroInitial()
    {
        SaveManager.Instance.ClearSave();
        SaveManager.Instance.SetEntityState("gym_door_locked", 0);

        var entity = _dtm.Tables.TbEntity["gym_door_locked"];
        Assert.AreEqual(1, entity.InitialState, "gym_door_locked 初始状态应为 1(Disabled)");

        int state = EntityConfigLoader.GetFinalState(entity);
        Assert.AreEqual(0, state,
            "存档状态应覆盖非零 InitialState");
    }

    [Test]
    public void ApplyState_Normal_ActivatesGameObject()
    {
        var go = new GameObject("[Test] NormalEntity");
        go.SetActive(false);
        var collider = go.AddComponent<SimpleInteractionObject>();

        EntityConfigLoader.ApplyState(collider, EntityConfigLoader.StateNormal);

        Assert.IsTrue(go.activeSelf,
            "StateNormal 应激活 GameObject");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ApplyState_Disabled_SetsExecutedAndStaysActive()
    {
        var go = new GameObject("[Test] DisabledEntity");
        var simple = go.AddComponent<SimpleInteractionObject>();

        EntityConfigLoader.ApplyState(simple, EntityConfigLoader.StateDisabled);

        Assert.IsTrue(go.activeSelf,
            "StateDisabled 应保持 GameObject 激活");
        Assert.IsTrue(simple.HasBeenExecuted,
            "StateDisabled 应设置 HasBeenExecuted=true");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ApplyState_Hidden_DeactivatesGameObject()
    {
        var go = new GameObject("[Test] HiddenEntity");
        go.SetActive(true);
        var collider = go.AddComponent<SimpleInteractionObject>();

        EntityConfigLoader.ApplyState(collider, EntityConfigLoader.StateHidden);

        Assert.IsFalse(go.activeSelf,
            "StateHidden 应停用 GameObject");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void OnEntityInteracted_UpdatesSaveManager()
    {
        SaveManager.Instance.ClearSave();

        EntityConfigLoader.OnEntityInteracted("gym_main_npc_01", 1);

        Assert.IsTrue(SaveManager.Instance.HasEntityState("gym_main_npc_01"),
            "交互后 SaveManager 应有该实体记录");
        Assert.AreEqual(1, SaveManager.Instance.GetEntityState("gym_main_npc_01"),
            "交互后状态应正确");
    }

    [Test]
    public void GetEntityConfig_ReturnsCorrectEntity()
    {
        var entity = EntityConfigLoader.GetEntityConfig("gym_main_npc_01");
        Assert.IsNotNull(entity);
        Assert.AreEqual(0, entity.Type);
        Assert.AreEqual(2, entity.SceneId);
    }

    [Test]
    public void GetEntityConfig_NotFound_ReturnsNull()
    {
        var entity = EntityConfigLoader.GetEntityConfig("nonexistent");
        Assert.IsNull(entity);
    }

    #endregion

    #region SaveManager 实体状态测试

    [Test]
    public void SaveManager_SetAndGetEntityState()
    {
        SaveManager.Instance.ClearSave();

        SaveManager.Instance.SetEntityState("test_a", 1);
        SaveManager.Instance.SetEntityState("test_b", 2);

        Assert.AreEqual(1, SaveManager.Instance.GetEntityState("test_a"));
        Assert.AreEqual(2, SaveManager.Instance.GetEntityState("test_b"));
        Assert.AreEqual(-1, SaveManager.Instance.GetEntityState("test_c"),
            "未设置的状态应返回 -1");
    }

    [Test]
    public void SaveManager_SaveAndLoadPersist()
    {
        SaveManager.Instance.ClearSave();

        SaveManager.Instance.SetEntityState("gym_main_npc_01", 1);
        SaveManager.Instance.SetEntityState("lounge_item_01", 2);
        SaveManager.Instance.Save();

        SaveManager.Instance.Load();

        Assert.AreEqual(1, SaveManager.Instance.GetEntityState("gym_main_npc_01"));
        Assert.AreEqual(2, SaveManager.Instance.GetEntityState("lounge_item_01"));
    }

    [Test]
    public void SaveManager_ClearSave_RemovesAll()
    {
        SaveManager.Instance.SetEntityState("test_x", 5);
        SaveManager.Instance.Save();

        SaveManager.Instance.ClearSave();

        Assert.IsFalse(SaveManager.Instance.HasEntityState("test_x"));
        Assert.AreEqual(-1, SaveManager.Instance.GetEntityState("test_x"));
    }

    #endregion

    private static void CleanupExisting()
    {
        var existing = ManagerRegistry.Get<DataTableManager>();
        if (existing != null)
        {
            ManagerRegistry.Unregister<DataTableManager>();
            Object.DestroyImmediate(existing.gameObject);
        }
    }

    private static void CleanupTestGameObjects()
    {
        var all = Object.FindObjectsOfType<GameObject>();
        foreach (var go in all)
        {
            if (go.name.StartsWith("[Test]"))
                Object.DestroyImmediate(go);
        }
    }
}
#endif
