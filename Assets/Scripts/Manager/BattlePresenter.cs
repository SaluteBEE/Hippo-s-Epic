using System;
using System.Collections;
using System.Collections.Generic;
using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 战斗表现层总控（P0 技能释放流程）
/// BattleManager 技能结算完成后调用 PlaySkillSequence 播放演出序列，回调后再推进回合。
/// 演出时序（对齐策划案 3.5.1 喜剧三拍子：铺垫→夸张动作→延迟反应）：
///   停顿0.1s → 转身 → 走向 → 蓄力预备 → 出招冲刺 → 命中停顿
///   → 受击反馈（闪白/后仰）→ 喜剧延迟0.15s → 飘字+血条延迟动画 → 归位
/// 本期不含战斗喊话（策划案标注【暂缓】）。
/// </summary>
public class BattlePresenter : MonoBehaviour
{
    public static BattlePresenter Instance { get; private set; }

    /// <summary> 演出播放中（输入抑制用） </summary>
    public bool IsPlaying { get; private set; }

    /// <summary> 序列开始/结束（UI 订阅做输入抑制） </summary>
    public event Action OnSequenceBegin;
    public event Action OnSequenceEnd;

    // 演出时长（对齐策划案 3.5.1 时间轴，P0 先常量，后续可配表驱动）
    private const float StartPause = 0.10f;       // 锁定反馈后停顿
    private const float FaceDuration = 0.05f;     // 转身时长
    private const float WalkDuration = 0.25f;     // 走向目标时长
    private const float ApproachRatio = 0.75f;    // 走近到目标距离的 3/4
    private const float WindUpDuration = 0.20f;   // 蓄力预备时长（喜剧铺垫）
    private const float WindUpBack = 0.25f;       // 蓄力后拉距离
    private const float LungeDistance = 0.30f;    // 出招冲刺距离
    private const float LungeDuration = 0.16f;    // 出招冲刺时长
    private const float StrikePause = 0.12f;      // 命中停顿（打击感"敲定"）
    private const float HitInterval = 0.08f;      // 多目标受击反馈间隔
    private const float FlashDuration = 0.12f;    // 受击闪白
    private const float KnockbackDistance = 0.30f;// 受击后仰距离
    private const float KnockbackDuration = 0.14f;// 受击后仰时长
    private const float ComedyDelay = 0.15f;      // 喜剧延迟：受击后停顿再飘字（WoL 滑稽感来源）
    private const float FloatTextStagger = 0.05f; // 多目标飘字间隔
    private const float ReturnDuration = 0.25f;   // 归位时长

    private BattleManager _battleManager;
    private BattleStageManager _stage;

    /// <summary> 演出加速倍率（点击加速：1 → 2.5 → 8） </summary>
    private float _speed = 1f;

    /// <summary> 行动者原始缩放（归位时恢复朝向） </summary>
    private Vector3 _actorOrigScale;

    private void Awake()
    {
        Instance = this;
        _stage = BattleStageManager.Instance;
        _battleManager = ManagerRegistry.Get<BattleManager>();
        if (_battleManager != null)
            _battleManager.OnUnitDeath += OnUnitDeath;
    }

    private void OnDestroy()
    {
        if (_battleManager != null)
            _battleManager.OnUnitDeath -= OnUnitDeath;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // 演出中点击加速：1 → 2.5 → 8（近似跳过）
        if (IsPlaying && Input.GetMouseButtonDown(0))
        {
            _speed = _speed >= 2f ? 8f : 2.5f;
        }
    }

    /// <summary>
    /// 播放技能释放序列（BattleManager 在技能结算后、回合推进前调用）
    /// </summary>
    public void PlaySkillSequence(BattleUnit actor, int skillId, List<BattleUnit> targets,
        Dictionary<BattleUnit, int> beforeHp, Action onComplete)
    {
        if (actor == null)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(SkillSequenceRoutine(actor, skillId, targets, beforeHp, onComplete));
    }

