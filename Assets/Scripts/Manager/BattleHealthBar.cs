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
}
