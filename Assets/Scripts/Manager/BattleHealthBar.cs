using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗头顶血条组件（World Space）
/// 跟随角色头顶，显示HP比例
/// </summary>
public class BattleHealthBar : MonoBehaviour
{
    private BattleUnit _unit;
    private Transform _followTarget;
    private Vector3 _offset;
    private Image _fillImage;
    private Slider _slider;
    private Image _ghostImage;
    private IEnumerator _ghostRoutine;

    /// <summary>
    /// 完整初始化（使用预制体时）
    /// </summary>
    public void Setup(BattleUnit unit, Transform followTarget, Vector3 offset)
    {
        _unit = unit;
        _followTarget = followTarget;
        _offset = offset;

        _slider = GetComponentInChildren<Slider>();
        if (_slider != null)
        {
            _slider.minValue = 0;
            _slider.maxValue = 1;
        }

        // 尝试找到填充图
        if (_fillImage == null)
        {
            var fillGo = transform.Find("Background/Fill");
            if (fillGo != null) _fillImage = fillGo.GetComponent<Image>();
        }

        UpdateHealth();
    }

    /// <summary>
    /// 简单初始化（动态创建时）
    /// </summary>
    public void SetupSimple(BattleUnit unit, Image fillImage, Transform followTarget, Vector3 offset)
    {
        _unit = unit;
        _fillImage = fillImage;
        _followTarget = followTarget;
        _offset = offset;
        UpdateHealth();
    }

    private void LateUpdate()
    {
        // 跟随目标头顶
        if (_followTarget != null && transform.parent == _followTarget)
        {
            // 已经是子物体，保持本地位置即可
            return;
        }

        if (_followTarget != null)
        {
            transform.position = _followTarget.position + _offset;
        }
    }

    /// <summary>
    /// 更新血量显示
    /// </summary>
    public void UpdateHealth()
    {
        if (_unit == null) return;

        float ratio = _unit.Stats.FinalHpMax > 0 ? (float)_unit.Stats.Hp / _unit.Stats.FinalHpMax : 0f;

        if (_slider != null)
            _slider.value = ratio;

        if (_fillImage != null)
        {
            if (_fillImage.type == Image.Type.Filled)
                _fillImage.fillAmount = ratio;
            else
                _fillImage.rectTransform.localScale = new Vector3(ratio, 1, 1);

            // 血量低时变红
            if (ratio <= 0.25f)
                _fillImage.color = Color.red;
            else if (ratio <= 0.5f)
                _fillImage.color = new Color(1f, 0.6f, 0f); // 橙色
        }

        // 阵亡时隐藏
        if (_unit.Stats.Hp <= 0)
            gameObject.SetActive(false);
    }

    #region 延迟血条动画（白条残影）

    /// <summary>
    /// 延迟扣血/回血动画：白色残影条从 beforeHp 缓动到当前 HP（经典 RPG 白条延迟）
    /// </summary>
    public void PlayGhostAnimation(int beforeHp, int afterHp)
    {
        // 单位已死亡/血条被隐藏时跳过（inactive 对象无法启动协程）
        if (_unit == null || !gameObject.activeInHierarchy) return;
        float max = _unit.Stats.FinalHpMax;
        if (max <= 0f) return;

        float from = Mathf.Clamp01(beforeHp / max);
        float to = Mathf.Clamp01(afterHp / max);

        if (_ghostRoutine != null)
            StopCoroutine(_ghostRoutine);

        _ghostRoutine = GhostRoutine(from, to);
        StartCoroutine(_ghostRoutine);
    }

    private IEnumerator GhostRoutine(float from, float to)
    {
        var ghost = EnsureGhostImage();
        if (ghost == null)
        {
            _ghostRoutine = null;
            yield break;
        }

        ghost.gameObject.SetActive(true);
        SetGhostValue(ghost, from);

        float dur = 0.7f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetGhostValue(ghost, Mathf.Lerp(from, to, t / dur));
            yield return null;
        }

        SetGhostValue(ghost, to);
        ghost.gameObject.SetActive(false);
        _ghostRoutine = null;
    }

    private Image EnsureGhostImage()
    {
        if (_ghostImage != null) return _ghostImage;
        if (_fillImage == null) return null;

        var parent = _fillImage.rectTransform.parent;
        if (parent == null) return null;

        var ghostGo = new GameObject("GhostFill");
        ghostGo.transform.SetParent(parent, false);
        var rt = ghostGo.AddComponent<RectTransform>();
        rt.anchorMin = _fillImage.rectTransform.anchorMin;
        rt.anchorMax = _fillImage.rectTransform.anchorMax;
        rt.offsetMin = _fillImage.rectTransform.offsetMin;
        rt.offsetMax = _fillImage.rectTransform.offsetMax;

        var ghost = ghostGo.AddComponent<Image>();
        ghost.color = new Color(1f, 1f, 1f, 0.85f);
        ghost.raycastTarget = false;

        if (_fillImage.type == Image.Type.Filled)
        {
            ghost.type = Image.Type.Filled;
            ghost.fillMethod = _fillImage.fillMethod;
            ghost.fillOrigin = _fillImage.fillOrigin;
            ghost.fillAmount = 1f;
        }
        else
        {
            ghost.type = Image.Type.Simple;
        }

        // 渲染在真实血条之上
        ghostGo.transform.SetSiblingIndex(_fillImage.transform.GetSiblingIndex() + 1);

        _ghostImage = ghost;
        return ghost;
    }

    /// <summary>
    /// 设置残影条比例：Filled类型用fillAmount，Simple类型用localScale.x
    /// </summary>
    private static void SetGhostValue(Image ghost, float ratio)
    {
        if (ghost == null) return;
        if (ghost.type == Image.Type.Filled)
            ghost.fillAmount = ratio;
        else
            ghost.rectTransform.localScale = new Vector3(ratio, 1f, 1f);
    }

    #endregion
}
