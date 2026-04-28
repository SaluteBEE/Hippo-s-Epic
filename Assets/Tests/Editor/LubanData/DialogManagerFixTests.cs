using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

class DialogManagerFixTests
{
    private DialogManager _mgr;
    private cfg.Tables _tables;

    private List<(SpeakerSide side, string text, string speaker)> _contentLog;
    private List<List<OptionInfo>> _optionsLog;
    private List<(int speakerid1, int speakerid2)> _dialogStartLog;
    private int _dialogEndedCount;
    private List<(int personId, string state)> _emotionLog;
    private List<(int personId, int slotStateId)> _slotStateLog;

    [SetUp]
    public void Setup()
    {
        var go = new UnityEngine.GameObject("TestDM_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        _mgr = go.AddComponent<DialogManager>();

        var dtObj = new UnityEngine.GameObject("TestDT_" + System.Guid.NewGuid().ToString("N").Substring(0, 8));
        var dt = dtObj.AddComponent<DataTableManager>();
        dt.LoadTables();
        _tables = dt.Tables;
        UnityEngine.Object.DestroyImmediate(dtObj);

        _mgr.SetTables(_tables);

        _contentLog = new List<(SpeakerSide, string, string)>();
        _optionsLog = new List<List<OptionInfo>>();
        _dialogStartLog = new List<(int, int)>();
        _dialogEndedCount = 0;
        _emotionLog = new List<(int, string)>();
        _slotStateLog = new List<(int, int)>();

        _mgr.OnContent += (side, text, speaker) => _contentLog.Add((side, text, speaker));
        _mgr.OnOptions += opts => _optionsLog.Add(opts);
        _mgr.OnDialogEnded += () => _dialogEndedCount++;
        _mgr.OnEmotion += (pid, state) => _emotionLog.Add((pid, state));
        _mgr.OnSlotState += (pid, ssid) => _slotStateLog.Add((pid, ssid));
    }

    [TearDown]
    public void TearDown()
    {
        if (_mgr != null)
            UnityEngine.Object.DestroyImmediate(_mgr.gameObject);
        ManagerRegistry.Clear();
    }

    #region BUG-3: 无订阅者时直接播放

    [Test]
    public void StartDialog_NoSubscriber_PlaysDirectly()
    {
        _mgr.StartDialog(1);
        Assert.AreEqual(DialogState.Playing, _mgr.State);
        Assert.AreEqual(1, _contentLog.Count, "无 OnDialogStart 订阅者时应直接播放第一条");
    }

    [Test]
    public void StartDialog_NoSubscriber_AllContentPlayable()
    {
        _mgr.StartDialog(1);
        for (int i = 1; i < 9; i++)
            _mgr.Advance();
        Assert.AreEqual(9, _contentLog.Count, "Dialog 1 应有 9 条内容全部可播放");
    }

    #endregion

    #region BUG-1: 防重入保护

    [Test]
    public void StartDialog_WhilePlaying_EndsPreviousFirst()
    {
        _mgr.StartDialog(1);
        _mgr.StartDialog(100);
        Assert.AreEqual(1, _dialogEndedCount, "重新开始对话时应触发 OnDialogEnded");
        Assert.AreEqual(DialogState.Playing, _mgr.State);
    }

    [Test]
    public void StartDialog_WhilePlaying_ClearsOldContent()
    {
        _mgr.StartDialog(1);
        _mgr.StartDialog(100);
        _contentLog.Clear();
        _mgr.Advance();
        Assert.AreEqual(1, _contentLog.Count, "应播放 Dialog 100 的内容");
    }

    [Test]
    public void StartDialog_9Advances_ReachesOptions()
    {
        _mgr.StartDialog(1);
        int safety = 30;
        while (_mgr.State == DialogState.Playing && _optionsLog.Count == 0 && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(DialogState.Playing, _mgr.State, "Dialog 1 播完应进入选项");
        Assert.AreEqual(1, _optionsLog.Count);
    }

    #endregion

    #region BUG-3 + BUG-2: 有订阅者时等待 NotifyCharactersReady

    [Test]
    public void StartDialog_WithSubscriber_WaitsForNotify()
    {
        _mgr.OnDialogStart += (s1, s2) => _dialogStartLog.Add((s1, s2));
        _mgr.StartDialog(1);

        Assert.AreEqual(0, _contentLog.Count, "有订阅者时应等待");
        Assert.AreEqual(1, _dialogStartLog.Count);

        _mgr.Advance();
        Assert.AreEqual(0, _contentLog.Count, "未 Notify 前 Advance 不应播放");

        _mgr.NotifyCharactersReady();
        Assert.AreEqual(1, _contentLog.Count, "Notify 后应开始播放");
    }

    [Test]
    public void NotifyCharactersReady_WithoutStart_Ignored()
    {
        _mgr.OnDialogStart += (s1, s2) => { };
        _mgr.NotifyCharactersReady();
        Assert.AreEqual(0, _contentLog.Count);
    }

    #endregion

    #region BUG-2: TransitionToDialog speaker 变化

    [Test]
    public void ChooseOption_SameSpeakers_NoExtraDialogStart()
    {
        _mgr.OnDialogStart += (s1, s2) => _dialogStartLog.Add((s1, s2));
        _mgr.StartDialog(1);
        _mgr.NotifyCharactersReady();
        int safety = 30;
        while (_mgr.State == DialogState.Playing && _optionsLog.Count == 0 && safety-- > 0)
            _mgr.Advance();

        _dialogStartLog.Clear();
        _mgr.ChooseOption(0);
        Assert.AreEqual(0, _dialogStartLog.Count, "speaker 未变不应触发 OnDialogStart");
    }

    [Test]
    public void AutoAdvance_SpeakerChanged_FiresOnDialogStart()
    {
        _mgr.OnDialogStart += (s1, s2) => _dialogStartLog.Add((s1, s2));
        _mgr.StartDialog(100);
        _dialogStartLog.Clear();

        var dialog100 = _tables.TbDialog.Get(100);
        var dialog101 = _tables.TbDialog.Get(101);
        bool speakersChanged = dialog100.Speakerid1 != dialog101.Speakerid1
            || dialog100.Speakerid2 != dialog101.Speakerid2;

        if (!speakersChanged)
        {
            Assert.Pass("Dialog 100→101 speaker 未变，跳过（代码正确不触发）");
            return;
        }

        int safety = 50;
        while (_mgr.State == DialogState.Playing && safety-- > 0)
        {
            _mgr.Advance();
            if (_dialogStartLog.Count > 0) break;
        }

        Assert.AreEqual(1, _dialogStartLog.Count, "speaker 变化应触发 OnDialogStart");
    }

    [Test]
    public void AutoAdvance_SameSpeakers_DoesNotFireOnDialogStart()
    {
        var dialog100 = _tables.TbDialog.Get(100);
        var dialog101 = _tables.TbDialog.GetOrDefault(dialog100.Param1[0]);
        if (dialog101 == null) { Assert.Pass("无后续 Dialog"); return; }

        bool speakersChanged = dialog100.Speakerid1 != dialog101.Speakerid1
            || dialog100.Speakerid2 != dialog101.Speakerid2;
        if (speakersChanged) { Assert.Pass("speaker 确实变了，跳过"); return; }

        _mgr.OnDialogStart += (s1, s2) => _dialogStartLog.Add((s1, s2));
        _mgr.StartDialog(100);
        _dialogStartLog.Clear();
        _mgr.NotifyCharactersReady();

        int safety = 50;
        while (_mgr.State == DialogState.Playing && safety-- > 0)
            _mgr.Advance();

        Assert.AreEqual(0, _dialogStartLog.Count, "speaker 未变时不应触发额外 OnDialogStart");
    }

    [Test]
    public void AutoAdvance_SpeakerChanged_BlocksContentUntilNotify()
    {
        bool speakersChanged = false;
        var d100 = _tables.TbDialog.Get(100);
        if (d100.Param1.Count > 0)
        {
            var d101 = _tables.TbDialog.GetOrDefault(d100.Param1[0]);
            if (d101 != null)
                speakersChanged = d100.Speakerid1 != d101.Speakerid1
                    || d100.Speakerid2 != d101.Speakerid2;
        }

        if (!speakersChanged) { Assert.Pass("speaker 未变，跳过"); return; }

        _mgr.OnDialogStart += (s1, s2) => { };
        _mgr.StartDialog(100);
        _mgr.NotifyCharactersReady();

        int advanceCount = 0;
        while (_mgr.State == DialogState.Playing && advanceCount < 30)
        {
            _mgr.Advance();
            advanceCount++;
        }

        Assert.AreEqual(DialogState.Playing, _mgr.State, "speaker 变化后应等待 NotifyCharactersReady");
    }

    #endregion

    #region BUG-4: 线性 Dialog 自动推进

    [Test]
    public void LinearDialog_AutoAdvancesToEnd()
    {
        _mgr.StartDialog(100);
        int safety = 50;
        while (_mgr.State == DialogState.Playing && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(DialogState.Ended, _mgr.State, "线性对话应推进到结束");
    }

    [Test]
    public void LinearDialog_ProducesContent()
    {
        _mgr.StartDialog(100);
        int safety = 50;
        while (_mgr.State == DialogState.Playing && safety-- > 0)
            _mgr.Advance();
        Assert.Greater(_contentLog.Count, 0, "线性对话应有内容输出");
    }

    #endregion

    #region ChooseOption 边界

    [Test]
    public void ChooseOption_InvalidIndex_DoesNotThrow()
    {
        _mgr.StartDialog(1);
        Assert.DoesNotThrow(() => _mgr.ChooseOption(-1));
        Assert.DoesNotThrow(() => _mgr.ChooseOption(99));
    }

    [Test]
    public void ChooseOption_WhileIdle_Ignored()
    {
        Assert.DoesNotThrow(() => _mgr.ChooseOption(0));
        Assert.AreEqual(DialogState.Idle, _mgr.State);
    }

    [Test]
    public void ChooseOption_OnLinearDialog_Ignored()
    {
        _mgr.StartDialog(1);
        int before = _contentLog.Count;
        _mgr.ChooseOption(0);
        Assert.AreEqual(before, _contentLog.Count);
    }

    [Test]
    public void ChooseOption_OnOptionDialog_Transitions()
    {
        _mgr.StartDialog(1);
        int safety = 30;
        while (_mgr.State == DialogState.Playing && _optionsLog.Count == 0 && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(1, _optionsLog.Count);
        _mgr.ChooseOption(0);
        Assert.AreEqual(DialogState.Playing, _mgr.State);
    }

    #endregion

    #region Advance 边界

    [Test]
    public void Advance_WhileEnded_Ignored()
    {
        _mgr.StartDialog(4);
        int safety = 30;
        while (_mgr.State == DialogState.Playing && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(DialogState.Ended, _mgr.State);

        int before = _contentLog.Count;
        _mgr.Advance();
        Assert.AreEqual(before, _contentLog.Count);
    }


    #endregion

    #region Emotion/SlotState 触发

    [Test]
    public void PlayNextContent_FiresEmotion_WhenStatenamePresent()
    {
        var contents = _tables.TbDialogcontent.DataList
            .Where(c => c.Dialogid == 1).OrderBy(c => c.Sortid).ToList();
        bool hasStatename = contents.Any(c =>
            !string.IsNullOrEmpty(c.Statename1) || !string.IsNullOrEmpty(c.Statename2));
        if (!hasStatename) Assert.Pass("无 statename 数据，跳过");

        _mgr.StartDialog(1);
        Assert.Greater(_emotionLog.Count, 0, "有 statename 应触发 OnEmotion");
    }

    [Test]
    public void PlayNextContent_FiresSlotState_WhenSlotstateidNonZero()
    {
        var contents = _tables.TbDialogcontent.DataList
            .Where(c => c.Dialogid == 1).OrderBy(c => c.Sortid).ToList();
        bool hasSlotState = contents.Any(c => c.Slotstateid1 != 0 || c.Slotstateid2 != 0);
        if (!hasSlotState) Assert.Pass("无 slotstateid 数据，跳过");

        _mgr.StartDialog(1);
        int safety = 30;
        while (_mgr.State == DialogState.Playing && _slotStateLog.Count == 0 && safety-- > 0)
            _mgr.Advance();

        Assert.Greater(_slotStateLog.Count, 0, "有 slotstateid 应触发 OnSlotState");
    }

    #endregion

    #region EndDialog 状态清理

    [Test]
    public void EndDialog_CanRestartNewDialog()
    {
        _mgr.StartDialog(4);
        int safety = 30;
        while (_mgr.State == DialogState.Playing && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(DialogState.Ended, _mgr.State);

        _contentLog.Clear();
        _mgr.StartDialog(1);
        Assert.AreEqual(DialogState.Playing, _mgr.State);
        Assert.AreEqual(1, _contentLog.Count, "结束后应能重新开始");
    }

    #endregion

    #region 完整流程

    [Test]
    public void FullFlow_Dialog1_Choose1_ReturnsToOptions()
    {
        _mgr.StartDialog(1);
        int safety = 30;
        while (_mgr.State == DialogState.Playing && _optionsLog.Count == 0 && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(1, _optionsLog.Count);

        _mgr.ChooseOption(0);
        Assert.AreEqual(DialogState.Playing, _mgr.State);

        _optionsLog.Clear();
        safety = 30;
        while (_mgr.State == DialogState.Playing && _optionsLog.Count == 0 && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(1, _optionsLog.Count, "分支3播完应回到选项");
    }

    [Test]
    public void FullFlow_Dialog1_Choose2_Ends()
    {
        _mgr.StartDialog(1);
        int safety = 30;
        while (_mgr.State == DialogState.Playing && _optionsLog.Count == 0 && safety-- > 0)
            _mgr.Advance();
        _mgr.ChooseOption(1);
        safety = 30;
        while (_mgr.State == DialogState.Playing && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(DialogState.Ended, _mgr.State);
    }

    [Test]
    public void FullFlow_Dialog200_LoopThenEnd()
    {
        _mgr.StartDialog(200);
        int safety = 30;
        while (_mgr.State == DialogState.Playing && _optionsLog.Count == 0 && safety-- > 0)
            _mgr.Advance();
        _mgr.ChooseOption(0);

        _optionsLog.Clear();
        safety = 30;
        while (_mgr.State == DialogState.Playing && _optionsLog.Count == 0 && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(1, _optionsLog.Count, "选'再来一遍'应回到选项");

        _mgr.ChooseOption(1);
        safety = 30;
        while (_mgr.State == DialogState.Playing && safety-- > 0)
            _mgr.Advance();
        Assert.AreEqual(DialogState.Ended, _mgr.State);
    }

    #endregion
}
