using UnityEngine;

/// <summary>
/// 测试用传送点，自动生成可视化和触发器。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class TestTeleportPoint : MapTeleportPoint
{
    [Header("Visual Settings")]
    [SerializeField] private Color gizmoColor = Color.magenta;
    [SerializeField] private Vector2 triggerSize = new Vector2(2f, 2f);

    public override void Initialize()
    {
        base.Initialize();

        // 自动设置触发器大小
        var collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = triggerSize;
            collider.isTrigger = true;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(transform.position, new Vector3(triggerSize.x, triggerSize.y, 0.1f));
        
        // 绘制箭头指示方向
        Gizmos.color = Color.white;
        Vector3 arrowStart = transform.position + Vector3.up * (triggerSize.y / 2 + 0.5f);
        Gizmos.DrawLine(arrowStart, arrowStart + Vector3.up * 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(transform.position, new Vector3(triggerSize.x, triggerSize.y, 0.1f));

        // 显示目标地图信息
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.down * 1.5f, 
            $"Target: {TargetMapId}\nPoint: {TargetTeleportIndex}");
#endif
    }
}
