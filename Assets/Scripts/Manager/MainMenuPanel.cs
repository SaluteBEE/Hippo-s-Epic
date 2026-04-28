using UnityEngine;

public class MainMenuPanel : UIWindow
{
    public void OnClickStart()
    {
        GameApp.Instance.StartGame(GameApp.Instance.GameSceneName);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }
}