using System.Collections.Generic;
using UnityEngine;

public class AnimationControllerTester : MonoBehaviour
{
    [System.Serializable]
    public class ClipTestButton
    {
        [Header("按钮显示名称")]
        public string buttonLabel = "Play Clip";

        [Header("目标层级名，例如 Base / UpperBody / LowerBody")]
        public string layerName = "Base";

        [Header("动画名，例如 idle / walk / attack")]
        public string clipName = "idle";

        [Header("过渡时间，-1 表示使用 AnimationController 默认值")]
        public float transitionTime = -1f;
    }

    [System.Serializable]
    public class CompositionTestButton
    {
        [Header("按钮显示名称")]
        public string buttonLabel = "Play Composition";

        [Header("组合名")]
        public string compositionName = "Default";
    }

    [Header("引用")]
    [SerializeField] private bool autoFindControllers = true;
    [SerializeField] private bool autoInitializeOnStart = false;

    [Header("界面设置")]
    [SerializeField] private bool showUI = true;
    [SerializeField] private Rect windowRect = new Rect(10, 10, 360, Screen.height - 20);
    [SerializeField] private bool draggableWindow = true;

    [Header("测试按钮 - Clip")]
    [SerializeField] private List<ClipTestButton> clipButtons = new List<ClipTestButton>();

    [Header("测试按钮 - Composition")]
    [SerializeField] private List<CompositionTestButton> compositionButtons = new List<CompositionTestButton>();

    [Header("自动显示控制器中的 Composition")]
    [SerializeField] private bool autoShowCompositionsFromController = true;

    [Header("自动显示 Layer 调试控制")]
    [SerializeField] private bool autoShowLayerControls = true;

    [Header("State Manager 测试")]
    [SerializeField] private int testPersonId = 1;
    [SerializeField] private int testPrefabType = 1;
    [SerializeField] private bool autoInitStateManager = true;

    [Header("调试")]
    [SerializeField] private bool logOperations = true;

    private readonly List<AnimationController> controllers = new List<AnimationController>();
    private AnimationController currentController;
    private int currentControllerIndex = -1;

    private Vector2 scrollPos;
    private readonly Dictionary<string, float> layerWeights = new Dictionary<string, float>();
    private readonly Dictionary<string, float> layerSpeeds = new Dictionary<string, float>();
    private readonly List<string> cachedLayerNames = new List<string>();
    private readonly List<string> cachedCompositionNames = new List<string>();

    private DataTableManager _dataTable;
    private AnimationStateManager _stateMgr;

    private string lastMessage = "Ready";
    private const int WindowId = 246810;

    private SlotManager _cachedSlotMgr;
    private readonly List<string> _cachedSlotNames = new List<string>();
    private Vector2 _slotScrollPos;
    private string _slotStateIdInput = "1";

    private static readonly string[] StateNames =
    {
        "idle", "talk", "angry", "think1", "think2",
        "think3", "throw", "idle2", "idle3"
    };

    private void Awake()
    {
        if (autoFindControllers)
        {
            RefreshControllerList();
        }

        if (autoInitStateManager)
        {
            InitStateManager();
        }
    }

    private void Start()
    {
        if (autoInitializeOnStart)
        {
            foreach (var ctrl in controllers)
            {
                if (ctrl != null && !ctrl.IsInitialized)
                {
                    ctrl.Initialize();
                }
            }
        }

        RefreshCurrentControllerCache();
    }

    private void Reset()
    {
        RefreshControllerList();
    }

    #region StateManager

    private void InitStateManager()
    {
        var dtObj = new GameObject("DataTableManager");
        dtObj.transform.SetParent(transform);
        _dataTable = dtObj.AddComponent<DataTableManager>();
        _dataTable.LoadTables();

        var mgrObj = new GameObject("AnimationStateManager");
        mgrObj.transform.SetParent(transform);
        _stateMgr = mgrObj.AddComponent<AnimationStateManager>();
        _stateMgr.SetTables(_dataTable.Tables);

        LogMessage($"StateManager 初始化完成, Slotstate={_dataTable.Tables.TbSlotstate.DataList.Count}, Animationstate={_dataTable.Tables.TbAnimationstate.DataList.Count}");
    }

