public enum SpeakerSide
{
    Left,
    Right
}
public sealed class ChatMessage
{
    public SpeakerSide Side { get; }
    public string Text { get; }
    public string GainItemText { get; } // 可空（用于 RightBubble 内框）

    public ChatMessage(SpeakerSide side, string text, string gainItemText = null)
    {
        Side = side;
        Text = text;
        GainItemText = gainItemText;
    }
}
