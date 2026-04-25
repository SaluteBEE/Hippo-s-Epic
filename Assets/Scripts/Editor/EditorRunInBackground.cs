using UnityEditor;

[InitializeOnLoad]
public static class EditorRunInBackground
{
    static EditorRunInBackground()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            EditorApplication.isPaused = false;
    }
}
