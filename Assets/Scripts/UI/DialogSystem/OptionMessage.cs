
public sealed class OptionMessage
{
    public string Text1 { get; }
    public string Extend1 { get; }
    public string Text2 { get; }
    public string Extend2 { get; }
    public string Text3 { get; }
    public string Extend3 { get; }

    public OptionMessage(string text1, string extend1,string text2, string extend2 ,string text3,string extend3)
    {
        Text1 = text1;
        Extend1 = extend1;
        Text2 = text2;
        Extend2 = extend2;
        Text3 = text3;
        Extend3 = extend3;
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