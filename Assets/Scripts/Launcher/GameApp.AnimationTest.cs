using System.Collections.Generic;
using UnityEngine;

public partial class GameApp
{
    #region Animation Test Inspector

    [Header("动画测试")]
    [SerializeField] private bool enableAnimationTest;
    [SerializeField] private int testPersonId = 1;
    [SerializeField] private int testPrefabType = 1;

    #endregion

    #region Animation Test State

    private bool _animTestInited;
    private AnimationStateManager _animStateMgr;
    private DataTableManager _animDataTable;
    private readonly List<AnimationController> _animControllers = new List<AnimationController>();
    private AnimationController _animCurrentCtrl;
    private string _animStatus = "Ready";
    private bool _animGuiMinimized = true;

    private Vector2 _animScrollPos;
    private readonly Dictionary<string, float> _animLayerWeights = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _animLayerSpeeds = new Dictionary<string, float>();
    private readonly List<string> _animCachedLayerNames = new List<string>();
    private readonly List<string> _animCachedCompNames = new List<string>();

    private SlotManager _animSlotMgr;
    private readonly List<string> _animSlotNames = new List<string>();
    private Vector2 _animSlotScrollPos;
    private string _animSlotStateIdInput = "1";

    private GUIStyle _animRichStyle;
    private GUIStyle _animBoldStyle;
    private GUIStyle _animMiniStyle;

    private static readonly string[] AnimStateNames =
    {
        "idle", "talk", "angry", "think1", "think2",
        "think3", "throw", "idle2", "idle3"
    };

    private const int AnimTestWindowId = 246811;

    public bool IsAnimationTestEnabled => enableAnimationTest;

    #endregion

    #region Animation Test Init

    public void InitAnimationTest()
    {
        if (!enableAnimationTest) return;

        _animDataTable = ManagerRegistry.Get<DataTableManager>();
        _animStateMgr = ManagerRegistry.Get<AnimationStateManager>();

        if (_animDataTable == null)
        {
            Debug.LogWarning("[GameApp] 动画测试: DataTableManager 未就绪");
            return;
        }

        RefreshAnimControllers();

        Debug.Log($"[GameApp] 动画测试已启用, 场景Controller={_animControllers.Count}");
    }

    public void ShutdownAnimationTest()
    {
        enableAnimationTest = false;
    }

    #endregion

    #region Controller List

    private bool IsDialogPrefab(GameObject go)
    {
        if (go.name.StartsWith("Dialog_")) return true;
        if (go.layer == LayerMask.NameToLayer("DialogCharacter")) return true;
        return false;
    }

    private void RefreshAnimControllers()
    {
        _animControllers.Clear();

        var found = FindObjectsOfType<AnimationController>(true);
        foreach (var ctrl in found)
        {
            if (ctrl == null || _animControllers.Contains(ctrl)) continue;
            if (IsDialogPrefab(ctrl.gameObject)) continue;
            _animControllers.Add(ctrl);
        }

        if (_animControllers.Count > 0)
        {
            if (_animCurrentCtrl == null || !_animControllers.Contains(_animCurrentCtrl))
                SetAnimCurrentController(0);
        }
        else
        {
            _animCurrentCtrl = null;
        }
    }

    private void SetAnimCurrentController(int index)
    {
        if (index < 0 || index >= _animControllers.Count) return;

        _animCurrentCtrl = _animControllers[index];
        RefreshAnimCache();

        if (_animCurrentCtrl != null)
            AnimLog($"当前Controller: {_animCurrentCtrl.gameObject.name}");
    }

    private void RefreshAnimCache()
    {
        _animCachedLayerNames.Clear();
        _animCachedCompNames.Clear();
        _animLayerWeights.Clear();
        _animLayerSpeeds.Clear();

        if (_animCurrentCtrl == null) return;

        if (_animCurrentCtrl.IsInitialized)
        {
            var layers = _animCurrentCtrl.GetLayerNames();
            _animCachedLayerNames.AddRange(layers);

            foreach (var name in _animCachedLayerNames)
            {
                _animLayerWeights[name] = _animCurrentCtrl.GetLayerWeight(name);
                _animLayerSpeeds[name] = 1f;
            }
        }

        var compositions = _animCurrentCtrl.GetCompositionNames();
        foreach (var comp in compositions)
        {
            var compStr = comp.ToString();
            if (!string.IsNullOrEmpty(compStr) && !_animCachedCompNames.Contains(compStr))
                _animCachedCompNames.Add(compStr);
        }
    }

