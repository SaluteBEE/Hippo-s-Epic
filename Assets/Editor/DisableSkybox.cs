using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DisableSkybox
{
    [MenuItem("Tools/DisableSkybox")]
    public static void Disable()
    {
        Undo.RecordObject(RenderSettings.skybox, "Disable Skybox");
        RenderSettings.skybox = null;

        var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in cameras)
        {
            Undo.RecordObject(cam, "Disable Skybox");
            cam.clearFlags = CameraClearFlags.SolidColor;
            Debug.Log($"[DisableSkybox] {cam.name} clearFlags -> SolidColor");
        }

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[DisableSkybox] Done - skybox disabled");
    }
}
