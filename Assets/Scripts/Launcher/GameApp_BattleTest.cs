using UnityEngine;

public partial class GameApp
{
    #region Battle Test Inspector

    [Header("战斗调试")]
    [SerializeField] private bool debugEnableBattleTest;
    [Tooltip("战斗配置表ID")]
    [SerializeField] private int debugBattleId = 1;

    #endregion

    #region Battle Test State

    private string _battleTestStatus = "";
    private string _battleTestInput = "1";
    private Vector2 _battleLogScroll;
    private bool _showBattleLog = true;

    public bool IsBattleTestEnabled => debugEnableBattleTest;

    #endregion

    public void InitBattleTest()
    {
        if (!debugEnableBattleTest) return;
        _battleTestInput = debugBattleId.ToString();
        _battleTestStatus = "战斗测试就绪";
        Debug.Log("[GameApp] 战斗测试已启用");
    }

    private void DrawBattleTestContent(GUIStyle lbl, GUIStyle btn, GUIStyle fld, float s)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("战斗ID:", lbl, GUILayout.Width(80 * s));
        _battleTestInput = GUILayout.TextField(_battleTestInput, fld, GUILayout.Width(100 * s));
        if (GUILayout.Button("进入战斗", btn, GUILayout.Height(30 * s)))
            BattleTestEnter();
        GUILayout.EndHorizontal();

        GUILayout.Space(4 * s);
        GUILayout.Label(_battleTestStatus, lbl);

        var bm = BattleManager;
        if (bm != null)
        {
            GUILayout.Label($"阶段: {bm.Phase}  当前: {(bm.CurrentUnit != null ? $"P{bm.CurrentUnit.PersonId}" : "无")}", lbl);
            GUILayout.Label($"友方: {bm.PlayerUnits.Count}人  敌方: {bm.EnemyUnits.Count}人  第{bm.RoundCount}轮", lbl);
            if (bm.CurrentUnit != null)
                GUILayout.Label($"HP: {bm.CurrentUnit.Stats.Hp}/{bm.CurrentUnit.Stats.FinalHpMax}  等待: {(bm.IsWaitingForPlayerAction ? "是" : "否")}", lbl);

            if (bm.TurnQueue != null && bm.TurnQueue.Count > 0)
            {
                string q = "";
                int idx = 0;
                foreach (var u in bm.TurnQueue)
                {
                    if (idx++ > 8) { q += "..."; break; }
                    q += $"P{u.PersonId}{(u.IsPlayerSide ? "A" : "E")} ";
                }
                GUILayout.Label($"队列: {q}", lbl);
            }
        }

        if (BattleLogger.Entries.Count > 0)
        {
            GUILayout.Space(4 * s);
            if (GUILayout.Button(_showBattleLog ? "▼ 日志" : "▶ 日志", btn, GUILayout.Height(28 * s)))
                _showBattleLog = !_showBattleLog;
            if (_showBattleLog)
            {
                _battleLogScroll = GUILayout.BeginScrollView(_battleLogScroll, GUILayout.ExpandHeight(true));
                foreach (var e in BattleLogger.Entries)
                    GUILayout.Label(e, lbl);
                GUILayout.EndScrollView();
            }
        }
    }

    private void BattleTestEnter()
    {
        if (!int.TryParse(_battleTestInput, out int battleId))
        {
            _battleTestStatus = "无效ID";
            return;
        }
        var bm = ManagerRegistry.GetTables<cfg.Tables>()?.TbBattle.GetOrDefault(battleId);
        if (bm == null)
        {
            _battleTestStatus = $"战斗{battleId}不存在";
            return;
        }
        _battleTestStatus = $"进入: {bm.Name}";
        EnterBattle(battleId);
    }
}
