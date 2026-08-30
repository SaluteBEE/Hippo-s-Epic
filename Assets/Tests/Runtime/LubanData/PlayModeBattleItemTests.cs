using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// 战斗物品端到端测试：鞭炮（伤害+眩晕BUFF）、绷带（范围治疗）、眩晕状态
/// </summary>
class PlayModeBattleItemTests
{
    private GameObject _root;
    private DataTableManager _dtm;
    private BattleManager _bm;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        _root = new GameObject("BattleItemTest");
        _dtm = _root.AddComponent<DataTableManager>();
        yield return null;
        _dtm.LoadTables();
        ManagerRegistry.Register(_dtm);  // 让 BattleManager.GetTables() 能找到表

        // 初始化玩家属性（CreatePlayerBattleUnit 依赖 PlayerStats）
        CharacterStatsManager.Instance.InitFromConfig();

        _bm = new BattleManager();
        _bm.InitBattle(2);  // 1v2 测试战斗（玩家1 + 敌人2,3）
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        BagManager.Instance.RemoveItem(12, 99);
        BagManager.Instance.RemoveItem(13, 99);
        ManagerRegistry.Unregister<DataTableManager>();
        if (_root != null) Object.DestroyImmediate(_root);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Firecracker_DamagesAndStunsEnemy()
    {
        if (_bm.PlayerUnits.Count == 0)
        {
            Assert.Ignore("玩家单位创建失败（存档/人物配置依赖）");
            yield break;
        }
        if (_bm.EnemyUnits.Count == 0)
        {
            Assert.Ignore("敌方单位创建失败");
            yield break;
        }

        BagManager.Instance.AddItem(12, 1);  // 鞭炮
        var player = _bm.PlayerUnits[0];
        var enemy = _bm.EnemyUnits[0];
        int beforeHp = enemy.Stats.Hp;

        player.CurrentActionPoints = 10;
        _bm.CurrentUnit = player;
        _bm.IsWaitingForPlayerAction = true;

        _bm.PlayerUseItem(12, enemy.SlotIndex);
        yield return null;

        // 鞭炮：param1=[2,3] → 5 物理伤害 + buff29(眩晕)
        Assert.Less(enemy.Stats.Hp, beforeHp, "鞭炮应对敌人造成伤害");
        Assert.IsTrue(enemy.UnitBuffs.HasStatus(BuffFuncType.Stun), "buff29(眩晕)应生效");
        Assert.IsTrue(enemy.UnitBuffs.HasBuff(29), "敌人应持有眩晕buff29");
    }

    [UnityTest]
    public IEnumerator Bandage_HealsAlly()
    {
        if (_bm.PlayerUnits.Count == 0)
        {
            Assert.Ignore("玩家单位创建失败（存档/人物配置依赖）");
            yield break;
        }

        BagManager.Instance.AddItem(13, 1);  // 绷带
        var player = _bm.PlayerUnits[0];

        // 先扣血
        player.Stats.Hp -= 30;
        player.Stats.ClampHp();
        int beforeHp = player.Stats.Hp;

        player.CurrentActionPoints = 10;
        _bm.CurrentUnit = player;
        _bm.IsWaitingForPlayerAction = true;

        // 绷带 func=1(对己方)，param2=[2,5,8]，以玩家自身为落点 → 治疗范围含自身
        _bm.PlayerUseItem(13, player.SlotIndex);
        yield return null;

        Assert.AreEqual(beforeHp + 10, player.Stats.Hp, "绷带应对己方治疗10点");
    }

    [UnityTest]
    public IEnumerator Firecracker_ItemRemovedFromBag()
    {
        if (_bm.PlayerUnits.Count == 0)
        {
            Assert.Ignore("玩家单位创建失败（存档/人物配置依赖）");
            yield break;
        }
        if (_bm.EnemyUnits.Count == 0)
        {
            Assert.Ignore("敌方单位创建失败");
            yield break;
        }

        BagManager.Instance.AddItem(12, 1);
        var player = _bm.PlayerUnits[0];
        var enemy = _bm.EnemyUnits[0];

        player.CurrentActionPoints = 10;
        _bm.CurrentUnit = player;
        _bm.IsWaitingForPlayerAction = true;

        _bm.PlayerUseItem(12, enemy.SlotIndex);
        yield return null;

        Assert.AreEqual(0, BagManager.Instance.GetItemCount(12), "使用后鞭炮应从背包移除");
    }

    [UnityTest]
    public IEnumerator HasUsableItems_EmptyBag_ReturnsFalse()
    {
        BagManager.Instance.Clear();
        Assert.IsFalse(_bm.HasUsableBattleItems(), "背包无战斗道具时 HasUsableBattleItems 应为 false");
        yield return null;
    }

    [UnityTest]
    public IEnumerator HasUsableItems_WithFirecracker_ReturnsTrue()
    {
        BagManager.Instance.Clear();
        BagManager.Instance.AddItem(12, 1);  // 鞭炮（func=2 战斗道具）
        Assert.IsTrue(_bm.HasUsableBattleItems(), "背包有战斗道具时 HasUsableBattleItems 应为 true");
        yield return null;
    }

    [UnityTest]
    public IEnumerator HasUsableItems_OnlyKey_ReturnsFalse()
    {
        BagManager.Instance.Clear();
        BagManager.Instance.AddItem(2, 1);  // 厕所钥匙（func 空，非战斗道具）
        Assert.IsFalse(_bm.HasUsableBattleItems(), "只有非战斗道具时 HasUsableBattleItems 应为 false");
        yield return null;
    }
}
