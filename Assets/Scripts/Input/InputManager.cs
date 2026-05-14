using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    public GameInput GameInput { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameInput = new GameInput();

        ManagerRegistry.Register(this);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        ManagerRegistry.Unregister<InputManager>();
        GameInput?.Dispose();
    }

    public void EnablePlayer()
    {
        GameInput.Player.Enable();
    }

    public void DisablePlayer()
    {
        GameInput.Player.Disable();
    }

    public void EnableUI()
    {
        GameInput.UI.Enable();
    }

    public void DisableUI()
    {
        GameInput.UI.Disable();
    }

    public void EnableDialog()
    {
        GameInput.Dialog.Enable();
    }

    public void DisableDialog()
    {
        GameInput.Dialog.Disable();
    }

    public void EnableOnlyPlayer()
    {
        GameInput.UI.Disable();
        GameInput.Dialog.Disable();
        GameInput.Player.Enable();
    }

    public void EnableOnlyUI()
    {
        GameInput.Player.Disable();
        GameInput.Dialog.Disable();
        GameInput.UI.Enable();
    }

    public void EnableOnlyDialog()
    {
        GameInput.Player.Disable();
        GameInput.UI.Disable();
        GameInput.Dialog.Enable();
    }

    public void EnablePlayerAndUI()
    {
        GameInput.Player.Enable();
        GameInput.UI.Enable();
        GameInput.Dialog.Disable();
    }

    public void EnablePlayerAndDialog()
    {
        GameInput.Player.Enable();
        GameInput.Dialog.Enable();
        GameInput.UI.Disable();
    }

    public void DisableAll()
    {
        GameInput.Player.Disable();
        GameInput.UI.Disable();
        GameInput.Dialog.Disable();
    }

    public void LoadBindingOverrides(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        GameInput.LoadBindingOverridesFromJson(json);
    }

    public string SaveBindingOverridesAsJson()
    {
        return GameInput.SaveBindingOverridesAsJson();
    }
}