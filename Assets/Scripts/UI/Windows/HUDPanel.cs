public class HUDPanel : UIWindow
{
    public void OnClickPause()
    {
        ManagerRegistry.Get<AudioManager>()?.PlayUI("Click");
        GameApp.Instance.TogglePause();
    }
}