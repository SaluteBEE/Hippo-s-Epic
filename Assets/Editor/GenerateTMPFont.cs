using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using System.IO;
using System.Linq;

public static class GenerateTMPFont
{
    [MenuItem("Tools/Generate TMP Font Asset")]
    public static void Generate()
    {
        string fontPath = "Assets/Fonts/AlimamaFangYuanTiVF-Thin.ttf";
        string charFile = Application.dataPath + "/Fonts/7000+symbols.txt";
        string outputPath = "Assets/Fonts/AlimamaFangYuanTiVF-Thin SDF.asset";

        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            Debug.LogError("[GenerateTMPFont] 找不到字体: " + fontPath);
            return;
        }

        string chars = File.ReadAllText(charFile);
        string uniqueChars = new string(chars.Distinct().ToArray());

        Debug.Log($"[GenerateTMPFont] 字体: {font.name}, 字符数: {uniqueChars.Length}");

        var fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 4096, 4096, AtlasPopulationMode.Dynamic);

        if (fontAsset == null)
        {
            Debug.LogError("[GenerateTMPFont] CreateFontAsset 失败");
            return;
        }

        fontAsset.TryAddCharacters(uniqueChars, out string missing);

        if (!string.IsNullOrEmpty(missing))
            Debug.LogWarning($"[GenerateTMPFont] 未填充字符: {missing}");

        fontAsset.name = "AlimamaFangYuanTiVF-Thin SDF";
        AssetDatabase.CreateAsset(fontAsset, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GenerateTMPFont] 完成! 已保存到 {outputPath}");
    }
}
