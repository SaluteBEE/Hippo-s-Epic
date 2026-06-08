using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Interactable))]
public class InteractableEditor : Editor
{
    private SerializedProperty _phasesProp;

    private void OnEnable()
    {
        _phasesProp = serializedObject.FindProperty("phases");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        for (int i = 0; i < _phasesProp.arraySize; i++)
        {
            var phase = _phasesProp.GetArrayElementAtIndex(i);
            DrawPhase(phase);
        }

        if (GUILayout.Button("+ 添加 Phase"))
        {
            _phasesProp.InsertArrayElementAtIndex(_phasesProp.arraySize);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPhase(SerializedProperty phase)
    {
        var stateProp = phase.FindPropertyRelative("state");
        var hintTextProp = phase.FindPropertyRelative("hintText");
        var buttonsProp = phase.FindPropertyRelative("buttons");
        var conditionTriggersProp = phase.FindPropertyRelative("conditionTriggers");
        var canRepeatProp = phase.FindPropertyRelative("canRepeat");
        var hideAfterExecuteProp = phase.FindPropertyRelative("hideAfterExecute");
        var destroySelfProp = phase.FindPropertyRelative("destroySelf");
        var deactivateSelfProp = phase.FindPropertyRelative("deactivateSelf");

        phase.isExpanded = EditorGUILayout.Foldout(phase.isExpanded, $"Phase {stateProp.intValue}", true, EditorStyles.boldLabel);
        if (!phase.isExpanded) return;

        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(stateProp);

        EditorGUILayout.LabelField("提示文字");
        hintTextProp.stringValue = EditorGUILayout.TextArea(hintTextProp.stringValue, GUILayout.Height(EditorGUIUtility.singleLineHeight * 4));

        EditorGUILayout.PropertyField(buttonsProp, true);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("条件触发器", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(conditionTriggersProp, true);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("行为", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(canRepeatProp);
        EditorGUILayout.PropertyField(hideAfterExecuteProp);
        EditorGUILayout.PropertyField(destroySelfProp);
        EditorGUILayout.PropertyField(deactivateSelfProp);

        EditorGUI.indentLevel--;

        EditorGUILayout.Space(4);
    }
}
