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

    [Header("对话调试")]
    [SerializeField] private bool debugEnableDialogTest;
    [Tooltip("对话起始ID")]
    [SerializeField] private int debugDialogId = 1001001;
    [Tooltip("启动时自动播放对话")]
    [SerializeField] private bool autoStartTest;

    public SceneMapId StartMap => startMap;

    public GameStateMachine StateMachine { get; private set; }
    public BattleManager BattleManager { get; private set; }

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
    private string _testSearchInput = "";
    private DialogManager _testDialogManager;

    public bool IsDialogTestEnabled => debugEnableDialogTest;

    public void InitDialogTest()
    {
        if (!debugEnableDialogTest) return;

        _testDialogManager = ManagerRegistry.Get<DialogManager>();
        if (_testDialogManager == null) return;

        _testDialogManager.OnOptions += OnTestOptions;
        _testDialogManager.OnDialogEnded += OnTestDialogEnded;

        _testIndex = Array.IndexOf(TestDialogIds, debugDialogId);
        if (_testIndex < 0) _testIndex = 0;
        debugDialogId = TestDialogIds[_testIndex];

        Debug.Log($"[GameApp] 对话测试已启用, 起始ID={debugDialogId}, autoStart={autoStartTest}");

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
            uiMgr.Open<DialogWindow>(debugDialogId);
        else
            _testDialogManager.StartDialog(debugDialogId);
    }

    public void ShutdownDialogTest()
    {
        if (_testDialogManager != null)
        {
            _testDialogManager.OnOptions -= OnTestOptions;
            _testDialogManager.OnDialogEnded -= OnTestDialogEnded;
            _testDialogManager = null;
        }
        debugEnableDialogTest = false;
    }

    private void UpdateDialogTest()
    {
    }

    private void SwitchTestDialog(int delta)
    {
        _testIndex = (_testIndex + delta + TestDialogIds.Length) % TestDialogIds.Length;
        debugDialogId = TestDialogIds[_testIndex];
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

    private void DrawDialogTestContent(GUIStyle lbl, GUIStyle btn, GUIStyle fld, float s)
    {
        if (_testDialogManager == null)
        {
            GUILayout.Label("DialogManager 未就绪", lbl);
            return;
        }

        GUILayout.Label($"[{TestDialogNames[_testIndex]}]", new GUIStyle(lbl) { fontStyle = FontStyle.Bold });
        GUILayout.Label($"状态: {_testDialogManager.State}  |  {_testStatus}", lbl);
        GUILayout.Space(4 * s);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("重新开始", btn, GUILayout.Height(30 * s))) RunTestDialog();
        if (GUILayout.Button("上一个", btn, GUILayout.Height(30 * s))) SwitchTestDialog(-1);
        if (GUILayout.Button("下一个", btn, GUILayout.Height(30 * s))) SwitchTestDialog(1);
        GUILayout.EndHorizontal();

        GUILayout.Space(4 * s);
        GUILayout.BeginHorizontal();
        GUILayout.Label("跳转ID:", lbl, GUILayout.Width(80 * s));
        GUI.SetNextControlName("TestDialogSearch");
        _testSearchInput = GUILayout.TextField(_testSearchInput, fld, GUILayout.Width(160 * s));
        if (GUILayout.Button("开始", btn, GUILayout.Height(30 * s)))
            TryJumpTestDialog();
        GUILayout.EndHorizontal();

        if (Event.current.isKey && Event.current.keyCode == KeyCode.Return
            && GUI.GetNameOfFocusedControl() == "TestDialogSearch")
            TryJumpTestDialog();
    }

    private void TryJumpTestDialog()
    {
        if (!int.TryParse(_testSearchInput, out int id)) return;

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return;

        var dialog = tables.TbDialog.GetOrDefault(id);
        if (dialog != null)
        {
            debugDialogId = id;
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

        SaveManager.Instance.Load();

        InitBag();
        InitEquip();
        InitCharacterStats();
        InitBuffs();
        InitQuest();

        ConditionSystem.Instance.Initialize();
        QuestManager.Instance.StartConditionListening();

        StartCoroutine(IconLoader.Init());

        InitDialogTest();
        InitAnimationTest();
        InitBagTest();
        InitBattleTest();
        SelectFirstEnabledHubTab();

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

    private void InitCharacterStats()
    {
        var statsMgr = CharacterStatsManager.Instance;
        if (statsMgr.PlayerStats == null)
        {
            statsMgr.InitFromConfig();
        }
        else
        {
            Debug.Log($"[GameApp] 角色属性从存档恢复: Lv{statsMgr.PlayerStats.Level}");
        }
    }

    private void InitBuffs()
    {
        var buffMgr = BuffManager.Instance;

        buffMgr.ReapplyEquipBuffs();
        buffMgr.ReapplySkillBuffs();

        Debug.Log($"[GameApp] Buff初始化完成: {buffMgr.ActiveBuffs.Count} 个活跃Buff");
    }

    private void Update()
    {
        UpdateDialogTest();
    }

    [ContextMenu("清除存档")]

    [ContextMenu("清除存档")]
    public void ClearSave()
    {
        SaveManager.Instance.ClearSave();
        Debug.Log("[GameApp] 存档已清除");
    }

    private void OnGUI()
    {
        DrawDebugHub();
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
    private IGameState _stateBeforeBattle;
    private string _returnSceneName; // 战斗结束后返回的场景名

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

    public void EnterBattle(int battleId)
    {
        // 记录当前场景，战斗结束后返回
        _returnSceneName = SceneManager.GetActiveScene().name;
        _stateBeforeBattle = StateMachine.Current;

        // 初始化战斗数据（不依赖场景）
        BattleManager = new BattleManager();
        ManagerRegistry.Register(BattleManager);
        BattleManager.UnlockAllSkillsForTest = IsBattleTestEnabled;
        BattleManager.OnBattleEnd += result => OnBattleFinished(battleId, result);
        BattleManager.InitBattle(battleId);

        // 切换到战斗场景
        var sceneCtrl = ManagerRegistry.Get<SceneController>();
        if (sceneCtrl != null)
        {
            sceneCtrl.LoadScene("Scene_Battle", onLoaded: () => OnBattleSceneLoaded());
        }
        else
        {
            // 无SceneController时直接在当前场景战斗
            OnBattleSceneLoaded();
        }
    }

    /// <summary>
    /// 战斗场景加载完成回调
    /// </summary>
    private void OnBattleSceneLoaded()
    {
        // 切换游戏状态
        StateMachine.ChangeState(new BattleGameState(this));

        // 生成战斗角色，完成后开始战斗
        var stageManager = FindObjectOfType<BattleStageManager>();
        if (stageManager != null && BattleManager != null)
        {
            var allUnits = new List<BattleUnit>();
            allUnits.AddRange(BattleManager.PlayerUnits);
            allUnits.AddRange(BattleManager.EnemyUnits);
            stageManager.SpawnUnits(allUnits, () =>
            {
                Debug.Log("[GameApp] 角色生成完成，开始战斗流程");
                BattleManager.StartBattle();
            });
        }
        else
        {
            Debug.LogWarning("[GameApp] 战斗场景中未找到 BattleStageManager");
            if (BattleManager != null)
                BattleManager.StartBattle();
        }

        // 打开战斗UI
        var ui = ManagerRegistry.Get<UIManager>();
        if (ui != null)
            ui.Open<BattleWindow>();

        // 订阅增援入场事件：新单位进入时生成视觉
        if (BattleManager != null)
        {
            BattleManager.OnNewUnitsReady += (newUnits) =>
            {
                var sm = FindObjectOfType<BattleStageManager>();
                if (sm != null)
                    sm.SpawnNewUnits(newUnits);
            };
        }
    }

    private void OnBattleFinished(int battleId, BattleResult result)
    {
        if (result == BattleResult.Win)
        {
            SaveManager.Instance.RecordCompletedBattle(battleId);
            ConditionSystem.Instance.Notify(ConditionChangeType.Battle);
        }
    }

    /// <summary>
    /// 战斗结果确认后回调，关闭UI并返回原场景
    /// </summary>
    public void OnBattleResultConfirmed()
    {
        // 关闭战斗UI
        var ui = ManagerRegistry.Get<UIManager>();
        if (ui != null)
            ui.Close<BattleWindow>();

        // 返回原场景
        if (!string.IsNullOrEmpty(_returnSceneName) && _returnSceneName != "Scene_Battle")
        {
            var sceneCtrl = ManagerRegistry.Get<SceneController>();
            if (sceneCtrl != null)
            {
                sceneCtrl.LoadScene(_returnSceneName, onLoaded: () => ExitBattle());
                return;
            }
        }

        ExitBattle();
    }

    public void ExitBattle()
    {
        BattleManager = null;
        _returnSceneName = null;

        if (_stateBeforeBattle is GameplayState)
            StateMachine.ChangeState(new GameplayState(this));
        else if (_stateBeforeBattle != null)
            StateMachine.ChangeState(_stateBeforeBattle);
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
