public class OptionInfo
{
    public string Text { get; }
    public int DialogId { get; }
    public int FirstContentType { get; }
    public int ConditionId { get; }

    public OptionInfo(string text, int dialogId, int firstContentType = 0, int conditionId = 0)
    {
        Text = text;
        DialogId = dialogId;
        FirstContentType = firstContentType;
        ConditionId = conditionId;
    }
}
