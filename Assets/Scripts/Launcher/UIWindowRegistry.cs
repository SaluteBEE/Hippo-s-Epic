using UnityEngine;

public static class UIWindowRegistry
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterAll()
    {
        UIManager.RegisterAddress(typeof(DialogWindow), "ui/Dialog/DialogWindow");
        UIManager.RegisterAddress(typeof(MainMenuPanel), "ui/Global/MainMenuPanel");
        UIManager.RegisterAddress(typeof(LoadingPanel), "ui/Global/LoadingPanel");
        UIManager.RegisterAddress(typeof(BagPanel), "ui/Bag/BagPanel");
        UIManager.RegisterAddress(typeof(UseItemTip), "ui/Bag/UseItemTip");
        UIManager.RegisterAddress(typeof(TaskPanel), "ui/task/TaskPanel");
        UIManager.RegisterAddress(typeof(MapPanel), "ui/map/MapPanel");
        UIManager.RegisterAddress(typeof(MainPanelController), "ui/Global/MainPanel");
    }
}
