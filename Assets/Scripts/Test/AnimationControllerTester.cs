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
    [SerializeField] private Rect windowRect = new Rect(20, 20, 480, 760);
    [SerializeField] private bool draggableWindow = true;

    [Header("测试按钮 - Clip")]
    [SerializeField] private List<ClipTestButton> clipButtons = new List<ClipTestButton>();

    [Header("测试按钮 - Composition")]
    [SerializeField] private List<CompositionTestButton> compositionButtons = new List<CompositionTestButton>();

    [Header("自动显示控制器中的 Composition")]
    [SerializeField] private bool autoShowCompositionsFromController = true;

    [Header("自动显示 Layer 调试控制")]
    [SerializeField] private bool autoShowLayerControls = true;

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

    private string lastMessage = "Ready";
    private const int WindowId = 246810;

    private void Awake()
    {
        if (autoFindControllers)
        {
            RefreshControllerList();
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

    private void OnGUI()
    {
        if (!showUI)
            return;

        windowRect = GUI.Window(WindowId, windowRect, DrawWindow, "Animation Controller Tester");
    }

    private void DrawWindow(int id)
    {
        scrollPos = GUILayout.BeginScrollView(
            scrollPos,
            GUILayout.Width(windowRect.width - 10),
            GUILayout.Height(windowRect.height - 35)
        );

        DrawHeader();
        GUILayout.Space(8);

        DrawControllerSelectionSection();
        GUILayout.Space(8);

        DrawControllerSection();
        GUILayout.Space(8);

        DrawCompositionSection();
        GUILayout.Space(8);

        DrawClipSection();
        GUILayout.Space(8);

        DrawLayerControlSection();
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
        GUILayout.Label("支持场景内多个 AnimationController 的选择与调试。");
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

    private void DrawStatusSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>状态</b>", GetRichLabelStyle());
        GUILayout.Label(lastMessage);
        GUILayout.EndVertical();
    }

    private void LogMessage(string msg)
    {
        lastMessage = msg;

        if (logOperations)
        {
            Debug.Log($"[AnimationControllerTester] {msg}");
        }
    }

    private GUIStyle richLabelStyle;
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
}