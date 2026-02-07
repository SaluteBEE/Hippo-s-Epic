using UnityEngine;

namespace GGJ2026
{
    /// <summary>
    /// 全局系统入口：负责一次性创建并持久化所有 Manager。
    /// 建议做成一个 Prefab（AppRoot），放在首个场景或用 RuntimeInitialize 自动生成。
    /// </summary>
    public class AppRoot : MonoBehaviour
    {
        public static AppRoot Instance { get; private set; }

        [Header("Managers")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private LevelManager levelManager;

        private void Awake()
        {
            // 防止切换场景或误放重复 AppRoot
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 统一初始化入口（如有需要）
            // 例如：音量读取存档、UI预热、事件总线绑定等
            Initialize();
        }

        private void Initialize()
        {
            // 这里可以保证初始化顺序：
            // Audio -> UI -> Game 或者你需要的任何顺序
            // if (audioManager != null) audioManager.Initialize();
            //if (uiManager != null) uiManager.Initialize();
            //if (gameManager != null) gameManager.Initialize();
        }
    }
}