    private void RegisterCurrentControllerToStateManager()
    {
        if (_stateMgr == null)
        {
            LogMessage("StateManager 未初始化");
            return;
        }

        if (currentController == null)
        {
            LogMessage("未选择 Controller");
            return;
        }

        var slotMgr = currentController.GetComponent<SlotManager>();
        _stateMgr.RegisterCharacter(testPersonId, testPrefabType, currentController, slotMgr);
        LogMessage($"注册角色{testPersonId} prefabType={testPrefabType}, SlotManager={slotMgr != null}, SlotCount={slotMgr?.SlotCount ?? 0}");
    }

    #endregion

    #region ControllerList

    private void RefreshControllerList()
    {
        controllers.Clear();

        var found = FindObjectsOfType<AnimationController>(true);
        foreach (var ctrl in found)
        {
            if (ctrl != null && !controllers.Contains(ctrl))
            {
                controllers.Add(ctrl);
            }
        }

        if (controllers.Count > 0)
        {
            if (currentController == null || !controllers.Contains(currentController))
            {
                SetCurrentController(0);
            }
            else
            {
                currentControllerIndex = controllers.IndexOf(currentController);
            }
        }
        else
        {
            currentController = null;
            currentControllerIndex = -1;
        }
    }

    private void SetCurrentController(int index)
    {
        if (index < 0 || index >= controllers.Count)
            return;

        currentControllerIndex = index;
        currentController = controllers[index];

        RefreshCurrentControllerCache();

        if (currentController != null)
        {
            LogMessage($"Current Controller: {currentController.gameObject.name}");
        }
    }

    private void RefreshCurrentControllerCache()
    {
        cachedLayerNames.Clear();
        cachedCompositionNames.Clear();
        layerWeights.Clear();
        layerSpeeds.Clear();

        if (currentController == null)
            return;

        if (currentController.IsInitialized)
        {
            var layers = currentController.GetLayerNames();
            cachedLayerNames.AddRange(layers);

            foreach (var layerName in cachedLayerNames)
            {
                layerWeights[layerName] = currentController.GetLayerWeight(layerName);
                layerSpeeds[layerName] = 1f;
            }
        }

        if (autoShowCompositionsFromController)
        {
            var compositions = currentController.GetCompositionNames();
            foreach (var composition in compositions)
            {
                if (!string.IsNullOrEmpty(composition) && !cachedCompositionNames.Contains(composition))
                {
                    cachedCompositionNames.Add(composition);
                }
            }
        }
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        if (!showUI)
        {
            if (GUI.Button(new Rect(10, 10, 120, 30), "显示动画测试"))
            {
                showUI = true;
            }
            return;
        }

        windowRect = GUI.Window(WindowId, windowRect, DrawWindow, "动画调试面板");
    }

