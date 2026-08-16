using System.Collections.Generic;
using UnityEngine;

public partial class GameApp
{
    #region Animation Test Inspector

    [Header("动画调试")]
    [SerializeField] private bool debugEnableAnimationTest;
    [Tooltip("角色Person ID")]
    [SerializeField] private int debugAnimationPersonId = 1;
    [Tooltip("预制体类型(1或2)")]
    [SerializeField] private int debugAnimationPrefabType = 1;

    #endregion

    #region Animation Test State

    private AnimationStateManager _animStateMgr;
    private DataTableManager _animDataTable;
    private readonly List<AnimationController> _animControllers = new List<AnimationController>();
    private AnimationController _animCurrentCtrl;
    private string _animStatus = "Ready";

    private Vector2 _animScrollPos;
    private readonly Dictionary<string, float> _animLayerWeights = new Dictionary<string, float>();
    private readonly Dictionary<string, float> _animLayerSpeeds = new Dictionary<string, float>();
    private readonly List<string> _animCachedLayerNames = new List<string>();
    private readonly List<string> _animCachedCompNames = new List<string>();

    private static readonly string[] AnimStateNames =
    {
        "idle", "talk", "angry", "think1", "think2",
        "think3", "throw", "idle2", "idle3"
    };

    public bool IsAnimationTestEnabled => debugEnableAnimationTest;

    #endregion

    #region Animation Test Init

    public void InitAnimationTest()
    {
        if (!debugEnableAnimationTest) return;
        _animDataTable = ManagerRegistry.Get<DataTableManager>();
        _animStateMgr = ManagerRegistry.Get<AnimationStateManager>();
        if (_animDataTable == null) { Debug.LogWarning("[GameApp] 动画测试: DataTableManager 未就绪"); return; }
        RefreshAnimControllers();
        Debug.Log($"[GameApp] 动画测试已启用, Controller={_animControllers.Count}");
    }

    public void ShutdownAnimationTest() { debugEnableAnimationTest = false; }

    #endregion

    #region Controller List

    private void RefreshAnimControllers()
    {
        _animControllers.Clear();
        var found = FindObjectsOfType<AnimationController>();
        foreach (var ctrl in found)
        {
            if (ctrl == null || _animControllers.Contains(ctrl)) continue;
            _animControllers.Add(ctrl);
        }
        if (_animControllers.Count > 0)
        {
            if (_animCurrentCtrl == null || !_animControllers.Contains(_animCurrentCtrl))
                SetAnimCurrentController(0);
        }
        else _animCurrentCtrl = null;
    }

    private void SetAnimCurrentController(int index)
    {
        if (index < 0 || index >= _animControllers.Count) return;
        _animCurrentCtrl = _animControllers[index];
        RefreshAnimCache();
        if (_animCurrentCtrl != null) AnimLog($"当前: {_animCurrentCtrl.gameObject.name}");
    }

    private void RefreshAnimCache()
    {
        _animCachedLayerNames.Clear(); _animCachedCompNames.Clear(); _animLayerWeights.Clear(); _animLayerSpeeds.Clear();
        if (_animCurrentCtrl == null) return;
        if (_animCurrentCtrl.IsInitialized)
        {
            var layers = _animCurrentCtrl.GetLayerNames();
            _animCachedLayerNames.AddRange(layers);
            foreach (var name in _animCachedLayerNames)
            { _animLayerWeights[name] = _animCurrentCtrl.GetLayerWeight(name); _animLayerSpeeds[name] = 1f; }
        }
        var comps = _animCurrentCtrl.GetCompositionNames();
        foreach (var comp in comps)
        {
            var s = comp.ToString();
            if (!string.IsNullOrEmpty(s) && !_animCachedCompNames.Contains(s))
                _animCachedCompNames.Add(s);
        }
    }

    #endregion

    #region Animation Test Content

