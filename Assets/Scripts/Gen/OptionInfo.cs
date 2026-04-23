public class OptionInfo
{
    public string Text { get; }
    public int DialogId { get; }

    public OptionInfo(string text, int dialogId)
    {
        Text = text;
        DialogId = dialogId;
    }
}