    private void DrawWindow(int id)
    {
        float svHeight = windowRect.height - 30;
        if (svHeight < 100) svHeight = Screen.height - 60;

        scrollPos = GUILayout.BeginScrollView(
            scrollPos,
            false,
            true,
            GUILayout.Width(windowRect.width - 16),
            GUILayout.Height(svHeight)
        );

        DrawHeader();
        GUILayout.Space(8);

        DrawControllerSelectionSection();
        GUILayout.Space(8);

        DrawControllerSection();
        GUILayout.Space(8);

        DrawStateManagerSection();
        GUILayout.Space(8);

        DrawCompositionSection();
        GUILayout.Space(8);

        DrawClipSection();
        GUILayout.Space(8);

        DrawLayerControlSection();
        GUILayout.Space(8);

        DrawSlotSection();
        GUILayout.Space(8);

        DrawStatusSection();

        GUILayout.EndScrollView();

        if (draggableWindow)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }
    }

    private void DrawHeader()
    {
        GUILayout.Label("<b>运行时动画调试面板</b>", GetRichLabelStyle());
        GUILayout.Label("支持 AnimationController 调试 + StateManager 表数据测试");
    }

    private void DrawControllerSelectionSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Controller 选择</b>", GetRichLabelStyle());

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("刷新场景 Controller", GUILayout.Height(28)))
        {
            RefreshControllerList();
            RefreshCurrentControllerCache();
            LogMessage($"Found Controllers: {controllers.Count}");
        }

        if (GUILayout.Button(showUI ? "Hide UI" : "Show UI", GUILayout.Height(28)))
        {
            showUI = !showUI;
        }

        GUILayout.EndHorizontal();

        if (controllers.Count == 0)
        {
            GUILayout.Label("当前场景中未找到 AnimationController。");
            GUILayout.EndVertical();
            return;
        }

        GUILayout.Label($"已找到 {controllers.Count} 个 Controller");
        GUILayout.Label($"当前对象: {(currentController != null ? currentController.gameObject.name : "无")}");

        for (int i = 0; i < controllers.Count; i++)
        {
            var ctrl = controllers[i];
            if (ctrl == null) continue;

            string label = ctrl.gameObject.name;
            if (ctrl == currentController)
            {
                label = $"[当前] {label}";
            }

            if (GUILayout.Button(label, GUILayout.Height(24)))
            {
                SetCurrentController(i);
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawControllerSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>当前 Controller</b>", GetRichLabelStyle());

        if (currentController == null)
        {
            GUILayout.Label("未选择 AnimationController。");
            GUILayout.EndVertical();
            return;
        }

        GUILayout.Label($"对象: {currentController.gameObject.name}");
        GUILayout.Label($"已初始化: {(currentController.IsInitialized ? "是" : "否")}");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Initialize", GUILayout.Height(28)))
        {
            currentController.Initialize();
            RefreshCurrentControllerCache();
            LogMessage("Initialize called.");
        }

        if (GUILayout.Button("Refresh Cache", GUILayout.Height(28)))
        {
            RefreshCurrentControllerCache();
            LogMessage("Cache refreshed.");
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Stop All", GUILayout.Height(28)))
        {
            currentController.StopAll();
            LogMessage("StopAll called.");
        }

        if (GUILayout.Button("Init All Controllers", GUILayout.Height(28)))
        {
            foreach (var ctrl in controllers)
            {
                if (ctrl != null && !ctrl.IsInitialized)
                {
                    ctrl.Initialize();
                }
            }

            RefreshCurrentControllerCache();
            LogMessage("All Controllers initialized.");
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawStateManagerSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>State Manager 测试</b>", GetRichLabelStyle());

        if (_stateMgr == null)
        {
            GUILayout.Label("StateManager 未初始化。");

            if (GUILayout.Button("初始化 StateManager", GUILayout.Height(28)))
            {
                InitStateManager();
            }

            GUILayout.EndVertical();
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label($"角色ID: {testPersonId}", GUILayout.Width(70));
        if (GUILayout.Button("-", GUILayout.Width(24), GUILayout.Height(24)))
        {
            testPersonId = Mathf.Max(1, testPersonId - 1);
            LogMessage($"切换到角色{testPersonId}");
        }
        if (GUILayout.Button("+", GUILayout.Width(24), GUILayout.Height(24)))
        {
            testPersonId = Mathf.Min(5, testPersonId + 1);
            LogMessage($"切换到角色{testPersonId}");
        }
        GUILayout.Label($"类型: {testPrefabType}", GUILayout.Width(60));
        if (GUILayout.Button("切换类型", GUILayout.Width(60), GUILayout.Height(24)))
        {
            testPrefabType = testPrefabType == 1 ? 2 : 1;
            LogMessage($"切换到 prefabType={testPrefabType}");
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("注册当前Controller", GUILayout.Height(24)))
        {
            RegisterCurrentControllerToStateManager();
        }

        GUILayout.Space(4);
        GUILayout.Label("状态测试:");

        string[] stateLabels = new string[StateNames.Length];
        var stateActions = new System.Action[StateNames.Length];
        for (int i = 0; i < StateNames.Length; i++)
        {
            string stateName = StateNames[i];
            stateLabels[i] = stateName;
            stateActions[i] = () =>
            {
                _stateMgr.ApplyState(testPersonId, stateName);
                LogMessage($"ApplyState({testPersonId}, {stateName})");
            };
        }
        DrawWrapButtons(stateLabels, stateActions);
        GUILayout.Space(4);

        GUILayout.Space(4);

        if (GUILayout.Button("打印所有 Slotstate 数据", GUILayout.Height(26)))
        {
            foreach (var row in _dataTable.Tables.TbSlotstate.DataList)
            {
                Debug.Log($"[Tester] Slotstate: id={row.Id}, personId={row.Personid}, prefabType={row.Prefabtype}, A={row.A}");
            }
            LogMessage($"打印了 {_dataTable.Tables.TbSlotstate.DataList.Count} 条 Slotstate 记录");
        }

        GUILayout.EndVertical();
    }

    private void DrawCompositionSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Composition 测试</b>", GetRichLabelStyle());

        if (currentController == null)
        {
            GUILayout.Label("无当前 Controller。");
            GUILayout.EndVertical();
            return;
        }

        bool hasAnyButton = false;

        foreach (var item in compositionButtons)
        {
            if (item == null || string.IsNullOrEmpty(item.compositionName))
                continue;

            hasAnyButton = true;

            if (GUILayout.Button($"{item.buttonLabel}  [{item.compositionName}]", GUILayout.Height(28)))
            {
                currentController.PlayComposition(item.compositionName);
                LogMessage($"PlayComposition: {item.compositionName}");
            }
        }

        if (autoShowCompositionsFromController)
        {
            foreach (var compositionName in cachedCompositionNames)
            {
                if (string.IsNullOrEmpty(compositionName))
                    continue;

                hasAnyButton = true;

                if (GUILayout.Button($"Auto: {compositionName}", GUILayout.Height(26)))
                {
                    currentController.PlayComposition(compositionName);
                    LogMessage($"PlayComposition: {compositionName}");
                }
            }
        }

        if (!hasAnyButton)
        {
            GUILayout.Label("没有可用的 Composition 按钮。");
            GUILayout.Label("您可以手动配置 compositionButtons，或检查当前 Controller 的配置。");
        }

        GUILayout.EndVertical();
    }

    private void DrawClipSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Clip 测试</b>", GetRichLabelStyle());

        if (currentController == null)
        {
            GUILayout.Label("无当前 Controller。");
            GUILayout.EndVertical();
            return;
        }

        if (clipButtons == null || clipButtons.Count == 0)
        {
            GUILayout.Label("当前没有配置 Clip 按钮。");
            GUILayout.Label("请在 Inspector 中添加 ClipTestButton 项。");
            GUILayout.EndVertical();
            return;
        }

        foreach (var item in clipButtons)
        {
            if (item == null || string.IsNullOrEmpty(item.layerName) || string.IsNullOrEmpty(item.clipName))
                continue;

            string label = $"{item.buttonLabel}  [{item.layerName} -> {item.clipName}]";

            if (GUILayout.Button(label, GUILayout.Height(28)))
            {
                currentController.PlayClip(item.layerName, item.clipName, item.transitionTime);
                LogMessage($"PlayClip: layer={item.layerName}, clip={item.clipName}, transition={item.transitionTime}");
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawLayerControlSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Layer 调试</b>", GetRichLabelStyle());

        if (!autoShowLayerControls)
        {
            GUILayout.Label("已关闭自动 Layer 调试显示。");
            GUILayout.EndVertical();
            return;
        }

        if (currentController == null)
        {
            GUILayout.Label("无当前 Controller。");
            GUILayout.EndVertical();
            return;
        }

        if (!currentController.IsInitialized)
        {
            GUILayout.Label("当前 Controller 尚未初始化，无法读取 Layer。");
            GUILayout.EndVertical();
            return;
        }

        if (cachedLayerNames.Count == 0)
        {
            GUILayout.Label("没有缓存到 Layer。");
            if (GUILayout.Button("重新读取 Layer", GUILayout.Height(26)))
            {
                RefreshCurrentControllerCache();
            }

            GUILayout.EndVertical();
            return;
        }

        foreach (var layerName in cachedLayerNames)
        {
            DrawSingleLayerControl(layerName);
            GUILayout.Space(6);
        }

        GUILayout.EndVertical();
    }

    private void DrawSingleLayerControl(string layerName)
    {
        GUILayout.BeginVertical("box");

        bool isPlaying = currentController.IsPlaying(layerName);
        float currentTime = currentController.GetAnimationTime(layerName);
        float duration = currentController.GetAnimationDuration(layerName);
        float currentWeight = currentController.GetLayerWeight(layerName);

        GUILayout.Label($"<b>{layerName}</b>", GetRichLabelStyle());
        GUILayout.Label($"状态: {(isPlaying ? "播放中" : "未播放")}");
        GUILayout.Label($"时间: {currentTime:F2} / {duration:F2}");
        GUILayout.Label($"当前权重: {currentWeight:F2}");

        if (!layerWeights.ContainsKey(layerName))
            layerWeights[layerName] = currentWeight;

        if (!layerSpeeds.ContainsKey(layerName))
            layerSpeeds[layerName] = 1f;

        GUILayout.Space(2);

        GUILayout.Label($"Weight: {layerWeights[layerName]:F2}");
        float newWeight = GUILayout.HorizontalSlider(layerWeights[layerName], 0f, 1f);
        if (!Mathf.Approximately(newWeight, layerWeights[layerName]))
        {
            layerWeights[layerName] = newWeight;
            currentController.SetLayerWeight(layerName, newWeight);
        }

        GUILayout.Space(2);

        GUILayout.Label($"Speed: {layerSpeeds[layerName]:F2}");
        float newSpeed = GUILayout.HorizontalSlider(layerSpeeds[layerName], 0f, 3f);
        if (!Mathf.Approximately(newSpeed, layerSpeeds[layerName]))
        {
            layerSpeeds[layerName] = newSpeed;
            currentController.SetAnimationSpeed(layerName, newSpeed);
        }

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Stop Layer", GUILayout.Height(24)))
        {
            currentController.StopLayer(layerName);
            LogMessage($"StopLayer: {layerName}");
        }

        if (GUILayout.Button("Reset Weight", GUILayout.Height(24)))
        {
            layerWeights[layerName] = 1f;
            currentController.SetLayerWeight(layerName, 1f);
            LogMessage($"Reset Weight: {layerName}");
        }

        if (GUILayout.Button("Reset Speed", GUILayout.Height(24)))
        {
            layerSpeeds[layerName] = 1f;
            currentController.SetAnimationSpeed(layerName, 1f);
            LogMessage($"Reset Speed: {layerName}");
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawSlotSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Slot 插槽调试</b>", GetRichLabelStyle());

        if (currentController == null)
        {
            GUILayout.Label("未选择 Controller。");
            GUILayout.EndVertical();
            return;
        }

        SlotManager slotMgr = GetCurrentSlotManager();
        if (slotMgr == null || !slotMgr.IsInitialized)
        {
            GUILayout.Label("当前角色无 SlotManager 或未初始化。");
            GUILayout.EndVertical();
            return;
        }

        int autoPersonId = ResolvePersonIdFromController();
        if (autoPersonId <= 0)
        {
            GUILayout.Label($"无法匹配角色 (gameObject={currentController.gameObject.name})，请手动设置角色ID");
            GUILayout.BeginHorizontal();
            GUILayout.Label("角色ID:", GUILayout.Width(55));
            string idStr = GUILayout.TextField(testPersonId.ToString(), GUILayout.Width(40));
            if (int.TryParse(idStr, out int parsed)) testPersonId = Mathf.Clamp(parsed, 1, 5);
            GUILayout.EndHorizontal();
        }
        else
        {
            testPersonId = autoPersonId;
        }

        GUILayout.Label($"当前角色: personId={testPersonId}  ({currentController.gameObject.name})", GetMiniLabelStyle());

        if (_stateMgr != null)
        {
            bool registered = _stateMgr.IsRegistered(testPersonId, testPrefabType);
            if (!registered)
            {
                if (GUILayout.Button($"注册角色{testPersonId} prefabType={testPrefabType} 到 StateManager", GUILayout.Height(22)))
                {
                    _stateMgr.RegisterCharacter(testPersonId, testPrefabType, currentController, slotMgr);
                    LogMessage($"注册角色{testPersonId} prefabType={testPrefabType}");
                }
            }
        }

        #region Slotstate ID 测试

        GUILayout.Label("Slotstate ID 测试:", GetBoldLabelStyle());

        GUILayout.BeginHorizontal();
        GUILayout.Label("slotstate ID:", GUILayout.Width(85));
        _slotStateIdInput = GUILayout.TextField(_slotStateIdInput, GUILayout.Width(50));
        if (GUILayout.Button("应用", GUILayout.Width(50), GUILayout.Height(22)))
        {
            if (int.TryParse(_slotStateIdInput, out int id) && _stateMgr != null)
            {
                _stateMgr.ApplySlotStateById(testPersonId, id);
                LogMessage($"ApplySlotStateById(personId={testPersonId}, slotstateId={id})");
            }
            else
            {
                LogMessage("无效 ID 或 StateManager 未初始化");
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);

        if (_dataTable != null)
        {
            var slotStates = _dataTable.Tables.TbSlotstate.DataList;
            if (slotStates != null && slotStates.Count > 0)
            {
                GUILayout.Label($"共 {slotStates.Count} 条 Slotstate 记录，点击快速应用:");

                int matched = 0;
                foreach (var ss in slotStates)
                {
                    if (ss.Personid == testPersonId)
                    {
                        matched++;
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button($"ID={ss.Id} prefabType={ss.Prefabtype}", GUILayout.Height(22)))
                        {
                            if (_stateMgr != null)
                            {
                                _stateMgr.ApplySlotStateById(testPersonId, ss.Id);
                                LogMessage($"ApplySlotStateById(personId={testPersonId}, slotstateId={ss.Id})");
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                if (matched == 0)
                    GUILayout.Label($"  当前角色({testPersonId})无匹配 Slotstate");
            }
        }

        GUILayout.Space(6);

        #endregion

        #region 逐插槽开关

        GUILayout.Label("逐插槽开关:", GetBoldLabelStyle());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("全部显示", GUILayout.Height(22)))
        {
            slotMgr.SetAllVisible(true, 0f);
            LogMessage("全部显示");
        }
        if (GUILayout.Button("全部隐藏", GUILayout.Height(22)))
        {
            slotMgr.SetAllVisible(false, 0f);
            LogMessage("全部隐藏");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(2);

        _slotScrollPos = GUILayout.BeginScrollView(_slotScrollPos, GUILayout.Height(300));
        {
            var names = slotMgr.SlotNames;
            foreach (var slotName in names)
            {
                bool visible = slotMgr.IsSlotVisible(slotName);
                string attachName = slotMgr.GetSlotAttachmentName(slotName) ?? "";

                bool newVisible = GUILayout.Toggle(visible, slotName);
                if (newVisible != visible)
                {
                    slotMgr.SetSlotVisible(slotName, newVisible, 0f);
                    LogMessage($"Slot '{slotName}' → {(newVisible ? "显示" : "隐藏")}");
                }

                if (!string.IsNullOrEmpty(attachName))
                {
                    GUILayout.Label($"  └ {attachName}", GetMiniLabelStyle());
                }
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Space(2);
        GUILayout.Label($"共 {slotMgr.SlotCount} 个插槽", GetMiniLabelStyle());

        #endregion

        GUILayout.EndVertical();
    }

    private SlotManager GetCurrentSlotManager()
    {
        if (currentController == null)
            return null;

        SlotManager mgr = currentController.GetComponent<SlotManager>();

        if (mgr != _cachedSlotMgr)
        {
            _cachedSlotMgr = mgr;
            _cachedSlotNames.Clear();
            if (mgr != null && mgr.IsInitialized)
                _cachedSlotNames.AddRange(mgr.SlotNames);
        }

        return mgr;
    }

    private int ResolvePersonIdFromController()
    {
        if (_dataTable == null || currentController == null)
            return -1;

        string goName = currentController.gameObject.name;

        foreach (var person in _dataTable.Tables.TbPerson.DataList)
        {
            if (person.Animconfigs != null)
            {
                foreach (var cfg in person.Animconfigs)
                {
                    if (goName.IndexOf(cfg, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return person.Id;
                }
            }
        }

        return -1;
    }

    private void DrawStatusSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>状态</b>", GetRichLabelStyle());
        GUILayout.Label(lastMessage);
        GUILayout.EndVertical();
    }

    #endregion

    private void LogMessage(string msg)
    {
        lastMessage = msg;

        if (logOperations)
        {
            Debug.Log($"[AnimationTester] {msg}");
        }
    }

    private GUIStyle richLabelStyle;
    private GUIStyle boldLabelStyle;
    private GUIStyle miniLabelStyle;

    private GUIStyle GetRichLabelStyle()
    {
        if (richLabelStyle == null)
        {
            richLabelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true
            };
        }

        return richLabelStyle;
    }

    private GUIStyle GetBoldLabelStyle()
    {
        if (boldLabelStyle == null)
        {
            boldLabelStyle = new GUIStyle(GUI.skin.label);
            boldLabelStyle.fontStyle = FontStyle.Bold;
        }
        return boldLabelStyle;
    }

    private GUIStyle GetMiniLabelStyle()
    {
        if (miniLabelStyle == null)
        {
            miniLabelStyle = new GUIStyle(GUI.skin.label);
            miniLabelStyle.fontSize = 10;
        }
        return miniLabelStyle;
    }

    private void DrawWrapButtons(string[] labels, System.Action[] callbacks, float height = 26f)
    {
        float available = windowRect.width - 36;
        float rowW = 0f;
        bool inRow = false;

        for (int i = 0; i < labels.Length; i++)
        {
            float btnW = GUI.skin.button.CalcSize(new GUIContent(labels[i])).x + 12f;

            if (rowW + btnW > available && inRow)
            {
                GUILayout.EndHorizontal();
                inRow = false;
                rowW = 0f;
            }

            if (!inRow)
            {
                GUILayout.BeginHorizontal();
                inRow = true;
            }

            int idx = i;
            if (GUILayout.Button(labels[i], GUILayout.Height(height), GUILayout.MinWidth(btnW)))
            {
                callbacks[idx]?.Invoke();
            }
            rowW += btnW + 4f;
        }

        if (inRow)
            GUILayout.EndHorizontal();
    }
}