    private void DrawAnimationTestContent(GUIStyle lbl, GUIStyle btn, GUIStyle fld, float s)
    {
        _animScrollPos = GUILayout.BeginScrollView(_animScrollPos, false, true);

        GUILayout.Label("<b>Controller 选择</b>", lbl);
        if (GUILayout.Button("刷新场景Controller", btn, GUILayout.Height(32 * s)))
        {
            RefreshAnimControllers();
            AnimLog($"找到 {_animControllers.Count} 个Controller");
        }

        if (_animControllers.Count == 0)
        {
            GUILayout.Label("无Controller", lbl);
            GUILayout.EndScrollView();
            return;
        }

        for (int i = 0; i < _animControllers.Count; i++)
        {
            var c = _animControllers[i];
            if (c == null) continue;
            string text = c.gameObject.name;
            if (c == _animCurrentCtrl) text = $"[当前] {text}";
            if (GUILayout.Button(text, btn, GUILayout.Height(28 * s)))
                SetAnimCurrentController(i);
        }

        GUILayout.Space(4 * s);
        GUILayout.Label("<b>当前 Controller</b>", lbl);
        if (_animCurrentCtrl != null)
        {
            GUILayout.Label($"{_animCurrentCtrl.gameObject.name}  已初始化:{(_animCurrentCtrl.IsInitialized ? "是" : "否")}", lbl);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Init", btn, GUILayout.Height(28 * s)))
            { _animCurrentCtrl.Initialize(); RefreshAnimCache(); }
            if (GUILayout.Button("StopAll", btn, GUILayout.Height(28 * s)))
                _animCurrentCtrl.StopAll();
            GUILayout.EndHorizontal();
        }

        if (_animStateMgr != null)
        {
            GUILayout.Space(4 * s);
            GUILayout.Label("<b>State Manager</b>", lbl);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"角色ID: {debugAnimationPersonId}", lbl);
            if (GUILayout.Button("-", btn, GUILayout.Width(28 * s), GUILayout.Height(28 * s)))
                debugAnimationPersonId = Mathf.Max(1, debugAnimationPersonId - 1);
            if (GUILayout.Button("+", btn, GUILayout.Width(28 * s), GUILayout.Height(28 * s)))
                debugAnimationPersonId = Mathf.Min(5, debugAnimationPersonId + 1);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("注册当前Controller", btn, GUILayout.Height(28 * s)))
            {
                if (_animCurrentCtrl == null) AnimLog("未选择");
                else
                {
                    var sm = _animCurrentCtrl.GetComponent<SlotManager>();
                    _animStateMgr.RegisterCharacter(debugAnimationPersonId, debugAnimationPrefabType, _animCurrentCtrl, sm);
                    AnimLog($"注册角色{debugAnimationPersonId}");
                }
            }
            GUILayout.Label("状态测试:", lbl);
            DrawWrapBtns(AnimStateNames, i =>
            {
                _animStateMgr.ApplyState(debugAnimationPersonId, AnimStateNames[i]);
            }, btn, 28 * s, Mathf.Max(200f, _hubRect.width - 40 * s));
        }

        if (_animCurrentCtrl != null && _animCachedCompNames.Count > 0)
        {
            GUILayout.Space(4 * s);
            GUILayout.Label("<b>Composition</b>", lbl);
            var comps = _animCachedCompNames.ToArray();
            DrawWrapBtns(comps, i =>
            {
                _animCurrentCtrl.PlayComposition(comps[i]);
                AnimLog($"Play: {comps[i]}");
            }, btn, 28 * s, Mathf.Max(200f, _hubRect.width - 40 * s));
        }

        if (_animCurrentCtrl != null && _animCurrentCtrl.IsInitialized && _animCachedLayerNames.Count > 0)
        {
            GUILayout.Space(4 * s);
            GUILayout.Label("<b>Layer 调试</b>", lbl);
            foreach (var ln in _animCachedLayerNames)
            {
                float w = _animCurrentCtrl.GetLayerWeight(ln);
                if (!_animLayerWeights.ContainsKey(ln)) _animLayerWeights[ln] = w;
                if (!_animLayerSpeeds.ContainsKey(ln)) _animLayerSpeeds[ln] = 1f;
                GUILayout.Label($"{ln}  weight={w:F2}", lbl);
                float nw = GUILayout.HorizontalSlider(_animLayerWeights[ln], 0f, 1f);
                if (!Mathf.Approximately(nw, _animLayerWeights[ln]))
                { _animLayerWeights[ln] = nw; _animCurrentCtrl.SetLayerWeight(ln, nw); }
                GUILayout.Label($"Speed: {_animLayerSpeeds[ln]:F2}", lbl);
                float ns = GUILayout.HorizontalSlider(_animLayerSpeeds[ln], 0f, 3f);
                if (!Mathf.Approximately(ns, _animLayerSpeeds[ln]))
                { _animLayerSpeeds[ln] = ns; _animCurrentCtrl.SetAnimationSpeed(ln, ns); }
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Stop", btn, GUILayout.Height(28 * s)))
                    _animCurrentCtrl.StopLayer(ln);
                if (GUILayout.Button("Reset", btn, GUILayout.Height(28 * s)))
                {
                    _animLayerWeights[ln] = 1f;
                    _animCurrentCtrl.SetLayerWeight(ln, 1f);
                    _animLayerSpeeds[ln] = 1f;
                    _animCurrentCtrl.SetAnimationSpeed(ln, 1f);
                }
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label(_animStatus, lbl);
        }

        GUILayout.EndScrollView();
    }

    private void DrawWrapBtns(string[] labels, System.Action<int> onClick, GUIStyle btn, float h, float maxW)
    {
        float rowW = 0;
        bool inRow = false;
        for (int i = 0; i < labels.Length; i++)
        {
            float bw = btn.CalcSize(new GUIContent(labels[i])).x + 16f;
            if (rowW + bw > maxW && inRow)
            {
                GUILayout.EndHorizontal();
                inRow = false;
                rowW = 0;
            }
            if (!inRow)
            {
                GUILayout.BeginHorizontal();
                inRow = true;
            }
            int idx = i;
            if (GUILayout.Button(labels[i], btn, GUILayout.Height(h)))
                onClick?.Invoke(idx);
            rowW += bw + 4f;
        }
        if (inRow) GUILayout.EndHorizontal();
    }

    #endregion

    #region Helper

    private int ResolveAnimPersonId()
    {
        if (_animDataTable == null || _animCurrentCtrl == null) return -1;
        string goName = _animCurrentCtrl.gameObject.name;
        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null) return -1;
        foreach (var person in tables.TbPerson.DataList)
        {
            if (person.Animconfigs != null)
                foreach (var cfg in person.Animconfigs)
                    if (goName.IndexOf(cfg, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return person.Id;
        }
        return -1;
    }

    private void AnimLog(string msg)
    {
        _animStatus = msg;
        Debug.Log($"[AnimTest] {msg}");
    }

    #endregion
}
