using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 测试 UI 面板构建器:纯代码生成 UGUI 层级,保存为 prefab 并注册 Addressable。
/// 入口:菜单 Tools/UI/Build TestUIPanel,或 MCP execute_code 调用 TestUIBuilder.Build()。
/// </summary>
public static class TestUIBuilder
{
    private const string PrefabPath = "Assets/Prefabs/UI/Test/TestUIPanel.prefab";
    private const string FontPath = "Assets/Fonts/DreamHanSans-W24 SDF.asset";
    private const string Address = "ui/Test/TestUIPanel";

    // 可复用引线:构建期收集,序列化写入
    private static readonly System.Collections.Generic.Dictionary<string, UnityEngine.Object> Bindings =
        new System.Collections.Generic.Dictionary<string, UnityEngine.Object>();

    [MenuItem("Tools/UI/Build TestUIPanel")]
    public static void Build()
    {
        Bindings.Clear();

        var root = new GameObject("TestUIPanel");
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        root.AddComponent<CanvasRenderer>();
        var panel = root.AddComponent<TestUIPanel>();

        // ===== SafeArea(全屏安全区容器) =====
        var safe = CreateRect("SafeArea", root.transform, 0, 0, 1, 1, Vector2.zero, Vector2.zero);

        // ===== TopHUD 顶栏 =====
        var hud = CreateRect("TopHUD", safe.transform, 0, 1, 1, 1, new Vector2(20, -10), new Vector2(-20, -90));
        var hudBg = hud.gameObject.AddComponent<Image>();
        hudBg.sprite = GetUISprite();
        hudBg.type = Image.Type.Sliced;
        hudBg.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

        var title = CreateText("Title", hud.transform, "测试 UI 面板(框架示例)", 30, TextAlignmentOptions.Left,
            new Vector2(20, 0), new Vector2(-420, 0));
        Bindings["titleText"] = title;

        var gold = CreateText("GoldText", hud.transform, "金币: 0", 24, TextAlignmentOptions.Center,
            new Vector2(-120, 0), new Vector2(120, 0));
        Bindings["goldText"] = gold;

        var popupBtn = CreateButton("PopupBtn", hud.transform, "弹窗", 22,
            new Vector2(-340, 0), new Vector2(-250, 0));
        Bindings["popupBtn"] = popupBtn;

        var closeBtn = CreateButton("CloseBtn", hud.transform, "关闭", 22,
            new Vector2(-200, 0), new Vector2(-110, 0));
        Bindings["closeBtn"] = closeBtn;

        // ===== 中央区域 =====
        var center = CreateRect("Center", safe.transform, 0, 0, 1, 1, new Vector2(20, 110), new Vector2(-20, -110));

        // --- 左侧滚动列表 ---
        var listRoot = CreateRect("LeftList", center.transform, 0, 0, 0.42f, 1, Vector2.zero, Vector2.zero);
        var listBg = listRoot.gameObject.AddComponent<Image>();
        listBg.sprite = GetUISprite();
        listBg.type = Image.Type.Sliced;
        listBg.color = new Color(0.16f, 0.16f, 0.2f, 0.9f);

        var scroll = listRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.scrollSensitivity = 20f;

        var viewport = CreateRect("Viewport", listRoot.transform, 0, 0, 1, 1, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        var viewportImg = viewport.gameObject.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0.01f); // 透明,供 Raycast
        scroll.viewport = viewport;

        var content = CreateRect("Content", viewport.transform, 0, 1, 1, 1, Vector2.zero, new Vector2(0, 0));
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        var csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        Bindings["listScroll"] = scroll;
        Bindings["listContent"] = content;

        var listItem = CreateListItem("ListItemTemplate", content.transform, 60f);
        Bindings["listItemTemplate"] = listItem;

        // --- 中间物品网格 ---
        var gridRoot = CreateRect("MidGrid", center.transform, 0.45f, 0, 0.78f, 1, Vector2.zero, Vector2.zero);
        var gridBg = gridRoot.gameObject.AddComponent<Image>();
        gridBg.sprite = GetUISprite();
        gridBg.type = Image.Type.Sliced;
        gridBg.color = new Color(0.16f, 0.16f, 0.2f, 0.9f);

        var grid = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(110, 120);
        grid.spacing = new Vector2(10, 10);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        Bindings["grid"] = grid;

        var gridItem = CreateListItem("GridItemTemplate", gridRoot.transform, 110f, 120f);
        Bindings["gridItemTemplate"] = gridItem;

        // --- 右侧弹窗(覆盖全面板) ---
        var popup = CreateRect("Popup", center.transform, 0, 0, 1, 1, Vector2.zero, Vector2.zero);
        var mask = popup.gameObject.AddComponent<Image>();
        mask.color = new Color(0, 0, 0, 0.6f);
        Bindings["popupRoot"] = popup.gameObject;

        var popupPanel = CreateRect("PopupPanel", popup.transform, 0.5f, 0.5f, 0.5f, 0.5f,
            new Vector2(-260, -170), new Vector2(260, 170));
        var panelBg = popupPanel.gameObject.AddComponent<Image>();
        panelBg.sprite = GetUISprite();
        panelBg.type = Image.Type.Sliced;
        panelBg.color = new Color(0.22f, 0.22f, 0.28f, 1f);

        var popupText = CreateText("PopupText", popupPanel.transform,
            "这是一个弹窗示例。", 24, TextAlignmentOptions.Center,
            new Vector2(20, 60), new Vector2(-20, -40));
        Bindings["popupText"] = popupText;

        var confirmBtn = CreateButton("ConfirmBtn", popupPanel.transform, "确 定", 22,
            new Vector2(20, -140), new Vector2(170, -90));
        Bindings["popupConfirmBtn"] = confirmBtn;

        var cancelBtn = CreateButton("CancelBtn", popupPanel.transform, "取 消", 22,
            new Vector2(190, -140), new Vector2(340, -90));
        Bindings["popupCancelBtn"] = cancelBtn;

        // ===== BottomBar 底部状态条 =====
        var bottom = CreateRect("BottomBar", safe.transform, 0, 0, 1, 0, new Vector2(20, 20), new Vector2(-20, 90));
        var bottomBg = bottom.gameObject.AddComponent<Image>();
        bottomBg.sprite = GetUISprite();
        bottomBg.type = Image.Type.Sliced;
        bottomBg.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);

