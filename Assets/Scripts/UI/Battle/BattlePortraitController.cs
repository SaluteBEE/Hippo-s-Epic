using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 战斗窗口肖像控制：
/// 1. 鼠标指针移动时，眼睛骨骼（yanjing IK）跟随鼠标位置（盯指针）
/// 2. 鼠标闲置 idleDelay 秒后，随机播放 idle / idle2 动画
/// 3. 玩家每累计掉血 hitThreshold 点，播放一次 hit 动画（掉牙）
/// </summary>
public class BattlePortraitController : MonoBehaviour
{
    [Header("Spine")]
    [SerializeField] private SkeletonGraphic skeletonGraphic;
    [Tooltip("控制眼睛看向的IK骨骼名")]
    [SerializeField] private string eyeBoneName = "yanjing";

    [Header("动画")]
    [Tooltip("鼠标移动时播放的基础动画（盯指针）")]
    [SerializeField] private string trackingAnim = "idle";
    [SerializeField] private string idleAnim = "idle";
    [SerializeField] private string idle2Anim = "idle2";
    [SerializeField] private string hitAnim = "hit";

    [Header("行为")]
    [Tooltip("鼠标闲置多久后切换 idle/idle2")]
    [SerializeField] private float idleDelay = 2f;
    [Tooltip("闲置期间 idle/idle2 的切换间隔（每次随机取一个）")]
    [SerializeField] private float idleSwitchInterval = 2f;
    [Tooltip("视为鼠标移动的最小屏幕位移（像素）")]
    [SerializeField] private float mouseMoveThreshold = 1f;
    [Tooltip("玩家累计掉血多少点触发一次hit（掉牙）")]
    [SerializeField] private float hitThreshold = 10f;
    [Tooltip("眼睛跟随鼠标时的最大偏移半径（骨架坐标，限制在眼眶内）")]
    [SerializeField] private float eyeMoveRadius = 15f;

    private Bone _eyeBone;
    private float _eyeBoneInitX;
    private float _eyeBoneInitY;
    private float _eyeBoneInitWorldX;
    private float _eyeBoneInitWorldY;
    private Vector3 _lastMousePos;
    private float _idleTimer;
    private bool _isTracking;
    private bool _isHitPlaying;
    private float _hitAccumulator;
    private bool _subscribed;
    private Camera _uiCamera;

    private void OnEnable()
    {
        if (skeletonGraphic == null) skeletonGraphic = GetComponent<SkeletonGraphic>();
        if (skeletonGraphic == null) return;

        var canvas = skeletonGraphic.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            _uiCamera = canvas.worldCamera;

        skeletonGraphic.UpdateComplete += OnSkeletonUpdateComplete;
        _lastMousePos = Input.mousePosition;

        // 懒加载眼睛骨骼（SkeletonGraphic 初始化后才有 Skeleton）
        TryGetEyeBone();

        var bm = GameApp.Instance != null ? GameApp.Instance.BattleManager : null;
        if (bm != null)
        {
            bm.OnDamageTaken += OnDamageTaken;
            _subscribed = true;
        }

        StartIdle();
    }

    private void OnDisable()
    {
        if (skeletonGraphic != null)
            skeletonGraphic.UpdateComplete -= OnSkeletonUpdateComplete;

        if (_subscribed && GameApp.Instance != null && GameApp.Instance.BattleManager != null)
            GameApp.Instance.BattleManager.OnDamageTaken -= OnDamageTaken;
        _subscribed = false;
    }

    private void Update()
    {
        if (skeletonGraphic == null) return;

        TryGetEyeBone();

        var mp = Input.mousePosition;
        bool moved = (mp - _lastMousePos).sqrMagnitude > mouseMoveThreshold * mouseMoveThreshold;
        _lastMousePos = mp;

        if (moved)
        {
            _idleTimer = 0f;
            if (!_isTracking && !_isHitPlaying) StartTracking();
        }
        else
        {
            _idleTimer += Time.deltaTime;
            if (_isHitPlaying) return;
            if (_isTracking)
            {
                // 盯指针超时 → 进入闲置
                if (_idleTimer >= idleDelay) StartIdle();
            }
            else
            {
                // 闲置中：周期性随机切换 idle/idle2
                if (_idleTimer >= idleSwitchInterval) StartIdle();
            }
        }
    }

