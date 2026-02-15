
public sealed class OptionMessage
{
    public string Text1 { get; }
    public string Text2 { get; }
    public string Text3 { get; }

    public OptionMessage(string text1, string text2, string text3)
    {
        Text1 = text1;
        Text2 = text2;
        Text3 = text3;
    }
    public string GetByIndex(int index)
    {
        return index switch
        {
            0 => Text1,
            1 => Text2,
            2 => Text3,
            _ => ""
        };
    }
}