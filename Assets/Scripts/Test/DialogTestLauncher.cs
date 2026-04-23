using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogTestLauncher : MonoBehaviour
{
    [Header("测试配置")]
    [SerializeField] private int startDialogId = 1;
    [SerializeField] private bool autoStart = true;

    private DataTableManager _dataTable;
    private DialogManager _dialogManager;
    private string _statusText = "等待初始化...";

    private static readonly int[] TestDialogIds = { 1, 100, 200, 300, 400 };
    private static readonly string[] TestDialogNames =
    {
        "1: 原始对话（选项循环）",
        "100: 线性对话（自动推进）",
        "200: 循环选项（可回退）",
        "300: 嵌套选项（多层分支）",
        "400: 纯旁白（无角色）"
    };
    private int _currentTestIndex;

    private void Awake()
    {
        var dtObj = new GameObject("DataTableManager");
        dtObj.transform.SetParent(transform);
        _dataTable = dtObj.AddComponent<DataTableManager>();
        _dataTable.LoadTables();

        var dmObj = new GameObject("DialogManager");
        dmObj.transform.SetParent(transform);
        _dialogManager = dmObj.AddComponent<DialogManager>();
        _dialogManager.SetTables(_dataTable.Tables);

        _dialogManager.OnOptions += OnOptions;
        _dialogManager.OnDialogEnded += OnDialogEnded;

        SetupCanvas();
    }

    private void SetupCanvas()
    {
        var canvasObj = new GameObject("DialogCanvas");
        canvasObj.transform.SetParent(transform);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        var panelPrefab = Resources.Load<GameObject>("UI/DialoguePanel");
        if (panelPrefab == null)
        {
            Debug.LogError("[DialogTestLauncher] 未找到 DialoguePanel 预制体");
            return;
        }

        var panel = Instantiate(panelPrefab, canvasObj.transform, false);
        panel.name = "DialoguePanel";

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.15f, 0.08f);
        panelRect.anchorMax = new Vector2(0.85f, 1f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var content = panel.transform.Find("Scroll View/Viewport/Content");
        if (content == null)
        {
            Debug.LogError("[DialogTestLauncher] 未找到 Content 节点");
            return;
        }

        var leftPrefab = Resources.Load<ChatBubbleLeftView>("UI/BubbleLeft");
        var middlePrefab = Resources.Load<ChatBubbleMiddleView>("UI/BubbleMiddle");
        var rightPrefab = Resources.Load<ChatBubbleRightView>("UI/BubbleRight");
        var optionPrefab = Resources.Load<ChatBubbleOptionView>("UI/BubbleOption");

        DestroyExistingBubbles(content);

        var ui = panel.AddComponent<DialogManagerUI>();
        ui.Initialize(
            content as RectTransform,
            leftPrefab,
            middlePrefab,
            rightPrefab,
            optionPrefab
        );
    }

    private void DestroyExistingBubbles(Transform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    private void Start()
    {
        _currentTestIndex = System.Array.IndexOf(TestDialogIds, startDialogId);
        if (_currentTestIndex < 0) _currentTestIndex = 0;
        startDialogId = TestDialogIds[_currentTestIndex];

        Debug.Log($"[DialogTestLauncher] 初始化完成, 当前对话ID={startDialogId}, autoStart={autoStart}");

        if (autoStart)
            StartDialog();
    }

    private bool _searchFocused;
    private bool _guiMinimized;
    private string _searchInput = "";
    private GUIStyle _labelStyle;
    private GUIStyle _richTextStyle;
    private GUIStyle _textFieldStyle;
    private GUIStyle _btnStyle;
    private Texture2D _bgTex;
    private Texture2D _btnBgTex;

    private void Update()
    {
        if (_searchFocused) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_dialogManager.State == DialogState.Idle
                || _dialogManager.State == DialogState.Ended)
            {
                StartDialog();
            }
            else
            {
                _dialogManager.Advance();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) _dialogManager.ChooseOption(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) _dialogManager.ChooseOption(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) _dialogManager.ChooseOption(2);

        if (Input.GetKeyDown(KeyCode.Tab))
            SwitchToNextTest();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            SwitchTest(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow))
            SwitchTest(1);
    }

    private void StartDialog()
    {
        _statusText = "对话进行中...";
        Debug.Log($"[DialogTestLauncher] 开始对话 ID={startDialogId}");
        _dialogManager.StartDialog(startDialogId);
    }

    private void SwitchToNextTest()
    {
        _currentTestIndex = (_currentTestIndex + 1) % TestDialogIds.Length;
        startDialogId = TestDialogIds[_currentTestIndex];
        _statusText = $"切换到测试对话: {TestDialogNames[_currentTestIndex]}";
    }

    private void SwitchTest(int delta)
    {
        _currentTestIndex = (_currentTestIndex + delta + TestDialogIds.Length) % TestDialogIds.Length;
        startDialogId = TestDialogIds[_currentTestIndex];
        _statusText = $"切换到测试对话: {TestDialogNames[_currentTestIndex]}";
    }

    private void OnOptions(List<OptionInfo> options)
    {
        _statusText = "等待选择：";
        for (int i = 0; i < options.Count; i++)
            _statusText += $"\n  [{i + 1}] {options[i].Text}";
    }

    private void OnDialogEnded()
    {
        _statusText = "对话结束。Space=重开, Tab/左右箭头=切换对话";
    }

    private void OnGUI()
    {
        InitStyles();

        if (_guiMinimized)
        {
            if (GUI.Button(new Rect(10, 10, 80, 30), "展开面板", _btnStyle))
                _guiMinimized = false;
            return;
        }

        var area = new Rect(10, 10, 700, 200);
        DrawBackground(area);
        GUILayout.BeginArea(area);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"<b>[{TestDialogNames[_currentTestIndex]}]</b>", new GUIStyle(_richTextStyle) { fontSize = 16 });
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("收起", GUILayout.Width(50)))
        {
            _guiMinimized = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(2);
        GUILayout.Label($"状态: {_dialogManager.State}  |  {_statusText}", _labelStyle);
        GUILayout.Space(2);
        GUILayout.Label("Space=推进/重开 | 1/2/3=选项 | Tab/左右=切换对话", _labelStyle);

        GUILayout.Space(4);
        GUILayout.BeginHorizontal();
        GUILayout.Label("跳转ID:", _labelStyle, GUILayout.Width(60));
        GUI.SetNextControlName("DialogSearchField");
        _searchInput = GUILayout.TextField(_searchInput, _textFieldStyle, GUILayout.Width(120));
        _searchFocused = GUI.GetNameOfFocusedControl() == "DialogSearchField";
        if (GUILayout.Button("开始", GUILayout.Width(60)))
            TryJumpToDialog();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (Event.current.isKey && Event.current.keyCode == KeyCode.Return
            && GUI.GetNameOfFocusedControl() == "DialogSearchField")
        {
            TryJumpToDialog();
        }
    }

    private void TryJumpToDialog()
    {
        if (int.TryParse(_searchInput, out int id))
        {
            var dialog = _dataTable.Tables.TbDialog.GetOrDefault(id);
            if (dialog != null)
            {
                startDialogId = id;
                _currentTestIndex = System.Array.IndexOf(TestDialogIds, id);
                if (_currentTestIndex < 0) _currentTestIndex = 0;
                _statusText = $"已跳转到对话 {id}";
                StartDialog();
                _searchInput = "";
                GUI.FocusControl("");
            }
            else
            {
                _statusText = $"Dialog {id} 不存在！";
            }
        }
    }

    private void InitStyles()
    {
        if (_labelStyle != null) return;
        _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
        _richTextStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 16, wordWrap = true };
        _textFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 14 };

        _btnBgTex = new Texture2D(1, 1);
        _btnBgTex.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.3f, 0.92f));
        _btnBgTex.Apply();
        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            normal = { textColor = Color.white, background = _btnBgTex },
            hover = { textColor = Color.white, background = _btnBgTex }
        };
    }

    private void DrawBackground(Rect rect)
    {
        if (_bgTex == null)
        {
            _bgTex = new Texture2D(1, 1);
            _bgTex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.15f, 0.92f));
            _bgTex.Apply();
        }
        GUI.DrawTexture(rect, _bgTex);
    }

    private void OnDestroy()
    {
        if (_dialogManager != null)
        {
            _dialogManager.OnOptions -= OnOptions;
            _dialogManager.OnDialogEnded -= OnDialogEnded;
        }
    }
}
