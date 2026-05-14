using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputRebindingManager
{
    private readonly InputManager _inputManager;
    private InputActionRebindingExtensions.RebindingOperation _currentRebind;

    public bool IsRebinding => _currentRebind != null;
    public event Action<string, int> OnRebindStarted;
    public event Action<string, int, bool> OnRebindCompleted;

    private const string SaveKey = "InputBindings";

    public InputRebindingManager(InputManager inputManager)
    {
        _inputManager = inputManager;
    }

    public void StartRebind(string actionName, int bindingIndex)
    {
        if (IsRebinding) return;

        InputAction action = FindAction(actionName);
        if (action == null)
        {
            Debug.LogWarning($"[InputRebinding] 未找到 Action: {actionName}");
            return;
        }

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            Debug.LogWarning($"[InputRebinding] bindingIndex 越界: {bindingIndex}");
            return;
        }

        _currentRebind = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .OnComplete(op =>
            {
                bool success = true;
                OnRebindCompleted?.Invoke(actionName, bindingIndex, success);
                op.Dispose();
                _currentRebind = null;
                SaveBindings();
            })
            .OnCancel(op =>
            {
                OnRebindCompleted?.Invoke(actionName, bindingIndex, false);
                op.Dispose();
                _currentRebind = null;
            })
            .Start();

        OnRebindStarted?.Invoke(actionName, bindingIndex);
    }

    public void CancelRebind()
    {
        if (_currentRebind == null) return;
        _currentRebind.Cancel();
    }

    public void SaveBindings()
    {
        string json = _inputManager.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public void LoadBindings()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            _inputManager.LoadBindingOverrides(json);
        }
    }

    public string GetBindingDisplayString(string actionName, int bindingIndex)
    {
        InputAction action = FindAction(actionName);
        if (action == null) return "";
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count) return "";

        string path = action.bindings[bindingIndex].effectivePath;

        int slashIndex = path.LastIndexOf('/');
        if (slashIndex >= 0)
            return path.Substring(slashIndex + 1);

        return path;
    }

    public void ResetToDefault()
    {
        _inputManager.GameInput.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    private InputAction FindAction(string actionName)
    {
        var asset = _inputManager.GameInput.asset;
        return asset.FindAction(actionName);
    }
}