        var hpBar = CreateBar("HPBar", bottom.transform, new Vector2(20, 0), new Vector2(-520, 0), new Color(0.85f, 0.25f, 0.25f));
        Bindings["hpBar"] = hpBar;

        var mpBar = CreateBar("MPBar", bottom.transform, new Vector2(20, 0), new Vector2(-520, 0), new Color(0.25f, 0.45f, 0.9f));
        mpBar.rectTransform.anchoredPosition = new Vector2(0, 0);
        // MP 条放在 HP 条下方
        mpBar.rectTransform.anchorMin = new Vector2(0, 0);
        mpBar.rectTransform.anchorMax = new Vector2(0, 0);
        mpBar.rectTransform.anchoredPosition = new Vector2(20, 6);
        mpBar.rectTransform.sizeDelta = new Vector2(0, 20);
        Bindings["mpBar"] = mpBar;

        var addGoldBtn = CreateButton("AddGoldBtn", bottom.transform, "加金币", 22,
            new Vector2(-140, 0), new Vector2(-40, 0));
        Bindings["addGoldBtn"] = addGoldBtn;

        var resetBtn = CreateButton("ResetListBtn", bottom.transform, "重置列表", 22,
            new Vector2(-20, 0), new Vector2(80, 0));
        Bindings["resetListBtn"] = resetBtn;

        // ===== 保存 prefab =====
        EnsureFolder("Assets/Prefabs/UI/Test");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        // ===== 序列化绑定字段 =====
        var panelSo = new SerializedObject(prefab.GetComponent<TestUIPanel>());
        foreach (var kv in Bindings)
        {
            var prop = panelSo.FindProperty(kv.Key);
            if (prop != null)
                prop.objectReferenceValue = kv.Value;
        }
        panelSo.ApplyModifiedPropertiesWithoutUndo();

