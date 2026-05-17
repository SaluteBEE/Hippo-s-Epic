using UnityEngine;

public class MainMenuPanel : UIWindow
{
    public void OnClickStart()
    {
        GameApp.Instance.StartGame(GameApp.Instance.StartMap);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }
}