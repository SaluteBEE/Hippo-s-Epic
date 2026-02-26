using UnityEngine;

public sealed class DialogueOpenArgs
{
    public TextAsset Csv;
    public int StartId;
    public bool AutoAdvanceFirstLine;

    public DialogueOpenArgs(TextAsset csv, int startId = 1, bool autoAdvanceFirstLine = true)
    {
        Csv = csv;
        StartId = startId;
        AutoAdvanceFirstLine = autoAdvanceFirstLine;
    }
}