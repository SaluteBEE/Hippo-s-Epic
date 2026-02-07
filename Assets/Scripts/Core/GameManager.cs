using UnityEngine;
using UnityEngine.SceneManagement;

namespace GGJ2026
{
    public enum GameState { MainMenu, Playing, Paused, LevelComplete, GameOver }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State = GameState.MainMenu;

        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private KeyCode skipKey = KeyCode.P;
        
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // 确保启动时 UI 显隐正确（尤其从 MainMenu 开始）
            ApplyStateUI(State);
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            if (Input.GetKeyDown(resetKey))
                ReloadCurrentLevel();
            if (Input.GetKeyDown(skipKey))
                LevelManager.Instance.LoadNextLevel();
        }

        /// <summary>
        /// 统一的状态切换入口：任何地方要改状态都调用它。
        /// </summary>
        public void SetState(GameState newState)
        {
            if (State == newState) return;

            State = newState;
            ApplyStateUI(State);
        }

        /// <summary>
        /// 根据状态应用 UI（这里集中控制 ResetHint 的显隐）
        /// </summary>
        private void ApplyStateUI(GameState state)
        {
            if (state == GameState.MainMenu)
            {
                UIManager.Instance.Open<MainMenuPanel>("UI/MainMenuPanel");
               
            }
            else if (state == GameState.Playing)
            {
               
            }
            else if (state == GameState.Paused)
            {
                
            }
        }

        public void ReloadCurrentLevel()
        {
            int idx = SceneManager.GetActiveScene().buildIndex;
            if (idx == 0) return;

            SceneManager.LoadScene(idx);

            // 重载后通常仍是 Playing；如你有流程，可在加载完成后再 SetState(Playing)
            // SetState(GameState.Playing);
        }
    }
}