    #endregion

    #region Animation Test GUI

    private void DrawAnimationTestGUI()
    {
        if (!enableAnimationTest) return;

        if (_animGuiMinimized)
        {
            if (_animTestBtnStyle == null) InitAnimTestStyles();

            if (GUI.Button(new Rect(10, enableDialogTest ? 220 : 10, 100, 30), "动画测试面板", _animTestBtnStyle))
                _animGuiMinimized = false;
            return;
        }

        var rect = new Rect(
            enableDialogTest ? 720 : 10,
            10,
            370,
            Screen.height - 20
        );
        rect = GUI.Window(AnimTestWindowId, rect, DrawAnimTestWindow, "动画调试面板");
    }

    private void DrawAnimTestWindow(int id)
    {
        float svH = 600f;

        _animScrollPos = GUILayout.BeginScrollView(_animScrollPos, false, true, GUILayout.Height(svH));

        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>运行时动画调试</b>", GetAnimRichStyle());
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("收起", GUILayout.Width(50)))
        {
            _animGuiMinimized = true;
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        DrawAnimCtrlSelectSection();
        GUILayout.Space(4);
        DrawAnimCtrlSection();
        GUILayout.Space(4);
        DrawAnimStateManagerSection();
        GUILayout.Space(4);
        DrawAnimCompSection();
        GUILayout.Space(4);
        DrawAnimLayerSection();
        GUILayout.Space(4);
        DrawAnimSlotSection();
        GUILayout.Space(4);

        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>状态</b>", GetAnimRichStyle());
        GUILayout.Label(_animStatus);
        GUILayout.EndVertical();

        GUILayout.EndScrollView();

        GUI.DragWindow(new Rect(0, 0, 10000, 22));
    }

    private void DrawAnimCtrlSelectSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Controller 选择</b>", GetAnimRichStyle());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("刷新场景Controller", GUILayout.Height(26)))
        {
            RefreshAnimControllers();
            AnimLog($"找到 {_animControllers.Count} 个Controller");
        }
        GUILayout.EndHorizontal();

        if (_animControllers.Count == 0)
        {
            GUILayout.Label("场景中未找到 AnimationController。");
            GUILayout.EndVertical();
            return;
        }

        GUILayout.Label($"已找到 {_animControllers.Count} 个, 当前: {(_animCurrentCtrl != null ? _animCurrentCtrl.gameObject.name : "无")}");

        for (int i = 0; i < _animControllers.Count; i++)
        {
            var ctrl = _animControllers[i];
            if (ctrl == null) continue;

            string label = ctrl.gameObject.name;
            if (ctrl == _animCurrentCtrl) label = $"[当前] {label}";

            if (GUILayout.Button(label, GUILayout.Height(22)))
                SetAnimCurrentController(i);
        }