        // ===== Addressable 注册 =====
        RegisterAddressable(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TestUIBuilder] 完成: {PrefabPath}, 绑定字段 {Bindings.Count} 个, Addressable: {Address}");
    }

    // ---------- 工具函数 ----------

    private static Sprite GetUISprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    private static TMP_FontAsset GetFont()
    {
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
    }

    private static RectTransform CreateRect(string name, Transform parent,
        float ax, float ay, float bx, float by, Vector2 offMin, Vector2 offMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(ax, ay);
        rt.anchorMax = new Vector2(bx, by);
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        return rt;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content,
        int fontSize, TextAlignmentOptions align, Vector2 offMin, Vector2 offMax)
    {
        var rt = CreateRect(name, parent, 0.5f, 0.5f, 0.5f, 0.5f, offMin, offMax);
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = GetFont();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, int fontSize,
        Vector2 offMin, Vector2 offMax)
    {
        var rt = CreateRect(name, parent, 0.5f, 0.5f, 0.5f, 0.5f, offMin, offMax);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = GetUISprite();
        img.type = Image.Type.Sliced;
        img.color = new Color(0.3f, 0.5f, 0.9f, 1f);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var text = CreateText("Text", rt, label, fontSize, TextAlignmentOptions.Center, Vector2.zero, Vector2.zero);
        return btn;
    }

    private static Image CreateBar(string name, Transform parent, Vector2 offMin, Vector2 offMax, Color color)
    {
        // 外框
        var frame = CreateRect(name, parent, 0, 0.5f, 0, 0.5f, offMin, offMax);
        var frameImg = frame.gameObject.AddComponent<Image>();
        frameImg.sprite = GetUISprite();
        frameImg.type = Image.Type.Sliced;
        frameImg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

        // 填充
        var fillRect = CreateRect("Fill", frame, 0, 0, 1, 1, new Vector2(2, 2), new Vector2(-2, -2));
        var fill = fillRect.gameObject.AddComponent<Image>();
        fill.sprite = GetUISprite();
        fill.type = Image.Type.Sliced;
        fill.color = color;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0.7f;
        return fill;
    }

    private static TestUIListItem CreateListItem(string name, Transform parent, float height)
    {
        return CreateListItem(name, parent, height, 0f);
    }

    private static TestUIListItem CreateListItem(string name, Transform parent, float height, float width)
    {
        var rt = CreateRect(name, parent, 0, 0, 1, 1, Vector2.zero, Vector2.zero);
        rt.sizeDelta = width > 0 ? new Vector2(width, height) : new Vector2(0, height);

        var bg = rt.gameObject.AddComponent<Image>();
        bg.sprite = GetUISprite();
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.24f, 0.24f, 0.3f, 1f);

        var selectedBg = CreateRect("Selected", rt, 0, 0, 1, 1, Vector2.zero, Vector2.zero);
        var selImg = selectedBg.gameObject.AddComponent<Image>();
        selImg.sprite = GetUISprite();
        selImg.type = Image.Type.Sliced;
        selImg.color = new Color(0.3f, 0.6f, 1f, 0.35f);

        var icon = CreateRect("Icon", rt, 0, 0.5f, 0, 0.5f, new Vector2(6, -8), new Vector2(40, 8));
        var iconImg = icon.gameObject.AddComponent<Image>();
        iconImg.sprite = GetUISprite();
        iconImg.color = Color.white;

        var nameText = CreateText("Name", rt, "物品", 20, TextAlignmentOptions.Left,
            new Vector2(48, 0), new Vector2(-40, 0));
        nameText.rectTransform.anchorMin = new Vector2(0, 0.5f);
        nameText.rectTransform.anchorMax = new Vector2(0, 0.5f);

        var countText = CreateText("Count", rt, "x1", 18, TextAlignmentOptions.Right,
            new Vector2(-40, 0), new Vector2(-8, 0));
        countText.rectTransform.anchorMin = new Vector2(1, 0.5f);
        countText.rectTransform.anchorMax = new Vector2(1, 0.5f);

        var item = rt.gameObject.AddComponent<TestUIListItem>();
        var so = new SerializedObject(item);
        so.FindProperty("icon").objectReferenceValue = iconImg;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("countText").objectReferenceValue = countText;
        so.FindProperty("selectedBg").objectReferenceValue = selImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        return item;
    }

    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (var seg in path.Substring("Assets/".Length).Split('/'))
        {
            current += "/" + seg;
            if (!AssetDatabase.IsValidFolder(current))
                AssetDatabase.CreateFolder(current.Substring(0, current.LastIndexOf('/')), seg);
        }
    }

    /// <summary>按名字和参数名序列查找方法,避免重载歧义。</summary>
    private static System.Reflection.MethodInfo FindMethod(System.Type type, string name, params string[] paramNames)
    {
        foreach (var m in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (m.Name != name) continue;
            var ps = m.GetParameters();
            if (ps.Length != paramNames.Length) continue;
            bool ok = true;
            for (int i = 0; i < ps.Length; i++)
            {
                if (!string.Equals(ps[i].Name, paramNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    ok = false;
                    break;
                }
            }
            if (ok) return m;
        }
        return null;
    }

    /// <summary>把实参裁剪/补齐到方法签名长度,缺省参数用默认值。</summary>
    private static object[] FitArgs(System.Reflection.MethodInfo method, params object[] args)
    {
        var ps = method.GetParameters();
        var result = new object[ps.Length];
        for (int i = 0; i < ps.Length; i++)
        {
            if (i < args.Length && args[i] != null)
            {
                result[i] = args[i];
            }
            else
            {
                var t = ps[i].ParameterType;
                result[i] = t.IsValueType ? Activator.CreateInstance(t) : null;
            }
        }
        return result;
    }

    private static void RegisterAddressable(GameObject prefab)
    {
        // 反射调用 Addressables API,避免 Assembly-CSharp-Editor 的程序集引用问题
        var defaultObjectType = System.Type.GetType(
            "UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");
        if (defaultObjectType == null)
        {
            Debug.LogWarning("[TestUIBuilder] 未找到 AddressableAssetSettingsDefaultObject,跳过 Addressable 注册。");
            return;
        }

        var settingsProp = defaultObjectType.GetProperty("Settings",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        var settings = settingsProp?.GetValue(null);
        if (settings == null)
        {
            Debug.LogWarning("[TestUIBuilder] Addressable Settings 为空,跳过注册。");
            return;
        }

        var settingsType = settings.GetType();
        AssetDatabase.Refresh(); // 确保新 prefab 已导入,否则 AssetPathToGUID 拿不到
        var guid = AssetDatabase.AssetPathToGUID(PrefabPath);
        if (string.IsNullOrEmpty(guid))
        {
            Debug.LogWarning($"[TestUIBuilder] 无法获取 {PrefabPath} 的 GUID,跳过 Addressable 注册。");
            return;
        }

        // 用正确的类型全名精确匹配(AddressableAssetGroup 在 .Settings 命名空间)
        var groupType = settingsType.Assembly.GetType(
            "UnityEditor.AddressableAssets.Settings.AddressableAssetGroup");
        var findGroup = groupType == null ? null : settingsType.GetMethod("FindGroup",
            new[] { typeof(string) });
        var group = findGroup?.Invoke(settings, new object[] { "UI" });
        if (group == null)
            group = settingsType.GetProperty("DefaultGroup")?.GetValue(settings);
        if (group == null)
        {
            Debug.LogWarning("[TestUIBuilder] 无可用 Addressable 组,跳过注册。");
            return;
        }

        var createOrMove = groupType == null ? null : settingsType.GetMethod("CreateOrMoveEntry",
            new[] { typeof(string), groupType, typeof(bool), typeof(bool) });
        var entry = createOrMove?.Invoke(settings, new object[] { guid, group, false, true });
        if (entry == null)
        {
            Debug.LogWarning("[TestUIBuilder] CreateOrMoveEntry 返回 null,Addressable 注册失败。");
            return;
        }

        // address 属性名是 "Address"(大写),用 IgnoreCase 匹配
        var addressProp = entry.GetType().GetProperty("Address",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        addressProp?.SetValue(entry, Address);

        var modEventType = settingsType.Assembly.GetType(
            "UnityEditor.AddressableAssets.AddressableAssetSettings+ModificationEvent");
        var setDirty = FindMethod(settingsType, "SetDirty", "modificationEvent", "context", "postEvent", "settingsModified");
        if (modEventType != null && setDirty != null)
        {
            var entryMoved = System.Enum.Parse(modEventType, "EntryMoved");
            setDirty.Invoke(settings, FitArgs(setDirty, entryMoved, entry, true, true));
        }

        // 落盘
        EditorUtility.SetDirty(settings as UnityEngine.Object);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TestUIBuilder] Addressable 注册: {Address} -> {group.GetType().GetProperty("Name")?.GetValue(group)}");
    }
}
