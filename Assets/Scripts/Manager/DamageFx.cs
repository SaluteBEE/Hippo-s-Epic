using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 飘字类型（颜色区分）
/// </summary>
public enum FloatTextType
{
    Damage, // 白色
    Crit,   // 黄色（暴击）
    Heal,   // 绿色
    Miss,   // 灰色
    Guard,  // 蓝色（格挡）
}

/// <summary>
/// 战斗表现：伤害/治疗飘字（P0）
/// World Space Canvas + TMP，目标头顶上升 + 淡出
/// </summary>
public static class DamageFx
{
    public static void SpawnFloatText(Transform anchor, string text, FloatTextType type)
    {
        if (anchor == null) return;

        var canvasGo = new GameObject("FloatText");
        canvasGo.transform.SetParent(anchor, false);
        canvasGo.transform.localPosition = new Vector3(0f, 2.3f, 0f);
        // 不设 Billboard: 战斗相机是正面正交 (0,0,-10) rot=(0,0,0), 角色 sprite 已面向相机,
        // canvas 继承角色旋转(identity localRotation) 法线自然朝相机, 文字正立可见。
        // 任何 Billboard 都会破坏朝向(俯视/正面相机下都 edge-on 不可见)

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 40;
        canvas.worldCamera = Camera.main; // WorldSpace Canvas 必须指定渲染相机, 否则文字不参与渲染

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320f, 110f); // 容纳 80pt/110pt 字号, 太小文字会被裁剪
        rt.localScale = Vector3.one * 0.01f;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = type == FloatTextType.Crit ? 110f : 80f; // 0.01 scale 下实际≈世界0.8/1.1单位, 才够醒目
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = GetColor(type);
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);
        tmp.enableWordWrapping = false;

        var item = canvasGo.AddComponent<FloatTextItem>();
        item.Play();
    }

    private static Color GetColor(FloatTextType type)
    {
        switch (type)
        {
            case FloatTextType.Crit:  return new Color(1f, 0.85f, 0.2f);
            case FloatTextType.Heal:  return new Color(0.3f, 1f, 0.4f);
            case FloatTextType.Miss:  return new Color(0.75f, 0.75f, 0.75f);
            case FloatTextType.Guard: return new Color(0.4f, 0.7f, 1f);
            default:                  return Color.white;
        }
    }
}

/// <summary>
/// 飘字动画宿主：上升 + 淡出 + 自毁
/// </summary>
public class FloatTextItem : MonoBehaviour
{
    private TextMeshProUGUI _tmp;
    private Vector3 _startPos;

    private const float RiseWorld = 1.0f; // 世界空间上升高度(正交相机 orthoSize≈5, 头顶 y≈3.8 起跳, 1.0 保证全程屏内)
    private const float Duration = 0.9f;

    private void Awake()
    {
        _startPos = transform.localPosition;
        _tmp = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Play()
    {
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        float t = 0f;
        var baseColor = _tmp != null ? _tmp.color : Color.white;
        // 世界上升 RiseWorld 单位。local 位移 = 世界位移 / 父级 lossyScale.y。
        // 注意: 不能用自身 localScale.y(那是 canvas 的 0.01 渲染缩放, 与位移无关),
        // 否则 riseLocal=160 会把飘字瞬间抬出屏幕(vp.y>1)
        var parent = transform.parent;
        float parentScale = parent != null ? Mathf.Max(Mathf.Abs(parent.lossyScale.y), 0.0001f) : 1f;
        float riseLocal = RiseWorld / parentScale;

        while (t < Duration)
        {
            t += Time.deltaTime;
            float k = t / Duration;
            transform.localPosition = _startPos + Vector3.up * (riseLocal * k);
            // 无 Billboard: 战斗相机正面正交, canvas 继承角色朝向即面向相机
            if (_tmp != null)
            {
                var c = baseColor;
                c.a = 1f - k * k;
                _tmp.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
