using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DialogAutoWalker : MonoBehaviour
{
    private DataTableManager _dataTable;
    private DialogManager _dialogManager;
    private readonly StringBuilder _log = new StringBuilder();

    private Queue<System.Action> _testQueue;
    private int[] _currentChoices;
    private int _choiceIdx;
    private bool _waitingChoice;

    private void Awake()
    {
        var dtObj = new GameObject("DataTableManager");
        dtObj.transform.SetParent(transform);
        _dataTable = dtObj.AddComponent<DataTableManager>();
        _dataTable.LoadTables();

        var dmObj = new GameObject("DialogManager");
        dmObj.transform.SetParent(transform);
        _dialogManager = dmObj.AddComponent<DialogManager>();
        _dialogManager.SetTables(_dataTable.Tables);

        _dialogManager.OnContent += OnContent;
        _dialogManager.OnOptions += OnOptions;
        _dialogManager.OnDialogEnded += OnDialogEnded;
    }

    private void Start()
    {
        _testQueue = new Queue<System.Action>();
        _testQueue.Enqueue(() => Run("===== 对话A: 线性对话 ID=100 =====", 100));
        _testQueue.Enqueue(() => RunChoices("===== 对话D: 纯旁白 ID=400 =====", 400, new int[0]));
        _testQueue.Enqueue(() => RunChoices("===== 对话B: 循环选项 ID=200 (先选1再选2) =====", 200, new[] { 0, 1 }));
        _testQueue.Enqueue(() => RunChoices("===== 对话C: 嵌套 ID=300 (选1→选1关于拳王) =====", 300, new[] { 0, 0 }));
        _testQueue.Enqueue(() => RunChoices("===== 对话C: 嵌套 ID=300 (选1→选2关于小镇) =====", 300, new[] { 0, 1 }));
        _testQueue.Enqueue(() => RunChoices("===== 对话C: 嵌套 ID=300 (选2转身离开) =====", 300, new[] { 1 }));
        _testQueue.Enqueue(() => RunChoices("===== 原始 ID=1001001 (选1跳舞) =====", 1001001, new[] { 0 }));
        _testQueue.Enqueue(() => RunChoices("===== 原始 ID=1001001 (选2一拳) =====", 1001001, new[] { 1 }));
        _testQueue.Enqueue(() => RunChoices("===== 原始 ID=1001001 (选3→9之后是10) =====", 1001001, new[] { 2 }));

        NextTest();
    }

    private void NextTest()
    {
        if (_testQueue.Count > 0)
        {
            _started = true;
            _testQueue.Dequeue()();
        }
        else
        {
            L("\n========== 全部测试完成 ==========");
            Debug.Log("<color=green>" + _log.ToString() + "</color>");
        }
    }

    private void Run(string header, int dialogId)
    {
        L("\n" + header);
        _currentChoices = new int[0];
        _choiceIdx = 0;
        _waitingChoice = false;
        _dialogManager.StartDialog(dialogId);
    }

    private void RunChoices(string header, int dialogId, int[] choices)
    {
        L("\n" + header);
        _currentChoices = choices;
        _choiceIdx = 0;
        _waitingChoice = false;
        _dialogManager.StartDialog(dialogId);
    }

    private int _frame;
    private bool _started;
    private void Update()
    {
        _frame++;
        if (!_started) return;
        if (_dialogManager.State != DialogState.Playing)
        {
            Debug.Log($"[Walker] 帧{_frame} State={_dialogManager.State}, 跳过Advance");
            return;
        }
        if (_waitingChoice)
        {
            Debug.Log($"[Walker] 帧{_frame} 等待选择, 跳过Advance");
            return;
        }

        Debug.Log($"[Walker] 帧{_frame} Advance");
        _dialogManager.Advance();
    }

    private void OnContent(SpeakerSide side, string text, string speaker, string gainItemText)
    {
        string pos = side == SpeakerSide.Left ? "左" : side == SpeakerSide.Right ? "右" : "中";
        string prefix = string.IsNullOrEmpty(speaker) ? $"[{pos}]" : $"[{pos}-{speaker}]";
        L($"  {prefix} {Truncate(text, 60)}");
    }

    private void OnOptions(List<OptionInfo> opts)
    {
        for (int i = 0; i < opts.Count; i++)
            L($"  ◆ 选项{i + 1}: {opts[i].Text}");

        if (_currentChoices != null && _choiceIdx < _currentChoices.Length)
        {
            int pick = _currentChoices[_choiceIdx++];
            L($"  ▶ 选择: {pick + 1}");
            _dialogManager.ChooseOption(pick);
        }
        else
        {
            _waitingChoice = true;
        }
    }

    private void OnDialogEnded()
    {
        L("  -- 对话结束 --");
        NextTest();
    }

    private void L(string msg)
    {
        _log.AppendLine(msg);
        Debug.Log(msg);
    }

    private static string Truncate(string s, int max)
    {
        if (s == null) return "";
        string clean = s.Replace("<color=red>", "").Replace("</color>", "");
        return clean.Length <= max ? clean : clean.Substring(0, max) + "...";
    }
}
