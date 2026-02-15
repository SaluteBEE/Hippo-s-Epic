using System;

public sealed class DialogueRow
{
    public char Flag;          // '#' 普通对白, '&' 选项行
    public int Id;             // ID
    public string Character;   // 人物
    public string Position;    // 位置：左/右
    public string Content;     // 内容
    public int? Jump;          // 跳转（可能为空）
    public string Effect;      // 效果（可能为空）
    public string Target;      // 目标（可能为空）
}