using UnityEngine;

/// <summary>
/// 碰撞物体。
/// 用于墙壁、桌椅、箱子等静态障碍物。
/// 子类可扩展额外功能（如可破坏物体）。
/// </summary>
/// <remarks>
/// CollisionObject可能有多个Collider2D组件。
/// 所有的Collider应当放置在GameObject的"Colliders"子物体中。
/// 如果有Colliders子物体，则从子物体中获取所有Collider2D。
/// 如果没有Colliders子物体，则尝试从自身获取（向后兼容）。
/// </remarks>
public class CollisionObject : MapObject
{
    public override void Initialize()
    {
        base.Initialize();

        // 获取Colliders子物体中的所有Collider2D组件
        Transform collidersParent = transform.Find("Colliders");

        if (collidersParent != null)
        {
            Collider2D[] colliders = collidersParent.GetComponentsInChildren<Collider2D>();
            if (colliders.Length == 0)
            {
                Debug.LogWarning($"{name} (CollisionObject) has Colliders child but no Collider2D components");
            }
        }
        else
        {
            // 如果没有Colliders子物体，尝试在自身查找
            Collider2D collider = GetComponent<Collider2D>();
            if (collider == null)
            {
                Debug.LogWarning($"{name} (CollisionObject) is missing Colliders child and Collider2D component");
            }
        }
    }
}