        GUILayout.EndVertical();
    }

    private void DrawAnimCtrlSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>当前 Controller</b>", GetAnimRichStyle());

        if (_animCurrentCtrl == null)
        {
            GUILayout.Label("未选择 Controller。");
            GUILayout.EndVertical();
            return;
        }

        GUILayout.Label($"对象: {_animCurrentCtrl.gameObject.name}");
        GUILayout.Label($"已初始化: {(_animCurrentCtrl.IsInitialized ? "是" : "否")}");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Initialize", GUILayout.Height(26)))
        {
            _animCurrentCtrl.Initialize();
            RefreshAnimCache();
            AnimLog("Initialize");
        }
        if (GUILayout.Button("Refresh Cache", GUILayout.Height(26)))
        {
            RefreshAnimCache();
            AnimLog("Cache refreshed");
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Stop All", GUILayout.Height(26)))
        {
            _animCurrentCtrl.StopAll();
            AnimLog("StopAll");
        }
        if (GUILayout.Button("Init All", GUILayout.Height(26)))
        {
            foreach (var c in _animControllers)
            {
                if (c != null && !c.IsInitialized) c.Initialize();
            }
            RefreshAnimCache();
            AnimLog("All Controllers initialized");
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawAnimStateManagerSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>State Manager 测试</b>", GetAnimRichStyle());

        if (_animStateMgr == null)
        {
            GUILayout.Label("StateManager 未就绪（ManagerRegistry 中未找到）。");
            GUILayout.EndVertical();
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label($"角色ID: {testPersonId}", GUILayout.Width(70));
        if (GUILayout.Button("-", GUILayout.Width(24), GUILayout.Height(24)))
        {
            testPersonId = Mathf.Max(1, testPersonId - 1);
            AnimLog($"角色{testPersonId}");
        }
        if (GUILayout.Button("+", GUILayout.Width(24), GUILayout.Height(24)))
        {
            testPersonId = Mathf.Min(5, testPersonId + 1);
            AnimLog($"角色{testPersonId}");
        }
        GUILayout.Label($"类型: {testPrefabType}", GUILayout.Width(60));
        if (GUILayout.Button("切换类型", GUILayout.Width(60), GUILayout.Height(24)))
        {
            testPrefabType = testPrefabType == 1 ? 2 : 1;
            AnimLog($"prefabType={testPrefabType}");
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("注册当前Controller", GUILayout.Height(22)))
        {
            if (_animCurrentCtrl == null)
            {
                AnimLog("未选择 Controller");
            }
            else
            {
                var slotMgr = _animCurrentCtrl.GetComponent<SlotManager>();
                _animStateMgr.RegisterCharacter(testPersonId, testPrefabType, _animCurrentCtrl, slotMgr);
                AnimLog($"注册角色{testPersonId} prefabType={testPrefabType}, SlotMgr={slotMgr != null}");
            }
        }

        GUILayout.Space(4);
        GUILayout.Label("状态测试:");

        string[] stateLabels = new string[AnimStateNames.Length];
        var stateActions = new System.Action[AnimStateNames.Length];
        for (int i = 0; i < AnimStateNames.Length; i++)
        {
            string stateName = AnimStateNames[i];
            stateLabels[i] = stateName;
            stateActions[i] = () =>
            {
                _animStateMgr.ApplyState(testPersonId, stateName);
                AnimLog($"ApplyState({testPersonId}, {stateName})");
            };
        }
        DrawAnimWrapButtons(stateLabels, stateActions);

        GUILayout.Space(4);
        if (GUILayout.Button("打印所有 Slotstate 数据", GUILayout.Height(24)))
        {
            var tables = ManagerRegistry.GetTables<cfg.Tables>();
            if (tables != null)
            {
                foreach (var row in tables.TbSlotstate.DataList)
                    Debug.Log($"[AnimTest] Slotstate: id={row.Id}, personId={row.Personid}, prefabType={row.Prefabtype}");
                AnimLog($"打印了 {tables.TbSlotstate.DataList.Count} 条 Slotstate");
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawAnimCompSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Composition 测试</b>", GetAnimRichStyle());

        if (_animCurrentCtrl == null)
        {
            GUILayout.Label("无当前 Controller。");
            GUILayout.EndVertical();
            return;
        }

        if (_animCachedCompNames.Count == 0)
        {
            GUILayout.Label("无可用 Composition。");
            GUILayout.EndVertical();
            return;
        }

        foreach (var compName in _animCachedCompNames)
        {
            if (string.IsNullOrEmpty(compName)) continue;

            if (GUILayout.Button(compName, GUILayout.Height(24)))
            {
                _animCurrentCtrl.PlayComposition(compName);
                AnimLog($"PlayComposition: {compName}");
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawAnimLayerSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Layer 调试</b>", GetAnimRichStyle());

        if (_animCurrentCtrl == null || !_animCurrentCtrl.IsInitialized)
        {
            GUILayout.Label("Controller 未选择或未初始化。");
            GUILayout.EndVertical();
            return;
        }

        if (_animCachedLayerNames.Count == 0)
        {
            if (GUILayout.Button("重新读取 Layer", GUILayout.Height(24)))
                RefreshAnimCache();
            GUILayout.EndVertical();
            return;
        }

        foreach (var layerName in _animCachedLayerNames)
        {
            GUILayout.BeginVertical("box");

            bool isPlaying = _animCurrentCtrl.IsPlaying(layerName);
            float time = _animCurrentCtrl.GetAnimationTime(layerName);
            float dur = _animCurrentCtrl.GetAnimationDuration(layerName);
            float weight = _animCurrentCtrl.GetLayerWeight(layerName);

            GUILayout.Label($"<b>{layerName}</b>", GetAnimRichStyle());
            GUILayout.Label($"状态: {(isPlaying ? "播放中" : "未播放")} | {time:F2}/{dur:F2} | weight={weight:F2}");

            if (!_animLayerWeights.ContainsKey(layerName)) _animLayerWeights[layerName] = weight;
            if (!_animLayerSpeeds.ContainsKey(layerName)) _animLayerSpeeds[layerName] = 1f;

            GUILayout.Label($"Weight: {_animLayerWeights[layerName]:F2}");
            float newW = GUILayout.HorizontalSlider(_animLayerWeights[layerName], 0f, 1f);
            if (!Mathf.Approximately(newW, _animLayerWeights[layerName]))
            {
                _animLayerWeights[layerName] = newW;
                _animCurrentCtrl.SetLayerWeight(layerName, newW);
            }

            GUILayout.Label($"Speed: {_animLayerSpeeds[layerName]:F2}");
            float newS = GUILayout.HorizontalSlider(_animLayerSpeeds[layerName], 0f, 3f);
            if (!Mathf.Approximately(newS, _animLayerSpeeds[layerName]))
            {
                _animLayerSpeeds[layerName] = newS;
                _animCurrentCtrl.SetAnimationSpeed(layerName, newS);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop", GUILayout.Height(22)))
            {
                _animCurrentCtrl.StopLayer(layerName);
                AnimLog($"StopLayer: {layerName}");
            }
            if (GUILayout.Button("Reset W", GUILayout.Height(22)))
            {
                _animLayerWeights[layerName] = 1f;
                _animCurrentCtrl.SetLayerWeight(layerName, 1f);
            }
            if (GUILayout.Button("Reset S", GUILayout.Height(22)))
            {
                _animLayerSpeeds[layerName] = 1f;
                _animCurrentCtrl.SetAnimationSpeed(layerName, 1f);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(4);
        }

        GUILayout.EndVertical();
    }

    private void DrawAnimSlotSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Slot 插槽调试</b>", GetAnimRichStyle());

        if (_animCurrentCtrl == null)
        {
            GUILayout.Label("未选择 Controller。");
            GUILayout.EndVertical();
            return;
        }

        var slotMgr = _animCurrentCtrl.GetComponent<SlotManager>();
        if (slotMgr == null || !slotMgr.IsInitialized)
        {
            GUILayout.Label("无 SlotManager 或未初始化。");
            GUILayout.EndVertical();
            return;
        }

        int autoId = ResolveAnimPersonId();
        if (autoId > 0) testPersonId = autoId;

        GUILayout.Label($"角色: personId={testPersonId} ({_animCurrentCtrl.gameObject.name})", GetAnimMiniStyle());

        if (_animStateMgr != null && !_animStateMgr.IsRegistered(testPersonId, testPrefabType))
        {
            if (GUILayout.Button($"注册角色{testPersonId} prefabType={testPrefabType}", GUILayout.Height(22)))
            {
                _animStateMgr.RegisterCharacter(testPersonId, testPrefabType, _animCurrentCtrl, slotMgr);
                AnimLog($"注册角色{testPersonId}");
            }
        }

        GUILayout.Space(2);
        GUILayout.Label("Slotstate ID 测试:", GetAnimBoldStyle());

        GUILayout.BeginHorizontal();
        GUILayout.Label("ID:", GUILayout.Width(30));
        _animSlotStateIdInput = GUILayout.TextField(_animSlotStateIdInput, GUILayout.Width(50));
        if (GUILayout.Button("应用", GUILayout.Width(50), GUILayout.Height(22)))
        {
            if (int.TryParse(_animSlotStateIdInput, out int id) && _animStateMgr != null)
            {
                _animStateMgr.ApplySlotStateById(testPersonId, id);
                AnimLog($"ApplySlotStateById(personId={testPersonId}, id={id})");
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(2);

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables != null)
        {
            var slotStates = tables.TbSlotstate.DataList;
            if (slotStates != null && slotStates.Count > 0)
            {
                GUILayout.Label($"共 {slotStates.Count} 条 Slotstate，匹配角色{testPersonId}:");

                foreach (var ss in slotStates)
                {
                    if (ss.Personid == testPersonId)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button($"ID={ss.Id} prefabType={ss.Prefabtype}", GUILayout.Height(20)))
                        {
                            if (_animStateMgr != null)
                            {
                                _animStateMgr.ApplySlotStateById(testPersonId, ss.Id);
                                AnimLog($"ApplySlotStateById(id={ss.Id})");
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }

        GUILayout.Space(4);
        GUILayout.Label("逐插槽开关:", GetAnimBoldStyle());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("全部显示", GUILayout.Height(22)))
        {
            slotMgr.SetAllVisible(true, 0f);
            AnimLog("全部显示");
        }
        if (GUILayout.Button("全部隐藏", GUILayout.Height(22)))
        {
            slotMgr.SetAllVisible(false, 0f);
            AnimLog("全部隐藏");
        }
        GUILayout.EndHorizontal();

        _animSlotScrollPos = GUILayout.BeginScrollView(_animSlotScrollPos, GUILayout.Height(200));
        foreach (var slotName in slotMgr.SlotNames)
        {
            bool visible = slotMgr.IsSlotVisible(slotName);
            bool newVis = GUILayout.Toggle(visible, slotName);
            if (newVis != visible)
            {
                slotMgr.SetSlotVisible(slotName, newVis, 0f);
                AnimLog($"Slot '{slotName}' → {(newVis ? "显示" : "隐藏")}");
            }
        }
        GUILayout.EndScrollView();

        GUILayout.Label($"共 {slotMgr.SlotCount} 个插槽", GetAnimMiniStyle());

        GUILayout.EndVertical();
    }

    #endregion

    #region Animation Test Helpers

    private int ResolveAnimPersonId()
    {
        if (_animDataTable == null || _animCurrentCtrl == null) return -1;

        string goName = _animCurrentCtrl.gameObject.name;
        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return -1;

        foreach (var person in tables.TbPerson.DataList)
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

    private void AnimLog(string msg)
    {
        _animStatus = msg;
        Debug.Log($"[AnimTest] {msg}");
    }

    private GUIStyle GetAnimRichStyle()
    {
        if (_animRichStyle == null)
            _animRichStyle = new GUIStyle(GUI.skin.label) { richText = true };
        return _animRichStyle;
    }

    private GUIStyle GetAnimBoldStyle()
    {
        if (_animBoldStyle == null)
            _animBoldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        return _animBoldStyle;
    }

    private GUIStyle GetAnimMiniStyle()
    {
        if (_animMiniStyle == null)
            _animMiniStyle = new GUIStyle(GUI.skin.label) { fontSize = 10 };
        return _animMiniStyle;
    }

    private GUIStyle _animTestBtnStyle;

    private void InitAnimTestStyles()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.3f, 0.92f));
        tex.Apply();
        _animTestBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            normal = { textColor = Color.white, background = tex },
            hover = { textColor = Color.white, background = tex }
        };
    }

    private void DrawAnimWrapButtons(string[] labels, System.Action[] callbacks, float height = 24f)
    {
        float available = 340f;
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
                callbacks[idx]?.Invoke();
            rowW += btnW + 4f;
        }

        if (inRow)
            GUILayout.EndHorizontal();
    }

    #endregion
}
