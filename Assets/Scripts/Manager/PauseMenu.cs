public class PausePanel : UIWindow
{
    public override UILayer Layer => UILayer.Popup;

    public void OnClickResume()
    {
        GameApp.Instance.ResumeGame();
    }

    public void OnClickMainMenu()
    {
        GameApp.Instance.GoMainMenu();
    }
}