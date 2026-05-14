using UnityEngine;

[DefaultExecutionOrder(-100)]
public class BootstrapQuickStart : MonoBehaviour
{
    private InputManager _inputManager;
    private bool _ownsInput;

    private void Awake()
    {
        _inputManager = ManagerRegistry.Get<InputManager>();
        if (_inputManager == null)
        {
            var go = new GameObject("[InputManager]");
            _inputManager = go.AddComponent<InputManager>();
            _ownsInput = true;
        }

        if (ManagerRegistry.Get<DataTableManager>() == null)
        {
            var dataTable = GetComponentInChildren<DataTableManager>(true);
            if (dataTable != null)
                ManagerRegistry.Register(dataTable);
        }

        ManagerRegistry.Register(this);
    }

    private void Start()
    {
        var dataTable = ManagerRegistry.Get<DataTableManager>();
        if (dataTable != null && !dataTable.IsLoaded)
            dataTable.LoadTables();

        if (dataTable != null && dataTable.IsLoaded)
        {
            var tables = dataTable.Tables;

            var dialogMgr = ManagerRegistry.Get<DialogManager>();
            if (dialogMgr != null)
                dialogMgr.SetTables(tables);

            var animStateMgr = ManagerRegistry.Get<AnimationStateManager>();
            if (animStateMgr != null)
                animStateMgr.SetTables(tables);

            var charMgr = ManagerRegistry.Get<DialogCharacterManager>();
            if (charMgr != null && tables != null)
                charMgr.Initialize(tables);
        }

        if (_inputManager != null)
            _inputManager.EnablePlayerAndDialog();

        Debug.Log("[BootstrapQuickStart] 快速启动初始化完成");
    }

    public void EnterDialogMode()
    {
        if (_inputManager != null)
            _inputManager.EnableOnlyDialog();
    }

    public void ExitDialogMode()
    {
        if (_inputManager != null)
            _inputManager.EnablePlayerAndDialog();
    }

    private void OnDestroy()
    {
        ManagerRegistry.Unregister<BootstrapQuickStart>();

        if (_ownsInput && _inputManager != null)
        {
            ManagerRegistry.Unregister<InputManager>();
        }
    }
}
