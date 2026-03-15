public class HUDPanel : UIWindow
{
    public void OnClickPause()
    {
        GameApp.Instance.Audio.PlayUI("Click");
        GameApp.Instance.TogglePause();
    }
}