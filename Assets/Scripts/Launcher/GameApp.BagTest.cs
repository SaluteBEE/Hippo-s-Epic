using UnityEngine;

public partial class GameApp
{
    #region Bag Test Inspector

    [Header("背包调试")]
    [SerializeField] private bool debugEnableBagTest;

    #endregion

    #region Bag Test State

    private string _bagAddInput = "";
    private string _bagCountInput = "1";
    private string _bagRemoveInput = "";
    private string _bagStatus = "";
    private Vector2 _bagItemScrollPos;
    private Vector2 _bagContentScrollPos;

    public bool IsBagTestEnabled => debugEnableBagTest;

    #endregion

    public void InitBagTest()
    {
        if (!debugEnableBagTest) return;
        _bagStatus = "背包测试就绪";
        Debug.Log("[GameApp] 背包测试已启用");
    }

    private void DrawBagTestContent(GUIStyle lbl, GUIStyle btn, GUIStyle fld, float s)
    {
        GUILayout.Label(_bagStatus, lbl);
        GUILayout.Space(4 * s);

        GUILayout.BeginHorizontal();
        GUILayout.Label("ID:", lbl, GUILayout.Width(36 * s));
        _bagAddInput = GUILayout.TextField(_bagAddInput, fld, GUILayout.Width(90 * s));
        GUILayout.Label("x", lbl, GUILayout.Width(18 * s));
        _bagCountInput = GUILayout.TextField(_bagCountInput, fld, GUILayout.Width(50 * s));
        if (GUILayout.Button("添加", btn, GUILayout.Height(28 * s))) BagTestAdd();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("移除ID:", lbl, GUILayout.Width(70 * s));
        _bagRemoveInput = GUILayout.TextField(_bagRemoveInput, fld, GUILayout.Width(90 * s));
        if (GUILayout.Button("移除", btn, GUILayout.Height(28 * s))) BagTestRemove();
        if (GUILayout.Button("清空", btn, GUILayout.Height(28 * s))) BagTestClear();
        GUILayout.EndHorizontal();

        GUILayout.Space(4 * s);
        var tables = ManagerRegistry.GetTables<cfg.Tables>();
        if (tables != null)
        {
            GUILayout.Label("快捷添加:", lbl);
            _bagItemScrollPos = GUILayout.BeginScrollView(_bagItemScrollPos, GUILayout.Height(160 * s));
            foreach (var item in tables.TbItem.DataList)
            {
                if (item.Type == 1) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{item.Id}", lbl, GUILayout.Width(50 * s));
                GUILayout.Label(item.Name, lbl, GUILayout.Width(120 * s));
                if (GUILayout.Button("+1", btn, GUILayout.Width(44 * s), GUILayout.Height(26 * s)))
                    BagManager.Instance.AddItem(item.Id, 1);
                if (GUILayout.Button("+10", btn, GUILayout.Width(48 * s), GUILayout.Height(26 * s)))
                    BagManager.Instance.AddItem(item.Id, 10);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        GUILayout.Space(4 * s);
        GUILayout.Label("背包内容:", lbl);
        var items = BagManager.Instance.AllItems;
        if (items.Count == 0)
        {
            GUILayout.Label("(空)", lbl);
        }
        else
        {
            _bagContentScrollPos = GUILayout.BeginScrollView(_bagContentScrollPos, GUILayout.Height(140 * s));
            foreach (var entry in items)
            {
                string name = entry.itemId.ToString();
                if (tables != null)
                {
                    var cfg = tables.TbItem.GetOrDefault(entry.itemId);
                    if (cfg != null) name = cfg.Name;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{entry.itemId}", lbl, GUILayout.Width(50 * s));
                GUILayout.Label($"{name} x{entry.count}", lbl, GUILayout.Width(160 * s));
                if (GUILayout.Button("-1", btn, GUILayout.Width(44 * s), GUILayout.Height(26 * s)))
                    BagManager.Instance.RemoveItem(entry.itemId, 1);
                if (GUILayout.Button("删", btn, GUILayout.Width(36 * s), GUILayout.Height(26 * s)))
                    BagManager.Instance.RemoveItem(entry.itemId, entry.count);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
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
}
