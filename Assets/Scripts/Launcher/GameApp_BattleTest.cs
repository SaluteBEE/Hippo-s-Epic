using UnityEngine;

public partial class GameApp
{
    #region Battle Test Inspector

    [Header("战斗测试")]
    [SerializeField] private bool enableBattleTest;
    [SerializeField] private int testBattleId = 1;

    #endregion

    #region Battle Test State

    private string _battleTestStatus = "";
    private string _battleTestInput = "1";
    private Rect _battleTestPanelRect = new Rect(-1, -1, 380, 260);
    private bool _isResizing;
    private Vector2 _resizeStartMouse;
    private Rect _resizeStartRect;
    private GUIStyle _battleLabelStyle;
    private GUIStyle _battleBtnStyle;
    private GUIStyle _battleFieldStyle;
    private Texture2D _battleBgTex;
    private Texture2D _battleBtnBgTex;

    #endregion

    public bool IsBattleTestEnabled => enableBattleTest;

    public void InitBattleTest()
    {
        if (!enableBattleTest) return;
        _battleTestStatus = "战斗测试就绪";
        Debug.Log("[GameApp] 战斗测试已启用");
    }

    private void DrawBattleTestGUI()
    {
        if (!enableBattleTest) return;

        InitBattleTestStyles();

        // 首次初始化面板位置（屏幕中央偏上）
        if (_battleTestPanelRect.x < 0)
            _battleTestPanelRect = new Rect(Screen.width / 2f - 190, 10, 380, 260);

        // 限制最小/最大尺寸
        _battleTestPanelRect.width = Mathf.Clamp(_battleTestPanelRect.width, 300, Screen.width);
        _battleTestPanelRect.height = Mathf.Clamp(_battleTestPanelRect.height, 180, Screen.height);

        // 可拖动+可拉伸窗口
        _battleTestPanelRect = GUI.Window(8888, _battleTestPanelRect, DrawBattleTestWindow, "", GUIStyle.none);
        HandleResize();
    }

    private void DrawBattleTestWindow(int windowID)
    {
        var area = new Rect(0, 0, _battleTestPanelRect.width, _battleTestPanelRect.height);
        GUI.DrawTexture(area, _battleBgTex);

        GUILayout.BeginArea(new Rect(4, 4, area.width - 8, area.height - 8));

        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>战斗测试</b>", new GUIStyle(_battleLabelStyle) { fontSize = 16, fontStyle = FontStyle.Bold });
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("关闭", _battleBtnStyle, GUILayout.Width(50)))
        {
            enableBattleTest = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.Label(_battleTestStatus, _battleLabelStyle);
        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        GUILayout.Label("战斗ID:", GUILayout.Width(55));
        _battleTestInput = GUILayout.TextField(_battleTestInput, _battleFieldStyle, GUILayout.Width(80));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("进入战斗", _battleBtnStyle, GUILayout.Width(90)))
            BattleTestEnter();
        GUILayout.EndHorizontal();

        GUILayout.Space(8);

        var bm = BattleManager;
        if (bm != null)
        {
            GUILayout.Label($"当前战斗: 阶段={bm.Phase}  等待操作={bm.IsWaitingForPlayerAction}", _battleLabelStyle);
            GUILayout.Label($"友方: {bm.PlayerUnits.Count}人  敌方: {bm.EnemyUnits.Count}人  第{bm.RoundCount}轮", _battleLabelStyle);
        }
        else
        {
            GUILayout.Label("当前无活跃战斗", _battleLabelStyle);
        }

        GUILayout.EndArea();

        // 标题栏区域拖动
        GUI.DragWindow(new Rect(0, 0, area.width - 70, 24));

        // 右下角拉伸手柄
        var handleRect = new Rect(area.width - 20, area.height - 20, 20, 20);
        GUI.Box(handleRect, "◢");
        EditorResizeHandle(handleRect);
    }

    private void EditorResizeHandle(Rect handleRect)
    {
        var e = Event.current;
        if (e.type == EventType.MouseDown && handleRect.Contains(e.mousePosition))
        {
            _isResizing = true;
            _resizeStartMouse = e.mousePosition;
            _resizeStartRect = _battleTestPanelRect;
            e.Use();
        }
        else if (e.type == EventType.MouseUp)
        {
            _isResizing = false;
        }
    }

    private void HandleResize()
    {
        if (!_isResizing) return;
        var e = Event.current;
        if (e.type == EventType.MouseDrag)
        {
            float dw = e.mousePosition.x - _resizeStartMouse.x;
            float dh = e.mousePosition.y - _resizeStartMouse.y;
            _battleTestPanelRect.width = _resizeStartRect.width + dw;
            _battleTestPanelRect.height = _resizeStartRect.height + dh;
            e.Use();
        }
    }

    private void BattleTestEnter()
    {
        if (!int.TryParse(_battleTestInput, out int battleId))
        {
            _battleTestStatus = "无效的战斗ID";
            return;
        }

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        var battleCfg = tables?.TbBattle.GetOrDefault(battleId);
        if (battleCfg == null)
        {
            _battleTestStatus = $"战斗{battleId}不存在";
            return;
        }

        _battleTestStatus = $"正在进入战斗: {battleCfg.Name}";
        EnterBattle(battleId);
    }

    private void InitBattleTestStyles()
    {
        if (_battleLabelStyle != null) return;

        _battleLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = false };
        _battleFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 13 };
        _battleBtnBgTex = new Texture2D(1, 1);
        _battleBtnBgTex.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.35f, 0.95f));
        _battleBtnBgTex.Apply();
        _battleBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            normal = { textColor = Color.white, background = _battleBtnBgTex },
            hover = { textColor = Color.white, background = _battleBtnBgTex }
        };
        _battleBgTex = new Texture2D(1, 1);
        _battleBgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.12f, 0.93f));
        _battleBgTex.Apply();
    }
}
