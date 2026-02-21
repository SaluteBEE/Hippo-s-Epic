using UnityEngine;

namespace Core.Level2D
{
    public static class LevelManager
    {
        private static LevelController _levelController;
        public static LevelController LevelController
        {
            get => _levelController;
        }

        public static void InstantiateLevelController()
        {
            if (_levelController == null)
            {
                _levelController = LevelController.InstantiateLevelController();
                return;
            }
            Debug.Log("Level2D: Level Controller is already initialized.");
        }
    }
}