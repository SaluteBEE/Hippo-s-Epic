using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    Settings
}

public class GameStateManager : SingletonMonoBehaviour<GameStateManager>
{
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    public event Action<GameState, GameState> OnStateChanged;
    
    
    private void SwitchState(GameState next)
    {
        if (next == CurrentState) return;

        var prev = CurrentState;
        CurrentState = next;

        // 状态副作用（UI/TimeScale/Audio）
        ApplyState(prev, next);

        OnStateChanged?.Invoke(prev, next);
    }

    private void ApplyState(GameState prev, GameState next)
    {
        // 默认恢复时间流速，只有暂停才置 0
        Time.timeScale = (next == GameState.Paused) ? 0f : 1f;
        switch (next)
        {
            case GameState.MainMenu:
               // UIManager.Instance.ShowMainMenu();
                
               
                break;

            case GameState.Playing:
                
                break;

            case GameState.Paused:
                
                
                break;

            case GameState.Settings:
                
                break;

 
        }
    }

    // ====== 对外流程接口（给按钮或输入调用）======


 
    public void StartGame()
    {
       
    }
    


    public void ReturnToMainMenu()
    {
        SwitchState(GameState.MainMenu);
    }

    public void Pause()
    {
        if (CurrentState == GameState.Playing)
            SwitchState(GameState.Paused);
    }

    public void Resume()
    {
        if (CurrentState == GameState.Paused)
            SwitchState(GameState.Playing);
    }

    public void OpenSettings()
    {
        // 从主菜单/暂停/游戏中都允许打开设置
        SwitchState(GameState.Settings);
    }




    
}
