using UnityEditor;
using UnityEngine;

namespace Core.Level2D
{
    public static class LevelManagerTool
    {
        [MenuItem("Tools/Level2D/Initialize Level", true)]
        public static bool InitializeLevel_Validate()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem("Tools/Level2D/Initialize Level")]
        public static void InitializeLevel()
        {
            LevelManager.InstantiateLevelController();
        }
    }
}