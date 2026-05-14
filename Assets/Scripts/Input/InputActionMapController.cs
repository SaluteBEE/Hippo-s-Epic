using System;
using System.Collections.Generic;
using UnityEngine;

public enum InputMode
{
    None,
    Player,
    UI,
    Dialog
}

public class InputActionMapController
{
    private readonly InputManager _inputManager;
    private readonly Stack<InputMode> _modeStack = new Stack<InputMode>();

    public InputMode CurrentMode => _modeStack.Count > 0 ? _modeStack.Peek() : InputMode.None;

    public event Action<InputMode> OnModeChanged;

    public InputActionMapController(InputManager inputManager)
    {
        _inputManager = inputManager;
    }

    public void PushMode(InputMode mode)
    {
        _modeStack.Push(mode);
        ApplyMode(mode);
        OnModeChanged?.Invoke(mode);
    }

    public void PopMode()
    {
        if (_modeStack.Count == 0) return;

        _modeStack.Pop();

        InputMode newMode = _modeStack.Count > 0 ? _modeStack.Peek() : InputMode.None;
        ApplyMode(newMode);
        OnModeChanged?.Invoke(newMode);
    }

    public void PopToMode(InputMode mode)
    {
        while (_modeStack.Count > 0 && _modeStack.Peek() != mode)
            _modeStack.Pop();

        InputMode current = _modeStack.Count > 0 ? _modeStack.Peek() : InputMode.None;
        ApplyMode(current);
        OnModeChanged?.Invoke(current);
    }

    public void ClearAll()
    {
        _modeStack.Clear();
        ApplyMode(InputMode.None);
        OnModeChanged?.Invoke(InputMode.None);
    }

    private void ApplyMode(InputMode mode)
    {
        switch (mode)
        {
            case InputMode.Player:
                _inputManager.EnableOnlyPlayer();
                break;
            case InputMode.UI:
                _inputManager.EnableOnlyUI();
                break;
            case InputMode.Dialog:
                _inputManager.EnableOnlyDialog();
                break;
            default:
                _inputManager.DisableAll();
                break;
        }
    }
}