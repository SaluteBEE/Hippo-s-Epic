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
        EditorGUILayout.PropertyField(_phasesProp, true);
        serializedObject.ApplyModifiedProperties();
    }
}

[CustomPropertyDrawer(typeof(InteractionPhase))]
public class InteractionPhaseDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var stateProp = property.FindPropertyRelative("state");
        var hintTextProp = property.FindPropertyRelative("hintText");
        var buttonsProp = property.FindPropertyRelative("buttons");
        var canRepeatProp = property.FindPropertyRelative("canRepeat");
        var hideAfterExecuteProp = property.FindPropertyRelative("hideAfterExecute");
        var destroySelfProp = property.FindPropertyRelative("destroySelf");
        var deactivateSelfProp = property.FindPropertyRelative("deactivateSelf");

        EditorGUILayout.LabelField($"Phase {stateProp.intValue}", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(stateProp);

        hintTextProp.stringValue = EditorGUILayout.TextArea(hintTextProp.stringValue, GUILayout.Height(EditorGUIUtility.singleLineHeight * 4));

        EditorGUILayout.PropertyField(buttonsProp, true);

        EditorGUILayout.LabelField("行为", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(canRepeatProp);
        EditorGUILayout.PropertyField(hideAfterExecuteProp);
        EditorGUILayout.PropertyField(destroySelfProp);
        EditorGUILayout.PropertyField(deactivateSelfProp);

        EditorGUI.EndProperty();
    }
}
