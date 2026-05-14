using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Map))]
public class MapEditor : Editor
{
    private SerializedProperty _interactablesProp;

    private void OnEnable()
    {
        _interactablesProp = serializedObject.FindProperty("interactables");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);

        if (GUILayout.Button("重新收集 Interactable", GUILayout.Height(28)))
        {
            CollectAndRefresh();
        }

        EditorGUILayout.Space(4);

        if (_interactablesProp != null && _interactablesProp.arraySize > 0)
        {
            EditorGUILayout.LabelField($"Interactable 列表 ({_interactablesProp.arraySize})", EditorStyles.boldLabel);

            for (int i = 0; i < _interactablesProp.arraySize; i++)
            {
                var element = _interactablesProp.GetArrayElementAtIndex(i);
                var obj = element.objectReferenceValue as Interactable;

                if (obj == null)
                {
                    EditorGUILayout.HelpBox($"[{i}] 引用丢失", MessageType.Error);
                    continue;
                }

                EditorGUILayout.BeginHorizontal("box");

                if (GUILayout.Button("●", GUILayout.Width(22), GUILayout.Height(20)))
                {
                    EditorGUIUtility.PingObject(obj.gameObject);
                }

                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(28));
                EditorGUILayout.ObjectField(obj, typeof(Interactable), true);

                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("未收集到任何 Interactable", MessageType.Info);
        }
    }

    private void CollectAndRefresh()
    {
        var map = (Map)target;
        Undo.RecordObject(map, "Collect Interactables");
        map.CollectInteractables();
        serializedObject.Update();
        EditorUtility.SetDirty(map);
    }

    [MenuItem("Tools/Map/收集所有场景 Map 的 Interactable")]
    private static void CollectAllMapsInScene()
    {
        var maps = Object.FindObjectsOfType<Map>(true);
        int total = 0;
        foreach (var map in maps)
        {
            Undo.RecordObject(map, "Collect Interactables");
            map.CollectInteractables();
            EditorUtility.SetDirty(map);
            total += map.InteractableList.Count;
            Debug.Log($"[MapEditor] {map.name}: 收集到 {map.InteractableList.Count} 个 Interactable");
        }
        Debug.Log($"[MapEditor] 完成，共 {maps.Length} 个 Map，{total} 个 Interactable");
    }
}
