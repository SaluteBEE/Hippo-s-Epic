using System.Collections.Generic;
using UnityEngine;
using System;

public class MainMenuPanel : UIWindow
{
    [SerializeField] private string gameSceneName = "Level01";

    public void OnClickStart()
    {
        GameApp.Instance.StartGame(gameSceneName);
    }

    public void OnClickQuit()
    {
        Application.Quit();
    }
}