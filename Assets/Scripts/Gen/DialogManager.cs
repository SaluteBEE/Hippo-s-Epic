using System;
using System.Collections.Generic;
using System.Linq;
using cfg.cfg.dialog;
using cfg.cfg.dialogcontent;
using UnityEngine;

public enum DialogState
{
    Idle,
    Playing,
    Ended
}

public class DialogManager : MonoBehaviour
{
    private static DialogManager _instance;
    public static DialogManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<DialogManager>();
            return _instance;
        }
    }

    public DialogState State { get; private set; } = DialogState.Idle;

    public event Action<SpeakerSide, string, string> OnContent;
    public event Action<List<OptionInfo>> OnOptions;
    public event Action OnDialogEnded;

    private cfg.Tables _tables;
    private Queue<Dialogcontent> _contentQueue;
    private Dialog _currentDialog;

    public void SetTables(cfg.Tables tables)
    {
        _tables = tables;
    }

    public void StartDialog(int dialogId)
    {
        if (_tables == null)
        {
            Debug.LogError("[DialogManager] Tables 未初始化");
            return;
        }

        var dialog = _tables.TbDialog.GetOrDefault(dialogId);
        if (dialog == null)
        {
            Debug.LogError($"[DialogManager] Dialog {dialogId} 不存在");
            return;
        }

        Debug.Log($"[DialogManager] 开始对话 {dialogId}, Type={dialog.Type}");
        _currentDialog = dialog;
        State = DialogState.Playing;
        PlayCurrentDialog();
    }

    public void Advance()
    {
        if (State != DialogState.Playing) return;

        if (_contentQueue != null && _contentQueue.Count > 0)
        {
            PlayNextContent();
            return;
        }

        ProcessCurrentDialogFlow();
    }

    public void ChooseOption(int index)
    {
        if (State != DialogState.Playing || _currentDialog == null) return;
        if (_currentDialog.Type != 2) return;
        if (index < 0 || index >= _currentDialog.Param1.Count) return;

        int nextId = _currentDialog.Param1[index];
        var nextDialog = _tables.TbDialog.GetOrDefault(nextId);
        if (nextDialog == null)
        {
            Debug.LogError($"[DialogManager] 选项目标 Dialog {nextId} 不存在");
            EndDialog();
            return;
        }

        _currentDialog = nextDialog;
        PlayCurrentDialog();
    }

    private void PlayCurrentDialog()
    {
        if (_currentDialog.Type == 2)
        {
            ShowOptions();
            return;
        }

        _contentQueue = new Queue<Dialogcontent>(
            _tables.TbDialogcontent.DataList
                .Where(c => c.Dialogid == _currentDialog.Id)
                .OrderBy(c => c.Sortid)
        );

        if (_contentQueue.Count == 0)
        {
            ProcessCurrentDialogFlow();
            return;
        }

        PlayNextContent();
    }

    private void PlayNextContent()
    {
        if (_contentQueue == null || _contentQueue.Count == 0) return;

        var content = _contentQueue.Dequeue();
        var side = ResolveSide(content);
        string speakerName = ResolveSpeakerName(content, side);

        OnContent?.Invoke(side, content.Content, speakerName);
    }

    private void ProcessCurrentDialogFlow()
    {
        if (_currentDialog == null)
        {
            EndDialog();
            return;
        }

        if (_currentDialog.Param1 == null || _currentDialog.Param1.Count == 0)
        {
            EndDialog();
            return;
        }

        int nextId = _currentDialog.Param1[0];
        var nextDialog = _tables.TbDialog.GetOrDefault(nextId);
        if (nextDialog == null)
        {
            EndDialog();
            return;
        }

        _currentDialog = nextDialog;
        PlayCurrentDialog();
    }

    private void ShowOptions()
    {
        var options = new List<OptionInfo>();
        foreach (int childId in _currentDialog.Param1)
        {
            var child = _tables.TbDialog.GetOrDefault(childId);
            if (child != null)
                options.Add(new OptionInfo(child.SelectionName, childId));
        }

        OnOptions?.Invoke(options);
    }

    private void EndDialog()
    {
        State = DialogState.Ended;
        _currentDialog = null;
        _contentQueue = null;
        OnDialogEnded?.Invoke();
    }

    private SpeakerSide ResolveSide(Dialogcontent content)
    {
        switch (content.Type)
        {
            case 0: return SpeakerSide.Middle;
            case 1: return SpeakerSide.Left;
            case 2: return SpeakerSide.Right;
            default: return SpeakerSide.Middle;
        }
    }

    private string ResolveSpeakerName(Dialogcontent content, SpeakerSide side)
    {
        var dialog = _tables.TbDialog.GetOrDefault(content.Dialogid);
        if (dialog == null) return "";

        switch (side)
        {
            case SpeakerSide.Left:
                return dialog.Speakerid1_Ref?.Name ?? "";
            case SpeakerSide.Right:
                return dialog.Speakerid2_Ref?.Name ?? "";
            default:
                return "";
        }
    }
}