    /// <summary>
    /// 骨架完全更新后设置眼睛骨骼位置（此时世界变换是最新的，避免基准过期导致的抖动）
    /// </summary>
    private void OnSkeletonUpdateComplete(ISkeletonAnimation animated)
    {
        if (!_isTracking || _isHitPlaying || _eyeBone == null || skeletonGraphic == null) return;

        // 屏幕坐标 → 肖像局部坐标（约等于骨架世界坐标）
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                skeletonGraphic.rectTransform, Input.mousePosition, _uiCamera, out Vector2 local))
            return;

        // 限制眼睛在初始位置附近移动（不超出眼眶）
        Vector2 init = new Vector2(_eyeBoneInitWorldX, _eyeBoneInitWorldY);
        Vector2 offset = (Vector2)local - init;
        if (offset.sqrMagnitude > eyeMoveRadius * eyeMoveRadius)
            offset = offset.normalized * eyeMoveRadius;
        Vector2 target = init + offset;

        // 用父骨骼的世界变换把目标转到局部（父骨骼由动画驱动、不受眼睛修改影响，可避免振荡）
        float lx, ly;
        var parent = _eyeBone.Parent;
        if (parent != null)
            parent.WorldToLocal(target.x, target.y, out lx, out ly);
        else
        {
            lx = target.x;
            ly = target.y;
        }
        _eyeBone.X = lx;
        _eyeBone.Y = ly;

        // 用最新局部重新计算世界坐标，保证眼睛世界位置精确落在目标上
        skeletonGraphic.Skeleton.UpdateWorldTransform();
    }

    /// <summary> 鼠标移动：进入盯指针状态（idle 基础动画 + 眼睛跟随鼠标） </summary>
    private void StartTracking()
    {
        _isTracking = true;
        if (skeletonGraphic.AnimationState != null)
            skeletonGraphic.AnimationState.SetAnimation(0, trackingAnim, true);
    }

    /// <summary> 鼠标闲置：随机切换 idle/idle2，眼睛归位 </summary>
    private void StartIdle()
    {
        _isTracking = false;
        _idleTimer = 0f;
        ResetEye();
        if (skeletonGraphic.AnimationState != null)
        {
            string anim = Random.value < 0.5f ? idleAnim : idle2Anim;
            skeletonGraphic.AnimationState.SetAnimation(0, anim, true);
        }
    }

    /// <summary> 眼睛骨骼恢复初始位置 </summary>
    private void ResetEye()
    {
        if (_eyeBone == null) return;
        _eyeBone.X = _eyeBoneInitX;
        _eyeBone.Y = _eyeBoneInitY;
    }

    private void TryGetEyeBone()
    {
        if (_eyeBone != null || skeletonGraphic == null || skeletonGraphic.Skeleton == null) return;
        _eyeBone = skeletonGraphic.Skeleton.FindBone(eyeBoneName);
        if (_eyeBone != null)
        {
            _eyeBoneInitX = _eyeBone.X;
            _eyeBoneInitY = _eyeBone.Y;
            _eyeBoneInitWorldX = _eyeBone.WorldX;
            _eyeBoneInitWorldY = _eyeBone.WorldY;
        }
    }

    /// <summary> 玩家每累计掉血 hitThreshold 点触发一次 hit（掉牙动画） </summary>
    private void OnDamageTaken(BattleUnit actor, BattleUnit target, int amount)
    {
        if (target == null || !target.IsPlayerSide) return;

        _hitAccumulator += amount;
        while (_hitAccumulator >= hitThreshold)
        {
            _hitAccumulator -= hitThreshold;
            PlayHit();
        }
    }

    /// <summary> 播放 hit 动画，播完回到基础状态 </summary>
    private void PlayHit()
    {
        if (skeletonGraphic == null || skeletonGraphic.AnimationState == null) return;

        _isHitPlaying = true;
        _idleTimer = 0f;
        _isTracking = false;
        ResetEye();

        var entry = skeletonGraphic.AnimationState.SetAnimation(0, hitAnim, false);
        if (entry != null)
            entry.Complete += _ =>
            {
                _isHitPlaying = false;
                if (_isTracking) StartTracking();
                else StartIdle();
            };
    }
}
