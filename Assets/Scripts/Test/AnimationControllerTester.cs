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
    [SerializeField] private AnimationController controller;
    [SerializeField] private bool autoFindController = true;
    [SerializeField] private bool autoInitializeOnStart = false;

    [Header("界面设置")]
    [SerializeField] private bool showUI = true;
    [SerializeField] private Rect windowRect = new Rect(20, 20, 430, 700);
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

    private Vector2 scrollPos;
    private readonly Dictionary<string, float> layerWeights = new Dictionary<string, float>();
    private readonly Dictionary<string, float> layerSpeeds = new Dictionary<string, float>();
    private readonly List<string> cachedLayerNames = new List<string>();
    private readonly List<string> cachedCompositionNames = new List<string>();

    private string lastMessage = "Ready";
    private const int WindowId = 246810;

    private void Awake()
    {
        TryBindController();
    }

    private void Start()
    {
        if (autoInitializeOnStart && controller != null && !controller.IsInitialized)
        {
            controller.Initialize();
            LogMessage("Controller initialized on Start.");
        }

        RefreshControllerCache();
    }

    private void Reset()
    {
        TryBindController();
    }

    private void TryBindController()
    {
        if (controller != null) return;

        if (autoFindController)
        {
            controller = GetComponent<AnimationController>();

            if (controller == null)
            {
                controller = FindObjectOfType<AnimationController>();
            }
        }
    }

    private void RefreshControllerCache()
    {
        cachedLayerNames.Clear();
        cachedCompositionNames.Clear();
        layerWeights.Clear();
        layerSpeeds.Clear();

        if (controller == null)
            return;

        if (controller.IsInitialized)
        {
            var layers = controller.GetLayerNames();
            cachedLayerNames.AddRange(layers);

            foreach (var layerName in cachedLayerNames)
            {
                layerWeights[layerName] = controller.GetLayerWeight(layerName);
                layerSpeeds[layerName] = 1f;
            }
        }

        if (autoShowCompositionsFromController)
        {
            var compositions = controller.GetCompositionNames();
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
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Width(windowRect.width - 10), GUILayout.Height(windowRect.height - 35));

        DrawHeader();
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
        GUILayout.Label("用于测试 AnimationController 的播放、停止、权重与速度控制。");
    }

    private void DrawControllerSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Controller</b>", GetRichLabelStyle());

        if (controller == null)
        {
            GUILayout.Label("未绑定 AnimationController。");

            if (GUILayout.Button("重新查找 Controller", GUILayout.Height(28)))
            {
                TryBindController();
                RefreshControllerCache();
                LogMessage(controller != null ? "Controller found." : "Controller not found.");
            }

            GUILayout.EndVertical();
            return;
        }

        GUILayout.Label($"对象: {controller.gameObject.name}");
        GUILayout.Label($"已初始化: {(controller.IsInitialized ? "是" : "否")}");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Initialize", GUILayout.Height(28)))
        {
            controller.Initialize();
            RefreshControllerCache();
            LogMessage("Initialize called.");
        }

        if (GUILayout.Button("Refresh Cache", GUILayout.Height(28)))
        {
            RefreshControllerCache();
            LogMessage("Cache refreshed.");
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Stop All", GUILayout.Height(28)))
        {
            controller.StopAll();
            LogMessage("StopAll called.");
        }

        if (GUILayout.Button(showUI ? "Hide UI" : "Show UI", GUILayout.Height(28)))
        {
            showUI = !showUI;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawCompositionSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Composition 测试</b>", GetRichLabelStyle());

        if (controller == null)
        {
            GUILayout.Label("无 Controller。");
            GUILayout.EndVertical();
            return;
        }

        bool hasAnyButton = false;

        // 手动配置的 Composition 按钮
        foreach (var item in compositionButtons)
        {
            if (item == null || string.IsNullOrEmpty(item.compositionName))
                continue;

            hasAnyButton = true;

            if (GUILayout.Button($"{item.buttonLabel}  [{item.compositionName}]", GUILayout.Height(28)))
            {
                controller.PlayComposition(item.compositionName);
                LogMessage($"PlayComposition: {item.compositionName}");
            }
        }

        // 自动从 controller 获取 Composition
        if (autoShowCompositionsFromController)
        {
            foreach (var compositionName in cachedCompositionNames)
            {
                if (string.IsNullOrEmpty(compositionName))
                    continue;

                hasAnyButton = true;

                if (GUILayout.Button($"Auto: {compositionName}", GUILayout.Height(26)))
                {
                    controller.PlayComposition(compositionName);
                    LogMessage($"PlayComposition: {compositionName}");
                }
            }
        }

        if (!hasAnyButton)
        {
            GUILayout.Label("没有可用的 Composition 按钮。");
            GUILayout.Label("您可以手动配置 compositionButtons，或检查 Controller 配置中是否存在 compositions。");
        }

        GUILayout.EndVertical();
    }

    private void DrawClipSection()
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label("<b>Clip 测试</b>", GetRichLabelStyle());

        if (controller == null)
        {
            GUILayout.Label("无 Controller。");
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
                controller.PlayClip(item.layerName, item.clipName, item.transitionTime);
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

        if (controller == null)
        {
            GUILayout.Label("无 Controller。");
            GUILayout.EndVertical();
            return;
        }

        if (!controller.IsInitialized)
        {
            GUILayout.Label("Controller 尚未初始化，无法读取 Layer。");
            GUILayout.EndVertical();
            return;
        }

        if (cachedLayerNames.Count == 0)
        {
            GUILayout.Label("没有缓存到 Layer。");
            if (GUILayout.Button("重新读取 Layer", GUILayout.Height(26)))
            {
                RefreshControllerCache();
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

        bool isPlaying = controller.IsPlaying(layerName);
        float currentTime = controller.GetAnimationTime(layerName);
        float duration = controller.GetAnimationDuration(layerName);
        float currentWeight = controller.GetLayerWeight(layerName);

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
            controller.SetLayerWeight(layerName, newWeight);
        }

        GUILayout.Space(2);

        GUILayout.Label($"Speed: {layerSpeeds[layerName]:F2}");
        float newSpeed = GUILayout.HorizontalSlider(layerSpeeds[layerName], 0f, 3f);
        if (!Mathf.Approximately(newSpeed, layerSpeeds[layerName]))
        {
            layerSpeeds[layerName] = newSpeed;
            controller.SetAnimationSpeed(layerName, newSpeed);
        }

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Stop Layer", GUILayout.Height(24)))
        {
            controller.StopLayer(layerName);
            LogMessage($"StopLayer: {layerName}");
        }

        if (GUILayout.Button("Reset Weight", GUILayout.Height(24)))
        {
            layerWeights[layerName] = 1f;
            controller.SetLayerWeight(layerName, 1f);
            LogMessage($"Reset Weight: {layerName}");
        }

        if (GUILayout.Button("Reset Speed", GUILayout.Height(24)))
        {
            layerSpeeds[layerName] = 1f;
            controller.SetAnimationSpeed(layerName, 1f);
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