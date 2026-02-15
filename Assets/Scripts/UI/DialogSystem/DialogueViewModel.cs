using System.Collections.Generic;
using Aesthete.MVVM.Properties;
using Aesthete.MVVM.ViewModels;
using UnityEngine;

public interface IDialogueGameState
{
    bool HasItem(string item);
    void AddItem(string item);
    bool EvaluateCondition(string condition);
}

/// <summary>
/// 临时默认实现：不做任何限制，条件永真；道具只做“记录”预留
/// </summary>
public sealed class DummyDialogueGameState : IDialogueGameState
{
    private readonly HashSet<string> _items = new HashSet<string>();

    public bool HasItem(string item) => _items.Contains(item);

    public void AddItem(string item)
    {
        if (!string.IsNullOrWhiteSpace(item))
            _items.Add(item);
    }

    public bool EvaluateCondition(string condition) => true;
}

public sealed class DialogueViewModel : ViewModel
{
    public Property<ChatMessage> OnMessagePushed { get; } = new Property<ChatMessage>();
    public Property<OptionMessage> OnOpMessagePushed { get; } = new Property<OptionMessage>();

    private Dictionary<int, DialogueRow> _table;
    private List<int> _sortedIds;

    private int _currentId;
    private bool _waitingOption;
    private bool _ended;

    private readonly List<DialogueRow> _currentOptions = new List<DialogueRow>();

    // 条件/道具系统预留：后续可从外部注入真实实现
    private IDialogueGameState _state = new DummyDialogueGameState();

    /// <summary>可选：外部注入真实状态系统</summary>
    public void SetGameState(IDialogueGameState state)
    {
        _state = state ?? new DummyDialogueGameState();
    }

    public bool IsEnded => _ended;
    public bool IsWaitingOption => _waitingOption;

    public void Initialize(TextAsset csv, int startId = 0)
    {
        _table = DialogueCsvLoader.Load(csv);
        _sortedIds = new List<int>(_table.Keys);
        _sortedIds.Sort();

        _currentId = startId;
        _waitingOption = false;
        _ended = false;
        _currentOptions.Clear();
    }

    /// <summary>
    /// 规则：河马永远在左侧，其它都在右侧
    /// </summary>
    public SpeakerSide ResolveSide(string speaker)
    {
        var s = speaker?.Trim();
        return s == "河马" ? SpeakerSide.Left : SpeakerSide.Right;
    }

    public void Advance()
    {
        if (_ended) return;
        if (_waitingOption) return;
        if (_table == null) return;
        if (!_table.TryGetValue(_currentId, out var row)) { _ended = true; return; }

        // 条件预留：不满足则跳过（策略：跳转优先，否则顺序下一条）
        if (!string.IsNullOrWhiteSpace(row.Condition) && !_state.EvaluateCondition(row.Condition))
        {
            MoveNext(row);
            return;
        }

        if (row.Type == DialogueType.结束)
        {
            _ended = true;
            return;
        }

        if (row.Type == DialogueType.普通)
        {
            var side = ResolveSide(row.Speaker);

            // 输出对白（若本句在右侧且有获得道具，则一次性带上 gainItemText，避免重复输出）
            if (side == SpeakerSide.Right)
            {
                var gain = string.IsNullOrWhiteSpace(row.GainItem) ? null : row.GainItem;
                PushRight(row.Text, gainItemText: gain);
            }
            else
            {
                PushLeft(row.Text);

                // 若获得道具：额外生成一个 RightBubble 的道具框（你的需求）
                if (!string.IsNullOrWhiteSpace(row.GainItem))
                {
                    _state.AddItem(row.GainItem);              // 预留：未来接入真实道具系统
                    PushRight("", gainItemText: row.GainItem); // 仅显示道具框
                }
            }

            MoveNext(row);
            return;
        }

        if (row.Type == DialogueType.选项)
        {
            _currentOptions.Clear();

            int id = _currentId;
            while (_table.TryGetValue(id, out var r) && r.Type == DialogueType.选项 && _currentOptions.Count < 3)
            {
                // 条件预留：不满足的选项隐藏（当前 Dummy 永真）
                if (string.IsNullOrWhiteSpace(r.Condition) || _state.EvaluateCondition(r.Condition))
                    _currentOptions.Add(r);

                id = GetNextId(id);
                if (id == -1) break;
            }

            string t1 = _currentOptions.Count > 0 ? _currentOptions[0].Text : "";
            string t2 = _currentOptions.Count > 1 ? _currentOptions[1].Text : "";
            string t3 = _currentOptions.Count > 2 ? _currentOptions[2].Text : "";

            PushOption(t1, t2, t3);
            _waitingOption = true;
            return;
        }

        // 未知类型：顺序推进
        _currentId = GetNextId(_currentId);
        if (_currentId == -1) _ended = true;
    }

    public void ChooseOption(int index)
    {
        if (_ended) return;
        if (!_waitingOption) return;
        if (index < 0 || index >= _currentOptions.Count) return;

        var chosen = _currentOptions[index];

        // 点击选项后生成对应内容的左侧气泡（你的需求）
        PushLeft(chosen.Text);

        // 若该选项也能获得道具（表格里填了），同样预留并显示
        if (!string.IsNullOrWhiteSpace(chosen.GainItem))
        {
            _state.AddItem(chosen.GainItem);
            PushRight("", gainItemText: chosen.GainItem);
        }

        _currentId = chosen.Jump ?? GetNextId(chosen.Id);
        if (_currentId == -1) _ended = true;

        _waitingOption = false;
    }

    public void PushLeft(string text)
        => OnMessagePushed.Value = new ChatMessage(SpeakerSide.Left, text);

    public void PushRight(string text, string gainItemText = null)
        => OnMessagePushed.Value = new ChatMessage(SpeakerSide.Right, text, gainItemText);

    public void PushOption(string t1, string t2, string t3)
        => OnOpMessagePushed.Value = new OptionMessage(t1, t2, t3);

    private void MoveNext(DialogueRow row)
    {
        _currentId = row.Jump ?? GetNextId(_currentId);
        if (_currentId == -1) _ended = true;
    }

    private int GetNextId(int id)
    {
        if (_sortedIds == null || _sortedIds.Count == 0) return -1;

        int idx = _sortedIds.BinarySearch(id);
        if (idx < 0) idx = ~idx - 1;

        int next = idx + 1;
        return (next >= 0 && next < _sortedIds.Count) ? _sortedIds[next] : -1;
    }

    public override void Bind() { }
    public override void Unbind() { }
    public override void Dispose() { }
}
