using System.Collections.Generic;
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
            DrawPhase(phase, i);
        }

        if (GUILayout.Button("+ 添加 Phase"))
        {
            _phasesProp.InsertArrayElementAtIndex(_phasesProp.arraySize);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPhase(SerializedProperty phase, int index)
    {
        var stateProp = phase.FindPropertyRelative("state");
        var hintTextProp = phase.FindPropertyRelative("hintText");
        var buttonsProp = phase.FindPropertyRelative("buttons");
        var conditionTriggersProp = phase.FindPropertyRelative("conditionTriggers");
        var activeChildNamesProp = phase.FindPropertyRelative("activeChildNames");
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
        EditorGUILayout.LabelField("子节点样式", EditorStyles.boldLabel);
        DrawActiveChildNames(activeChildNamesProp);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("行为", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(canRepeatProp);
        EditorGUILayout.PropertyField(hideAfterExecuteProp);
        EditorGUILayout.PropertyField(destroySelfProp);
        EditorGUILayout.PropertyField(deactivateSelfProp);

        EditorGUI.indentLevel--;

        EditorGUILayout.Space(4);
    }

    private void DrawActiveChildNames(SerializedProperty activeChildNamesProp)
    {
        var interactable = (Interactable)target;
        if (interactable == null)
        {
            EditorGUILayout.PropertyField(activeChildNamesProp, true);
            return;
        }

        var styleRoot = interactable.transform.Find("Style");
        var childNames = new List<string>();
        if (styleRoot != null)
        {
            for (int i = 0; i < styleRoot.childCount; i++)
                childNames.Add(styleRoot.GetChild(i).name);
        }

        if (childNames.Count == 0)
        {
            EditorGUILayout.HelpBox("Style 节点下没有子节点", MessageType.Info);
            EditorGUILayout.PropertyField(activeChildNamesProp, true);
            return;
        }

        var selected = new HashSet<string>();
        for (int i = 0; i < activeChildNamesProp.arraySize; i++)
            selected.Add(activeChildNamesProp.GetArrayElementAtIndex(i).stringValue);

        EditorGUI.indentLevel++;
        foreach (var childName in childNames)
        {
            bool wasSelected = selected.Contains(childName);
            bool isSelected = EditorGUILayout.ToggleLeft(childName, wasSelected);

            if (isSelected != wasSelected)
            {
                if (isSelected)
                {
                    activeChildNamesProp.InsertArrayElementAtIndex(activeChildNamesProp.arraySize);
                    activeChildNamesProp.GetArrayElementAtIndex(activeChildNamesProp.arraySize - 1).stringValue = childName;
                }
                else
                {
                    for (int i = 0; i < activeChildNamesProp.arraySize; i++)
                    {
                        if (activeChildNamesProp.GetArrayElementAtIndex(i).stringValue == childName)
                        {
                            activeChildNamesProp.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }
            }
        }
        EditorGUI.indentLevel--;
    }
}
