using System;
using TMPro;
using UnityEngine;

public class TypewriterEffect : MonoBehaviour
{
    private TMP_Text _tmpText;
    private int _totalChars;
    private float _charsPerSecond;
    private float _timer;
    private bool _isTyping;
    private Action _onComplete;

    public bool IsTyping => _isTyping;

    public void Play(TMP_Text tmpText, float charsPerSecond, Action onComplete = null)
    {
        _tmpText = tmpText;
        _charsPerSecond = charsPerSecond;
        _totalChars = tmpText.textInfo.characterCount;
        _timer = 0f;
        _isTyping = true;
        _onComplete = onComplete;
        _tmpText.maxVisibleCharacters = 0;
        enabled = true;
    }

    public void Skip()
    {
        if (!_isTyping) return;
        _isTyping = false;
        _tmpText.maxVisibleCharacters = _totalChars;
        enabled = false;
        _onComplete?.Invoke();
    }

    private void Update()
    {
        if (!_isTyping) return;

        _timer += Time.deltaTime * _charsPerSecond;
        int target = Mathf.Min(Mathf.FloorToInt(_timer), _totalChars);
        _tmpText.maxVisibleCharacters = target;

        if (target >= _totalChars)
        {
            _isTyping = false;
            enabled = false;
            _onComplete?.Invoke();
        }
    }
}