    private IEnumerator SkillSequenceRoutine(BattleUnit actor, int skillId, List<BattleUnit> targets,
        Dictionary<BattleUnit, int> beforeHp, Action onComplete)
    {
        IsPlaying = true;
        _speed = 1f;
        OnSequenceBegin?.Invoke();
        try
        {
            // 技能信息（判断治疗/伤害飘字）
            bool isHeal = false;
            if (skillId > 0 && _battleManager != null)
            {
                var cfg = _battleManager.GetTables()?.TbSkill.GetOrDefault(skillId);
                if (cfg != null && (SkillType)cfg.Skilltype == SkillType.Heal)
                    isHeal = true;
            }

            var actorGo = GetUnitGo(actor);
            var validTargets = new List<BattleUnit>();
            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t != null) validTargets.Add(t);
                }
            }
            // 多目标按格子顺序（左上→右下，槽位升序）依次反馈
            validTargets.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

            // ── [0.00s] 锁定反馈后停顿 0.1s ──
            yield return W(StartPause);

            // ── [0.10s] 转身：朝向目标（翻转 Sprite X）──
            if (actorGo != null && validTargets.Count > 0)
            {
                var firstGo = GetUnitGo(validTargets[0]);
                if (firstGo != null)
                {
                    _actorOrigScale = actorGo.transform.localScale;
                    FaceTarget(actorGo, firstGo.transform.position);
                }
            }

            // ── [0.15s] 走向目标（首个目标的斜前方 3/4）──
            if (actorGo != null && validTargets.Count > 0)
            {
                var firstGo = GetUnitGo(validTargets[0]);
                if (firstGo != null)
                {
                    Vector3 from = actorGo.transform.position;
                    Vector3 to = firstGo.transform.position;
                    yield return StartCoroutine(MoveRoutine(actorGo, Vector3.Lerp(from, to, ApproachRatio), WalkDuration));
                }
            }

            // ── [0.40s] 蓄力预备：夸张后拉（喜剧铺垫，越夸张越好笑）──
            if (actorGo != null)
            {
                yield return StartCoroutine(WindUpRoutine(actorGo, WindUpBack, WindUpDuration));
            }

            // ── [0.60s] 出招冲刺 + 命中停顿 ──
            if (actorGo != null)
            {
                Vector3 start = actorGo.transform.position;
                Vector3 dir = Vector3.zero;
                if (validTargets.Count > 0)
                {
                    var firstGo = GetUnitGo(validTargets[0]);
                    if (firstGo != null)
                        dir = firstGo.transform.position - start;
                }

                if (dir.sqrMagnitude < 0.0001f)
                    dir = Vector3.right * (actor.IsPlayerSide ? 1f : -1f);

                yield return StartCoroutine(MoveRoutine(actorGo, start + dir.normalized * LungeDistance, LungeDuration));
                yield return W(StrikePause);
            }

            // ── [0.88s] 受击反馈：每目标闪白 + 后仰（间隔 0.08s）──
            foreach (var target in validTargets)
            {
                var targetGo = GetUnitGo(target);
                if (targetGo == null) continue;

                StartCoroutine(FlashWhiteRoutine(targetGo, FlashDuration));
                StartCoroutine(KnockbackRoutine(targetGo, actorGo, KnockbackDistance, KnockbackDuration));
                yield return W(HitInterval);
            }

            // ── [1.02s] 喜剧延迟：受击演完停顿一拍，再飘字（WoL 滑稽感关键）──
            yield return W(ComedyDelay);

            // ── [1.17s] 伤害飘字 + 血条延迟动画（白条残影）──
            foreach (var target in validTargets)
            {
                var targetGo = GetUnitGo(target);
                if (targetGo == null) continue;

                int before = beforeHp != null && beforeHp.TryGetValue(target, out int b) ? b : target.Stats.Hp;
                int delta = target.Stats.Hp - before;

                if (delta < 0)
                {
                    DamageFx.SpawnFloatText(targetGo.transform, delta.ToString(), FloatTextType.Damage);
                }
                else if (delta > 0)
                {
                    DamageFx.SpawnFloatText(targetGo.transform, "+" + delta, FloatTextType.Heal);
                }
                else if (isHeal)
                {
                    DamageFx.SpawnFloatText(targetGo.transform, "恢复", FloatTextType.Heal);
                }

                var bar = _stage != null ? _stage.GetHealthBar(target.IsPlayerSide, target.SlotIndex) : null;
                if (bar != null)
                    bar.PlayGhostAnimation(before, target.Stats.Hp);

                yield return W(FloatTextStagger);
            }

            // ── [2.40s] 归位：走回站位 + 恢复朝向 ──
            if (actorGo != null)
            {
                var slot = _stage != null ? _stage.GetSlot(actor.IsPlayerSide, actor.SlotIndex) : null;
                if (slot != null)
                    yield return StartCoroutine(MoveRoutine(actorGo, slot.position, ReturnDuration));

                actorGo.transform.localScale = _actorOrigScale;
            }
        }
        finally
        {
            IsPlaying = false;
            OnSequenceEnd?.Invoke();
        }

        onComplete?.Invoke();
    }

    #region 死亡演出

    private void OnUnitDeath(BattleUnit unit)
    {
        StartCoroutine(DeathRoutine(unit));
    }

    private IEnumerator DeathRoutine(BattleUnit unit)
    {
        var go = GetUnitGo(unit);
        if (go == null) yield break;

        // 等飘字先亮出来（受击反馈 + 喜剧延迟）
        yield return new WaitForSeconds(0.2f);

        float dur = 0.45f;
        float t = 0f;
        var startRot = go.transform.rotation;
        var startPos = go.transform.position;
        float dir = unit.IsPlayerSide ? -1f : 1f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            go.transform.rotation = startRot * Quaternion.Euler(0f, 0f, dir * 85f * k);
            go.transform.position = startPos + Vector3.down * (0.25f * k);
            yield return null;
        }
        go.SetActive(false);
    }

    #endregion

    #region 工具

    /// <summary> 加速感知的等待（点击加速时所有停顿同步缩短） </summary>
    private WaitForSeconds W(float seconds)
    {
        return new WaitForSeconds(seconds / _speed);
    }

    private GameObject GetUnitGo(BattleUnit unit)
    {
        if (unit == null || _stage == null) return null;
        return _stage.GetUnitObject(unit.IsPlayerSide, unit.SlotIndex);
    }

    /// <summary> 朝向目标：翻转 Sprite X（敌人默认朝左，玩家默认朝右） </summary>
    private static void FaceTarget(GameObject go, Vector3 targetPos)
    {
        if (go == null) return;
        Vector3 scale = go.transform.localScale;
        float faceRight = targetPos.x >= go.transform.position.x ? 1f : -1f;
        scale.x = Mathf.Abs(scale.x) * faceRight;
        go.transform.localScale = scale;
    }

    /// <summary> 蓄力预备：向远离目标方向后拉 + 压扁拉伸（喜剧铺垫） </summary>
    private static IEnumerator WindUpRoutine(GameObject go, float backDistance, float duration)
    {
        if (go == null) yield break;
        Vector3 origin = go.transform.position;
        Vector3 back = origin + Vector3.right * (-Mathf.Sign(go.transform.localScale.x)) * backDistance;
        Vector3 origScale = go.transform.localScale;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            go.transform.position = Vector3.Lerp(origin, back, Mathf.SmoothStep(0f, 1f, k));
            // 压扁（x 拉宽 y 压扁），为冲刺蓄力
            float squash = 1f + 0.18f * k;
            go.transform.localScale = new Vector3(origScale.x * squash, origScale.y / squash, origScale.z);
            yield return null;
        }
    }

    /// <summary> 平滑位移（SmoothStep 缓动） </summary>
    private static IEnumerator MoveRoutine(GameObject go, Vector3 target, float duration)
    {
        if (go == null) yield break;
        var start = go.transform.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            go.transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        if (go != null) go.transform.position = target;
    }

    /// <summary> 受击闪白（SpriteRenderer + Spine 骨骼） </summary>
    private static IEnumerator FlashWhiteRoutine(GameObject go, float duration)
    {
        if (go == null) yield break;

        var spine = go.GetComponentInChildren<SkeletonAnimation>(true);
        Color spineColor = default;
        bool hasSpine = spine != null;
        if (hasSpine)
            spineColor = spine.Skeleton.GetColor();

        var renders = go.GetComponentsInChildren<SpriteRenderer>(true);
        var colors = new Color[renders.Length];
        for (int i = 0; i < renders.Length; i++)
        {
            colors[i] = renders[i].color;
            renders[i].color = Color.white;
        }

        yield return new WaitForSeconds(duration);

        if (go == null) yield break;
        for (int i = 0; i < renders.Length; i++)
        {
            if (renders[i] != null) renders[i].color = colors[i];
        }
        if (hasSpine && spine != null)
            spine.Skeleton.SetColor(spineColor);
    }

    /// <summary> 受击后仰：向远离攻击者的方向推出再弹回（Sin 曲线，一段完成去+回） </summary>
    private static IEnumerator KnockbackRoutine(GameObject go, GameObject attackerGo, float distance, float duration)
    {
        if (go == null) yield break;

        Vector3 origin = go.transform.position;
        Vector3 dir = Vector3.right;
        if (attackerGo != null)
        {
            Vector3 d = origin - attackerGo.transform.position;
            if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
        }

        Vector3 pushed = origin + dir * distance;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            go.transform.position = Vector3.Lerp(origin, pushed, Mathf.Sin(k * Mathf.PI));
            yield return null;
        }
        if (go != null) go.transform.position = origin;
    }

    #endregion
}
