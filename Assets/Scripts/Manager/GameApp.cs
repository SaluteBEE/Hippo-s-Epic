using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameApp : MonoBehaviour
{
    public static GameApp Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private UIManager ui;
    [SerializeField] private new AudioManager audio;
    [SerializeField] private SceneController scene;
    [SerializeField] private DataTableManager dataTable;

    [Header("Startup")]
    [SerializeField] private string mainMenuSceneName = "Scene_Init";
    [SerializeField] private string gameSceneName = "Level01";
    [SerializeField] private LaunchConfig launchConfig;

    public string GameSceneName => gameSceneName;

    public GameStateMachine StateMachine { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;

        StateMachine = new GameStateMachine();

        if (ui == null) ui = GetComponentInChildren<UIManager>(true);
        if (audio == null) audio = GetComponentInChildren<AudioManager>(true);
        if (scene == null) scene = GetComponentInChildren<SceneController>(true);
        if (dataTable == null)  dataTable = GetComponentInChildren<DataTableManager>(true);

        ManagerRegistry.Register(this);
        ManagerRegistry.Register(ui);
        ManagerRegistry.Register(audio);
        ManagerRegistry.Register(scene);
        ManagerRegistry.Register(dataTable);
    }

    private void Start()
    {
        if (launchConfig != null && launchConfig.LaunchTasks != null && launchConfig.LaunchTasks.Length > 0)
        {
            foreach (var task in launchConfig.LaunchTasks)
            {
                if (task == null) continue;
                task.Execute();
            }
            Debug.Log("[GameApp] 启动任务链完成");
        }
        else
        {
            dataTable.LoadTables();
        }

        GoMainMenu();
    }

    private void Update()
    {
        StateMachine.Update();
    }

    public void GoMainMenu()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            StateMachine.ChangeState(new MainMenuState(this));
            return;
        }

        StateMachine.ChangeState(
            new LoadingState(this, mainMenuSceneName, () => new MainMenuState(this))
        );
    }

    public void StartGame(string sceneName)
    {
        StateMachine.ChangeState(
            new LoadingState(this, sceneName, () => new GameplayState(this))
        );
    }

    public void PauseGame()
    {
        StateMachine.ChangeState(new PauseState(this));
    }

    public void ResumeGame()
    {
        StateMachine.ChangeState(new GameplayState(this));
    }

    public void TogglePause()
    {
        if (StateMachine.Current is PauseState)
            ResumeGame();
        else if (StateMachine.Current is GameplayState)
            PauseGame();
    }
}
