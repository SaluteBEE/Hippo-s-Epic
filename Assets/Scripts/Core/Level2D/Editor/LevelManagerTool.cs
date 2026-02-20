using Core.Level2D.LevelObjects;
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

        [MenuItem("Tools/Level2D/Instantiate Player Character 2D", true)]
        public static bool InstantiatePlayerCharacter_Validate()
        {
            bool result = false;
            if (LevelManager.LevelController != null)
            {
                if (LevelManager.LevelController.PlayerCharacter == null)
                {
                    result = true;
                }
            }
            return result;
        }

        [MenuItem("Tools/Level2D/Instantiate Player Character 2D")]
        public static void InstantiatePlayerCharacter()
        {
            GameObject playerCharacterPrefab = Resources.Load<GameObject>("Level2D/Player Character 2D");
            LevelManager.LevelController.SetPlayerCharacter(Object.Instantiate(playerCharacterPrefab).GetComponent<PlayerCharacter>());
        }
    }
}