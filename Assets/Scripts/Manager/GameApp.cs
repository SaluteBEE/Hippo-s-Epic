using UnityEngine;

public class GameApp : MonoBehaviour
{
    public static GameApp Instance { get; private set; }

    [Header("Managers")]
    [SerializeField] private UIManager ui;
    [SerializeField] private new AudioManager audio;
    [SerializeField] private SceneController scene;
    [SerializeField] private DataTableManager dataTable;

    [Header("Startup")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public UIManager UI => ui;
    public AudioManager Audio => audio;
    public SceneController Scene => scene;
    public DataTableManager DataTable => dataTable;

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

        StateMachine = new GameStateMachine();

        // 如果 Inspector 没拖，自动从子物体查找
        if (ui == null) ui = GetComponentInChildren<UIManager>(true);
        if (audio == null) audio = GetComponentInChildren<AudioManager>(true);
        if (scene == null) scene = GetComponentInChildren<SceneController>(true);
        if (dataTable == null) dataTable = GetComponentInChildren<DataTableManager>(true);

        dataTable.LoadTables();
    }

    private void Start()
    {
        GoMainMenu();
    }

    private void Update()
    {
        StateMachine.Update();
    }

    public void GoMainMenu()
    {
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