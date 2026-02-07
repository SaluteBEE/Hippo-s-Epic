using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GGJ2026
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance;
        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void LoadNextLevel()
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            int nextSceneIndex = currentSceneIndex + 1;

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
                SceneManager.LoadSceneAsync(nextSceneIndex);
            else
                SceneManager.LoadSceneAsync(0);
        }

        public void LoadLevel(int levelIndex) => SceneManager.LoadSceneAsync(levelIndex);

        public void RestartCurrentLevel() => SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
}
