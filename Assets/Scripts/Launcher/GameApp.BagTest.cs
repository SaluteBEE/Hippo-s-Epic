using System.Collections.Generic;
using UnityEngine;

public partial class GameApp
{
    #region Bag Test Inspector

    [Header("背包测试")]
    [SerializeField] private bool enableBagTest;

    #endregion

    #region Bag Test State

    private string _bagAddInput = "";
    private string _bagCountInput = "1";
    private string _bagRemoveInput = "";
    private string _bagStatus = "";
    private Vector2 _bagItemScrollPos;
    private Vector2 _bagContentScrollPos;
    private GUIStyle _bagLabelStyle;
    private GUIStyle _bagBtnStyle;
    private GUIStyle _bagFieldStyle;
    private Texture2D _bagBgTex;
    private Texture2D _bagBtnBgTex;
    private Texture2D _bagSectionTex;

    #endregion

    public bool IsBagTestEnabled => enableBagTest;

    public void InitBagTest()
    {
        if (!enableBagTest) return;
        _bagStatus = "背包测试就绪";
        Debug.Log("[GameApp] 背包测试已启用");
    }

    private void UpdateBagTest()
    {
    }

    private void DrawBagTestGUI()
    {
        if (!enableBagTest) return;

        InitBagTestStyles();

        float panelX = Screen.width - 370;
        float panelY = 10;
        float panelW = 360;
        float panelH = 520;

        var area = new Rect(panelX, panelY, panelW, panelH);
        GUI.DrawTexture(area, _bagBgTex);

        GUILayout.BeginArea(area);

        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>背包测试</b>", new GUIStyle(_bagLabelStyle) { fontSize = 16, fontStyle = FontStyle.Bold });
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("关闭", _bagBtnStyle, GUILayout.Width(50)))
        {
            enableBagTest = false;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(4);
        GUILayout.Label(_bagStatus, _bagLabelStyle);
        GUILayout.Space(4);

        DrawBagAddSection();
        DrawBagRemoveSection();
        GUILayout.Space(4);
        DrawBagQuickAddSection();
        GUILayout.Space(4);
        DrawBagContentSection();

        GUILayout.EndArea();
    }

    private void DrawBagAddSection()
    {
        GUI.DrawTexture(new Rect(10, GUILayoutUtility.GetLastRect().yMax, 340, 60), _bagSectionTex);

        GUILayout.Label("添加物品:", _bagLabelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("ID:", GUILayout.Width(25));
        _bagAddInput = GUILayout.TextField(_bagAddInput, _bagFieldStyle, GUILayout.Width(60));
        GUILayout.Label("x", GUILayout.Width(12));
        _bagCountInput = GUILayout.TextField(_bagCountInput, _bagFieldStyle, GUILayout.Width(40));
        if (GUILayout.Button("添加", _bagBtnStyle, GUILayout.Width(50)))
            BagTestAdd();
        GUILayout.EndHorizontal();
    }

    private void DrawBagRemoveSection()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("移除ID:", GUILayout.Width(50));
        _bagRemoveInput = GUILayout.TextField(_bagRemoveInput, _bagFieldStyle, GUILayout.Width(60));
        GUILayout.Label("x1", GUILayout.Width(20));
        if (GUILayout.Button("移除", _bagBtnStyle, GUILayout.Width(50)))
            BagTestRemove();
        if (GUILayout.Button("清空背包", _bagBtnStyle, GUILayout.Width(70)))
            BagTestClear();
        GUILayout.EndHorizontal();
    }

    private void DrawBagQuickAddSection()
    {
        GUILayout.Label("快捷添加:", _bagLabelStyle);

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables == null)
        {
            GUILayout.Label("Tables 未加载", _bagLabelStyle);
            return;
        }

        _bagItemScrollPos = GUILayout.BeginScrollView(_bagItemScrollPos, GUILayout.Height(100));

