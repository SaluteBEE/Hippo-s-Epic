using UnityEngine;

    public static class LevelManager
    {
        private static LevelController _levelController;
        public static LevelController LevelController
        {
            get => _levelController;
            private set => _levelController = value;
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

        public static void SetLevelController(LevelController levelController)
        {
            LevelController = levelController;
        }
    }
