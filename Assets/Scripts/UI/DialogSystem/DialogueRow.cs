public enum DialogueType
{
    普通,
    选项,
    结束
}

public sealed class DialogueRow
{
    public int Id;
    public DialogueType Type;
    public string Speaker;        // 说话人
    public string Text;           // 对话文本
    public string Text2;
    public string Condition;      // 条件（预留）
    public int? Jump;             // 跳转（可空）
    public string Emotion;        // 表情（预留）
    public string Background;     // 背景变化（预留）
    public string GainItem;       // 获得道具（预留 + UI显示）
}