        foreach (var item in tables.TbItem.DataList)
        {
            if (item.Type == 1) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{item.Id}", GUILayout.Width(40));
            GUILayout.Label(item.Name, GUILayout.Width(80));
            GUILayout.Label($"类型{item.Type}", GUILayout.Width(45));
            string stackLabel = item.Stackable ? "可叠" : "不可叠";
            GUILayout.Label(stackLabel, GUILayout.Width(45));
            if (GUILayout.Button("+1", _bagBtnStyle, GUILayout.Width(30)))
            {
                BagManager.Instance.AddItem(item.Id, 1);
                _bagStatus = $"添加: {item.Name} x1";
            }
            if (GUILayout.Button("+10", _bagBtnStyle, GUILayout.Width(35)))
            {
                BagManager.Instance.AddItem(item.Id, 10);
                _bagStatus = $"添加: {item.Name} x10";
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void DrawBagContentSection()
    {
        GUILayout.Label("背包内容:", _bagLabelStyle);

        var items = BagManager.Instance.AllItems;
        if (items.Count == 0)
        {
            GUILayout.Label("  (空)", _bagLabelStyle);
            return;
        }

        var tables = ManagerRegistry.GetTables<cfg.Tables>();

        _bagContentScrollPos = GUILayout.BeginScrollView(_bagContentScrollPos, GUILayout.Height(120));

        foreach (var entry in items)
        {
            GUILayout.BeginHorizontal();
            string name = entry.itemId.ToString();
            if (tables != null)
            {
                var cfg = tables.TbItem.GetOrDefault(entry.itemId);
                if (cfg != null) name = cfg.Name;
            }
            GUILayout.Label($"{entry.itemId}", GUILayout.Width(40));
            GUILayout.Label(name, GUILayout.Width(80));
            GUILayout.Label($"x{entry.count}", GUILayout.Width(40));
            if (GUILayout.Button("-1", _bagBtnStyle, GUILayout.Width(30)))
            {
                BagManager.Instance.RemoveItem(entry.itemId, 1);
                _bagStatus = $"移除: {name} x1";
            }
            if (GUILayout.Button("删", _bagBtnStyle, GUILayout.Width(25)))
            {
                BagManager.Instance.RemoveItem(entry.itemId, entry.count);
                _bagStatus = $"删除全部: {name}";
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
    }

    private void BagTestAdd()
    {
        if (!int.TryParse(_bagAddInput, out int itemId)) return;
        if (!int.TryParse(_bagCountInput, out int count)) count = 1;

        BagManager.Instance.AddItem(itemId, count);

        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        string name = itemId.ToString();
        if (tables != null)
        {
            var cfg = tables.TbItem.GetOrDefault(itemId);
            if (cfg != null) name = cfg.Name;
        }
        _bagStatus = $"添加: {name} x{count}";
    }

    private void BagTestRemove()
    {
        if (!int.TryParse(_bagRemoveInput, out int itemId)) return;

        bool ok = BagManager.Instance.RemoveItem(itemId, 1);
        _bagStatus = ok ? $"移除: 物品{itemId} x1" : $"移除失败: 物品{itemId} 不存在或不足";
    }

    private void BagTestClear()
    {
        BagManager.Instance.Clear();
        _bagStatus = "背包已清空";
    }

    private void InitBagTestStyles()
    {
        if (_bagLabelStyle != null) return;

        _bagLabelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = false };
        _bagFieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 12 };
        _bagBtnBgTex = new Texture2D(1, 1);
        _bagBtnBgTex.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.35f, 0.95f));
        _bagBtnBgTex.Apply();
        _bagBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 11,
            normal = { textColor = Color.white, background = _bagBtnBgTex },
            hover = { textColor = Color.white, background = _bagBtnBgTex }
        };
        _bagBgTex = new Texture2D(1, 1);
        _bagBgTex.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.12f, 0.93f));
        _bagBgTex.Apply();
        _bagSectionTex = new Texture2D(1, 1);
        _bagSectionTex.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.2f, 0.5f));
        _bagSectionTex.Apply();
    }
}
