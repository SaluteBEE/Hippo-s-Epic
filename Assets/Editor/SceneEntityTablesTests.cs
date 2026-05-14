#if UNITY_EDITOR
using System.Collections.Generic;
using cfg.cfg.entity;
using cfg.cfg.scene;
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

    #region InteractableManager 集成测试

    private const string TestMapName = "TestMap";

    private static GameObject EnsureTestMap()
    {
        var existing = GameObject.Find("[Test] " + TestMapName);
        if (existing != null) return existing;
        var map = new GameObject("[Test] " + TestMapName);
        return map;
    }

    private static void WriteButtonProperty(SerializedProperty buttonsProp, int index, ButtonOption btn)
    {
        buttonsProp.arraySize = Mathf.Max(buttonsProp.arraySize, index + 1);
        var bp = buttonsProp.GetArrayElementAtIndex(index);
        bp.FindPropertyRelative("buttonText").stringValue = btn.buttonText ?? "";
        bp.FindPropertyRelative("type").intValue = (int)btn.type;
        bp.FindPropertyRelative("dataId").intValue = btn.dataId;
        bp.FindPropertyRelative("param1").stringValue = btn.param1 ?? "";
        bp.FindPropertyRelative("param2").stringValue = btn.param2 ?? "";
        bp.FindPropertyRelative("transitionToState").intValue = btn.transitionToState;
    }

    private static Interactable CreateTestInteractable(string name, params InteractionPhase[] phases)
    {
        var map = EnsureTestMap();
        var go = new GameObject(name);
        go.transform.parent = map.transform;
        go.AddComponent<BoxCollider2D>();
        var interactable = go.AddComponent<Interactable>();

        if (phases == null || phases.Length == 0)
            return interactable;

        var so = new SerializedObject(interactable);
        var phasesProp = so.FindProperty("phases");
        phasesProp.arraySize = phases.Length;
        for (int i = 0; i < phases.Length; i++)
        {
            var element = phasesProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("state").intValue = phases[i].state;
            element.FindPropertyRelative("hintText").stringValue = phases[i].hintText ?? "";
            element.FindPropertyRelative("canRepeat").boolValue = phases[i].canRepeat;
            element.FindPropertyRelative("hideAfterExecute").boolValue = phases[i].hideAfterExecute;
            element.FindPropertyRelative("destroySelf").boolValue = phases[i].destroySelf;
            element.FindPropertyRelative("deactivateSelf").boolValue = phases[i].deactivateSelf;

            var buttonsProp = element.FindPropertyRelative("buttons");
            buttonsProp.arraySize = 0;
            if (phases[i].buttons != null)
            {
                for (int j = 0; j < phases[i].buttons.Count; j++)
                    WriteButtonProperty(buttonsProp, j, phases[i].buttons[j]);
            }
        }

        so.ApplyModifiedProperties();
        return interactable;
    }

    [Test]
    public void InteractableManager_NoSavedState_UsesFirstPhase()
    {
        SaveManager.Instance.ClearSave();

        CreateTestInteractable("NPC",
            new InteractionPhase
            {
                state = 0,
                hintText = "按 E 对话",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "E", type = InteractionType.Dialogue, dataId = 1001001, transitionToState = 1 }
                },
                canRepeat = false
            },
            new InteractionPhase
            {
                state = 1,
                hintText = "......",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "好吧", type = InteractionType.HintOnly, transitionToState = -1 }
                },
                canRepeat = true
            });

        InteractableManager.InitializeScene();

        var interactable = Object.FindObjectsOfType<Interactable>(true)[0];
        Assert.AreEqual(0, interactable.CurrentState, "无存档时应用第一个 phase 的 state");
        Assert.IsNotNull(interactable.CurrentPhase);
        Assert.AreEqual(InteractionType.Dialogue, interactable.CurrentPhase.buttons[0].type);

        CleanupTestGameObjects();
    }

    [Test]
    public void InteractableManager_SavedState_UsesSavedState()
    {
        SaveManager.Instance.ClearSave();
        string eid = $"[Test] {TestMapName}_NPC";
        SaveManager.Instance.SetEntityState(eid, 1);

        CreateTestInteractable("NPC",
            new InteractionPhase
            {
                state = 0,
                hintText = "按 E 对话",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "E", type = InteractionType.Dialogue, dataId = 1001001, transitionToState = 1 }
                },
                canRepeat = false
            },
            new InteractionPhase
            {
                state = 1,
                hintText = "......",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "好吧", type = InteractionType.HintOnly, transitionToState = -1 }
                },
                canRepeat = true
            });

        InteractableManager.InitializeScene();

        var interactable = Object.FindObjectsOfType<Interactable>(true)[0];
        Assert.AreEqual(1, interactable.CurrentState, "有存档时应用存档状态");
        Assert.AreEqual(InteractionType.HintOnly, interactable.CurrentPhase.buttons[0].type);

        CleanupTestGameObjects();
    }

    [Test]
    public void Interactable_ApplyState_DeactivateSelf_DisablesGameObject()
    {
        var map = EnsureTestMap();
        var go = new GameObject("Deactivated");
        go.transform.parent = map.transform;
        var interactable = go.AddComponent<Interactable>();

        var so = new SerializedObject(interactable);
        var phasesProp = so.FindProperty("phases");
        phasesProp.arraySize = 2;

        var phase0 = phasesProp.GetArrayElementAtIndex(0);
        phase0.FindPropertyRelative("state").intValue = 0;
        phase0.FindPropertyRelative("canRepeat").boolValue = false;
        phase0.FindPropertyRelative("destroySelf").boolValue = true;
        var p0Buttons = phase0.FindPropertyRelative("buttons");
        p0Buttons.arraySize = 1;
        var p0Btn0 = p0Buttons.GetArrayElementAtIndex(0);
        p0Btn0.FindPropertyRelative("buttonText").stringValue = "拾取";
        p0Btn0.FindPropertyRelative("type").intValue = (int)InteractionType.Pickup;
        p0Btn0.FindPropertyRelative("transitionToState").intValue = 1;

        var phase1 = phasesProp.GetArrayElementAtIndex(1);
        phase1.FindPropertyRelative("state").intValue = 1;
        phase1.FindPropertyRelative("deactivateSelf").boolValue = true;
        var p1Buttons = phase1.FindPropertyRelative("buttons");
        p1Buttons.arraySize = 1;
        var p1Btn0 = p1Buttons.GetArrayElementAtIndex(0);
        p1Btn0.FindPropertyRelative("buttonText").stringValue = "查看";
        p1Btn0.FindPropertyRelative("type").intValue = (int)InteractionType.HintOnly;

        so.ApplyModifiedProperties();

        interactable.ApplyState(1);

        Assert.IsFalse(go.activeSelf, "deactivateSelf 的 phase 应禁用 GameObject");
        CleanupTestGameObjects();
    }

    [Test]
    public void Interactable_TransitionToState_UpdatesSaveManager()
    {
        SaveManager.Instance.ClearSave();

        CreateTestInteractable("Transition",
            new InteractionPhase
            {
                state = 0,
                hintText = "按 E 对话",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "E", type = InteractionType.Dialogue, dataId = 1001001, transitionToState = 1 }
                },
                canRepeat = false
            },
            new InteractionPhase
            {
                state = 1,
                hintText = "......",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "好吧", type = InteractionType.HintOnly, transitionToState = -1 }
                },
                canRepeat = true
            });

        InteractableManager.InitializeScene();

        var interactable = Object.FindObjectsOfType<Interactable>(true)[0];
        Assert.AreEqual(InteractionType.Dialogue, interactable.CurrentPhase.buttons[0].type);
        Assert.AreEqual(1, interactable.CurrentPhase.buttons[0].transitionToState);

        interactable.OnPlayerEnter();
        interactable.OnPlayerExecute(0);

        string eid = interactable.EntityId;
        Assert.AreEqual(1, SaveManager.Instance.GetEntityState(eid),
            "状态转换应写入 SaveManager");
        Assert.AreEqual(1, interactable.CurrentState);
        Assert.AreEqual(InteractionType.HintOnly, interactable.CurrentPhase.buttons[0].type);

        CleanupTestGameObjects();
    }

    [Test]
    public void Interactable_CanRepeat_AllowsMultipleExecute()
    {
        CreateTestInteractable("Repeat",
            new InteractionPhase
            {
                state = 0,
                hintText = "按 E 对话",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "E", type = InteractionType.Dialogue, dataId = 1001001, transitionToState = 1 }
                },
                canRepeat = false
            },
            new InteractionPhase
            {
                state = 1,
                hintText = "......",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "好吧", type = InteractionType.HintOnly, transitionToState = -1 }
                },
                canRepeat = true
            });

        InteractableManager.InitializeScene();

        var interactable = Object.FindObjectsOfType<Interactable>(true)[0];

        interactable.ApplyState(1);
        Assert.IsTrue(interactable.CurrentPhase.canRepeat);

        interactable.OnPlayerEnter();
        interactable.OnPlayerExecute(0);
        interactable.OnPlayerExecute(0);

        CleanupTestGameObjects();
    }

    [Test]
    public void Interactable_CannotRepeat_BlocksSecondExecute()
    {
        CreateTestInteractable("NoRepeat",
            new InteractionPhase
            {
                state = 0,
                hintText = "按 E 对话",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "E", type = InteractionType.Dialogue, dataId = 1001001, transitionToState = 1 }
                },
                canRepeat = false
            },
            new InteractionPhase
            {
                state = 1,
                hintText = "......",
                buttons = new List<ButtonOption>
                {
                    new ButtonOption { buttonText = "好吧", type = InteractionType.HintOnly, transitionToState = -1 }
                },
                canRepeat = true
            });

        InteractableManager.InitializeScene();

        var interactable = Object.FindObjectsOfType<Interactable>(true)[0];
        Assert.IsFalse(interactable.CurrentPhase.canRepeat);

        interactable.OnPlayerEnter();
        interactable.OnPlayerExecute(0);

        Assert.AreEqual(1, interactable.CurrentState, "首次执行应触发状态转换");

        CleanupTestGameObjects();
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