using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 控制气泡“高亮/置灰”：最新气泡保持原色，其余置灰
/// </summary>
public sealed class BubbleTintController : MonoBehaviour
{
    [Header("Auto Collect")]
    [SerializeField] private bool includeTexts = false; // 默认只改 Image，不改文字颜色

    [Header("Tint")]
    [SerializeField] private Color dimColor = new Color(0.75f, 0.75f, 0.75f, 1f);

    private Image[] _images;
    private TMP_Text[] _texts;

    private readonly List<Color> _imageOriginal = new List<Color>();
    private readonly List<Color> _textOriginal = new List<Color>();

    private bool _inited;

    private void Awake()
    {
        InitIfNeeded();
    }

    private void InitIfNeeded()
    {
        if (_inited) return;
        _inited = true;

        _images = GetComponentsInChildren<Image>(includeInactive: true);
        _imageOriginal.Clear();
        for (int i = 0; i < _images.Length; i++)
            _imageOriginal.Add(_images[i].color);

        _texts = GetComponentsInChildren<TMP_Text>(includeInactive: true);
        _textOriginal.Clear();
        for (int i = 0; i < _texts.Length; i++)
            _textOriginal.Add(_texts[i].color);
    }

    /// <summary>
    /// dimmed=true：置灰；dimmed=false：恢复原色
    /// </summary>
    public void SetDimmed(bool dimmed)
    {
        InitIfNeeded();

        for (int i = 0; i < _images.Length; i++)
            _images[i].color = dimmed ? dimColor : _imageOriginal[i];

        if (includeTexts)
        {
            for (int i = 0; i < _texts.Length; i++)
                _texts[i].color = dimmed ? dimColor : _textOriginal[i];
        }
    }
}