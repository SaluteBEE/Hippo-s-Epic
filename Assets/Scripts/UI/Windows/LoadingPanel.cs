using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : UIWindow
{
    public override UILayer Layer => UILayer.Top;

    [SerializeField] private Slider slider;

    public void SetProgress(float value)
    {
        if (slider != null)
            slider.value = value;
    }
}