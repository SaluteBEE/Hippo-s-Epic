using System.Collections.Generic;
using Aesthete.MVVM.Properties;
using Aesthete.MVVM.ViewModels;
using UnityEngine;

public sealed class DialogueViewModel : ViewModel
{
    public Property<ChatMessage> OnMessagePushed { get; } = new Property<ChatMessage>();
    public Property<OptionMessage> OnOpMessagePushed { get; } = new Property<OptionMessage>();

    private Dictionary<int, DialogueRow> _table;
    private List<int> _sortedIds;

    private int _currentId;
    private bool _waitingOption;
    private readonly List<DialogueRow> _currentOptions = new List<DialogueRow>();

    public void Initialize(TextAsset csv, int startId = 0)
    {
        _table = DialogueCsvLoader.Load(csv);
        _sortedIds = new List<int>(_table.Keys);
        _sortedIds.Sort();

        _currentId = startId;
        _waitingOption = false;
        _currentOptions.Clear();
    }

    public void Advance()
    {
        // 等待选项时，不允许点击推进（你也可以改为允许）
        if (_waitingOption) return;
        if (_table == null) return;

        if (!_table.TryGetValue(_currentId, out var row)) return;

        if (row.Flag == '#')
        {
            // 普通对白
            if (row.Position == "右") PushRight(row.Content);
            else PushLeft(row.Content);

            _currentId = row.Jump ?? GetNextId(_currentId);
            return;
        }

        if (row.Flag == '&')
        {
            // 选项组：从当前ID开始收集连续 '&'（最多3条）
            _currentOptions.Clear();

            int id = _currentId;
            while (_table.TryGetValue(id, out var r) && r.Flag == '&' && _currentOptions.Count < 3)
            {
                _currentOptions.Add(r);
                id = GetNextId(id);
                if (id == -1) break;
            }

            var t1 = _currentOptions.Count > 0 ? _currentOptions[0].Content : "";
            var t2 = _currentOptions.Count > 1 ? _currentOptions[1].Content : "";
            var t3 = _currentOptions.Count > 2 ? _currentOptions[2].Content : "";

            OnOpMessagePushed.Value = new OptionMessage(t1, t2, t3);
            _waitingOption = true;
            return;
        }

        // 未知标记：顺序推进
        _currentId = GetNextId(_currentId);
    }

    public void ChooseOption(int index)
    {
        if (!_waitingOption) return;
        if (index < 0 || index >= _currentOptions.Count) return;

        var chosen = _currentOptions[index];

        // 你的需求：点击选项后生成对应内容的左侧气泡
        PushLeft(chosen.Content);

        // 跳转到该选项行 Jump；无 Jump 就顺序下一条
        _currentId = chosen.Jump ?? GetNextId(chosen.Id);

        _waitingOption = false;
    }

    private void PushLeft(string text)
        => OnMessagePushed.Value = new ChatMessage(SpeakerSide.Left, text);

    private void PushRight(string text)
        => OnMessagePushed.Value = new ChatMessage(SpeakerSide.Right, text);

    private int GetNextId(int id)
    {
        int idx = _sortedIds.BinarySearch(id);
        if (idx < 0) idx = ~idx - 1;

        int next = idx + 1;
        return (next >= 0 && next < _sortedIds.Count) ? _sortedIds[next] : -1;
    }

    public override void Bind() { }
    public override void Unbind() { }
    public override void Dispose() { }
}
