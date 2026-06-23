using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class GameApp : MonoBehaviour
{
    public static GameApp Instance { get; private set; }

    private static string _pendingMapName;

    public static void SetPendingMap(string mapName)
    {
        _pendingMapName = mapName;
    }

    public static string ConsumePendingMap()
    {
        string name = _pendingMapName;
        _pendingMapName = null;
        return name;
    }

    [Header("Managers")]
    [SerializeField] private UIManager ui;
    [SerializeField] private new AudioManager audio;
    [SerializeField] private SceneController scene;
    [SerializeField] private DataTableManager dataTable;
    [SerializeField] private InputManager inputManager;

    [Header("Startup")]
    [SerializeField] private string mainMenuSceneName = "Scene_Init";
    [SerializeField] private SceneMapId startMap = SceneMapId.StaffLounge;
    [SerializeField] private LaunchConfig launchConfig;

    [Header("对话测试")]
    [SerializeField] private bool enableDialogTest;
    [SerializeField] private int testDialogId = 1001001;
    [SerializeField] private bool autoStartTest;

    public SceneMapId StartMap => startMap;

    public GameStateMachine StateMachine { get; private set; }

    #region Dialog Test

    private static readonly int[] TestDialogIds = { 1001001, 100, 200, 300, 400, 500, 600, 9000001 };
    private static readonly string[] TestDialogNames =
    {
        "1001001: 原始对话（选项循环）",
        "100: 线性对话（自动推进）",
        "200: 循环选项（可回退）",
        "300: 嵌套选项（多层分支）",
        "400: 纯旁白（无角色）",
        "500: 物品获得测试",
        "600: ItemBox长度测试",
        "9000001: 条件选项测试"
    };

    private int _testIndex;
    private string _testStatus = "";
    private bool _testGuiMinimized;
    private string _testSearchInput = "";
    private bool _testSearchFocused;
    private GUIStyle _testLabelStyle;
    private GUIStyle _testRichStyle;
    private GUIStyle _testFieldStyle;
    private GUIStyle _testBtnStyle;
    private Texture2D _testBgTex;
    private Texture2D _testBtnBgTex;
    private DialogManager _testDialogManager;

    public bool IsDialogTestEnabled => enableDialogTest;

    public void InitDialogTest()
    {
        if (!enableDialogTest) return;

        _testDialogManager = ManagerRegistry.Get<DialogManager>();
        if (_testDialogManager == null) return;

        _testDialogManager.OnOptions += OnTestOptions;
        _testDialogManager.OnDialogEnded += OnTestDialogEnded;

        _testIndex = Array.IndexOf(TestDialogIds, testDialogId);
        if (_testIndex < 0) _testIndex = 0;
        testDialogId = TestDialogIds[_testIndex];

        Debug.Log($"[GameApp] 对话测试已启用, 起始ID={testDialogId}, autoStart={autoStartTest}");

        if (autoStartTest)
            RunTestDialog();
    }

    public void RunTestDialog()
    {
        if (_testDialogManager == null) return;

        if (_testDialogManager.State == DialogState.Playing)
            _testDialogManager.ForceEndDialog();

        _testStatus = "对话进行中...";

        var uiMgr = ManagerRegistry.Get<UIManager>();
        if (uiMgr != null)
            uiMgr.Open<DialogWindow>(testDialogId);
        else
            _testDialogManager.StartDialog(testDialogId);
    }

    public void ShutdownDialogTest()
    {
        if (_testDialogManager != null)
        {
            _testDialogManager.OnOptions -= OnTestOptions;
            _testDialogManager.OnDialogEnded -= OnTestDialogEnded;
            _testDialogManager = null;
        }
        enableDialogTest = false;
    }

    private void UpdateDialogTest()
    {
    }

    private void SwitchTestDialog(int delta)
    {
        _testIndex = (_testIndex + delta + TestDialogIds.Length) % TestDialogIds.Length;
        testDialogId = TestDialogIds[_testIndex];
        _testStatus = $"切换到: {TestDialogNames[_testIndex]}";
        RunTestDialog();
    }

    private void OnTestOptions(List<OptionInfo> options)
    {
        _testStatus = "等待选择：";
        for (int i = 0; i < options.Count; i++)
            _testStatus += $"\n  [{i + 1}] {options[i].Text}";
    }

    private void OnTestDialogEnded()
    {
        _testStatus = "对话结束。Space=重开, Tab/箭头=切换, F1=开关";
    }

    private void DrawDialogTestGUI()
    {
        if (!enableDialogTest || _testDialogManager == null) return;

        if (_testLabelStyle == null)
        {
            _testLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            _testRichStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16, wordWrap = true };
            _testFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 14 };
            _testBtnBgTex = new Texture2D(1, 1);
            _testBtnBgTex.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.3f, 0.92f));
            _testBtnBgTex.Apply();
            _testBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                normal = { textColor = Color.white, background = _testBtnBgTex },
                hover = { textColor = Color.white, background = _testBtnBgTex }
            };
        }

        if (_testGuiMinimized)
        {
            if (GUI.Button(new Rect(10, 10, 100, 30), "展开测试面板", _testBtnStyle))
                _testGuiMinimized = false;
            return;
        }

        var area = new Rect(10, 10, 700, 200);
        if (_testBgTex == null)
        {
            _testBgTex = new Texture2D(1, 1);
            _testBgTex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.15f, 0.92f));
            _testBgTex.Apply();
        }
        GUI.DrawTexture(area, _testBgTex);

        GUILayout.BeginArea(area);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"<b>[{TestDialogNames[_testIndex]}]</b>", new GUIStyle(_testRichStyle) { fontSize = 16 });
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("收起", GUILayout.Width(50)))
        {
            _testGuiMinimized = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(2);
        GUILayout.Label($"状态: {_testDialogManager.State}  |  {_testStatus}", _testLabelStyle);
        GUILayout.Space(2);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("重新开始", GUILayout.Width(80)))
            RunTestDialog();
        if (GUILayout.Button("上一个", GUILayout.Width(60)))
            SwitchTestDialog(-1);
        if (GUILayout.Button("下一个", GUILayout.Width(60)))
            SwitchTestDialog(1);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("关闭测试", GUILayout.Width(70)))
            ShutdownDialogTest();
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        GUILayout.Label("跳转ID:", _testLabelStyle, GUILayout.Width(60));
        GUI.SetNextControlName("TestDialogSearch");
        _testSearchInput = GUILayout.TextField(_testSearchInput, _testFieldStyle, GUILayout.Width(120));
        _testSearchFocused = GUI.GetNameOfFocusedControl() == "TestDialogSearch";
        if (GUILayout.Button("开始", GUILayout.Width(60)))
            TryJumpTestDialog();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (Event.current.isKey && Event.current.keyCode == KeyCode.Return
            && GUI.GetNameOfFocusedControl() == "TestDialogSearch")
        {
            TryJumpTestDialog();
        }
    }

    private void TryJumpTestDialog()
    {
        if (!int.TryParse(_testSearchInput, out int id)) return;

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return;

        var dialog = tables.TbDialog.GetOrDefault(id);
        if (dialog != null)
        {
            testDialogId = id;
            _testIndex = Array.IndexOf(TestDialogIds, id);
            if (_testIndex < 0) _testIndex = 0;
            _testStatus = $"已跳转到对话 {id}";
            RunTestDialog();
            _testSearchInput = "";
            GUI.FocusControl("");
        }
        else
        {
            _testStatus = $"Dialog {id} 不存在！";
        }
    }

    #endregion

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

        if (inputManager == null) inputManager = GetComponentInChildren<InputManager>(true);
        if (inputManager == null)
        {
            var go = new GameObject("[InputManager]");
            inputManager = go.AddComponent<InputManager>();
        }
        ManagerRegistry.Register(inputManager);

        StateMachine.OnStateChanged += OnGameStateChanged;
    }

    private void Start()
    {
        SaveManager.Instance.Load();

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

        InitBag();
        InitEquip();
        InitQuest();

        ConditionSystem.Instance.Initialize();
        QuestManager.Instance.StartConditionListening();

        StartCoroutine(IconLoader.Init());

        InitDialogTest();
        InitAnimationTest();
        InitBagTest();

        GoMainMenu();
    }

    private void InitBag()
    {
        var bag = BagManager.Instance;
        if (bag.AllItems.Count == 0)
        {
            bag.InitFromConfig();
        }
        else
        {
            Debug.Log($"[GameApp] 背包从存档恢复: {bag.AllItems.Count} 个物品");
        }
    }

    private void InitEquip()
    {
        var equip = EquipManager.Instance;
        if (equip.Equips.Count == 0)
        {
            Debug.Log("[GameApp] 装备: 无已装备物品");
        }
        else
        {
            Debug.Log($"[GameApp] 装备从存档恢复: {equip.Equips.Count} 件装备");
        }
    }

    private void InitQuest()
    {
        if (QuestManager.Instance.AllQuests.Count > 0)
        {
            Debug.Log($"[GameApp] 任务从存档恢复: {QuestManager.Instance.AllQuests.Count} 个任务");
        }
        else
        {
            QuestManager.Instance.Initialize();
        }
    }

    private void Update()
    {
        StateMachine.Update();
        UpdateDialogTest();
    }

    [ContextMenu("清除存档")]
    public void ClearSave()
    {
        SaveManager.Instance.ClearSave();
        Debug.Log("[GameApp] 存档已清除");
    }

    private void OnGUI()
    {
        DrawDialogTestGUI();
        DrawAnimationTestGUI();
        DrawBagTestGUI();
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

    public void GoToMap(SceneMapId mapId)
    {
        SaveManager.Instance.CurrentMapId = (int)mapId;

        string targetScene = mapId.GetSceneName();
        string targetMap = mapId.GetMapName();
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == targetScene && LevelController.Instance != null)
        {
            LevelController.Instance.MapManager.SwitchMap(targetMap);
            return;
        }

        _pendingMapName = targetMap;
        StateMachine.ChangeState(
            new LoadingState(this, targetScene, () => new GameplayState(this))
        );
    }

    public void StartGame(SceneMapId mapId)
    {
        GoToMap(mapId);
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

    private IGameState _stateBeforeDialog;

    public void EnterDialog()
    {
        _stateBeforeDialog = StateMachine.Current;
        StateMachine.ChangeState(new DialogGameState(this));
    }

    public void ExitDialog()
    {
        if (_stateBeforeDialog is GameplayState)
            StateMachine.ChangeState(new GameplayState(this));
        else if (_stateBeforeDialog != null)
            StateMachine.ChangeState(_stateBeforeDialog);
        else
            StateMachine.ChangeState(new GameplayState(this));
    }

    private void OnGameStateChanged(IGameState state)
    {
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (_testDialogManager != null)
        {
            _testDialogManager.OnOptions -= OnTestOptions;
            _testDialogManager.OnDialogEnded -= OnTestDialogEnded;
        }

        SaveManager.Instance.Save();
        IconLoader.ReleaseAll();
        StateMachine.OnStateChanged -= OnGameStateChanged;
    }
}
