public enum SpeakerSide
{
    Left,
    Right
}

public sealed class ChatMessage
{
    public SpeakerSide Side { get; }
    public string Text { get; }

    public ChatMessage(SpeakerSide side, string text)
    {
        Side = side;
        Text = text;
    }
}