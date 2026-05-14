#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class EntitySystemTestRunner
{
    [MenuItem("Tools/InteractableTest/TestSaveManager")]
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

    [MenuItem("Tools/InteractableTest/CreateAndInit")]
    public static void TestCreateSceneObjectsAndInit()
    {
        CleanupTestObjects();

        var parent = new GameObject("[Test] TestMap");
        parent.transform.position = Vector3.zero;

        CreateTestInteractable("TestNPC", parent.transform,
            new InteractionPhase
            {
                state = 0,
                hintText = "按 E 对话",
                buttons = new System.Collections.Generic.List<ButtonOption>
                {
                    new ButtonOption
                    {
                        buttonText = "E",
                        type = InteractionType.Dialogue,
                        dataId = 1001001,
                        transitionToState = 1
                    }
                },
                canRepeat = false
            },
            new InteractionPhase
            {
                state = 1,
                hintText = "......",
                buttons = new System.Collections.Generic.List<ButtonOption>
                {
                    new ButtonOption
                    {
                        buttonText = "好吧",
                        type = InteractionType.HintOnly
                    }
                },
                canRepeat = true
            });

        CreateTestInteractable("TestNoConfig", parent.transform);

        Debug.Log("[Test] 已创建测试交互物体");

        SaveManager.Instance.ClearSave();
        InteractableManager.InitializeScene();

        Debug.Log("[Test] InteractableManager 初始化完成，查看上方日志验证匹配结果");
    }

    [MenuItem("Tools/InteractableTest/Cleanup")]
    public static void CleanupAll()
    {
        CleanupTestObjects();
        SaveManager.Instance.ClearSave();
        Debug.Log("[Test] 清理完成");
    }

    private static void CreateTestInteractable(string name, Transform parent, params InteractionPhase[] phases)
    {
        var go = new GameObject(name);
        go.transform.parent = parent;
        go.AddComponent<BoxCollider2D>();
        var interactable = go.AddComponent<Interactable>();

        if (phases == null || phases.Length == 0)
            return;

        var so = new SerializedObject(interactable);
        var phasesProp = so.FindProperty("phases");
        phasesProp.arraySize = phases.Length;
        for (int i = 0; i < phases.Length; i++)
        {
            var element = phasesProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("state").intValue = phases[i].state;
            element.FindPropertyRelative("hintText").stringValue = phases[i].hintText;
            element.FindPropertyRelative("canRepeat").boolValue = phases[i].canRepeat;
            element.FindPropertyRelative("hideAfterExecute").boolValue = phases[i].hideAfterExecute;
            element.FindPropertyRelative("destroySelf").boolValue = phases[i].destroySelf;
            element.FindPropertyRelative("deactivateSelf").boolValue = phases[i].deactivateSelf;

            var buttonsProp = element.FindPropertyRelative("buttons");
            buttonsProp.arraySize = phases[i].buttons.Count;
            for (int j = 0; j < phases[i].buttons.Count; j++)
            {
                var btn = buttonsProp.GetArrayElementAtIndex(j);
                var src = phases[i].buttons[j];
                btn.FindPropertyRelative("buttonText").stringValue = src.buttonText;
                btn.FindPropertyRelative("type").intValue = (int)src.type;
                btn.FindPropertyRelative("dataId").intValue = src.dataId;
                btn.FindPropertyRelative("param1").stringValue = src.param1 ?? "";
                btn.FindPropertyRelative("param2").stringValue = src.param2 ?? "";
                btn.FindPropertyRelative("transitionToState").intValue = src.transitionToState;
            }
        }

        so.ApplyModifiedProperties();
    }

    private static void CleanupTestObjects()
    {
        var obj = GameObject.Find("[Test] TestMap");
        if (obj != null) Object.DestroyImmediate(obj);
    }
}
#endif