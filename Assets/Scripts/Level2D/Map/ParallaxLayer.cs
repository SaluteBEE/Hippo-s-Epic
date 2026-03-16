using UnityEngine;

public class ParallaxLayer
{
    private readonly Transform transform;
    private readonly Vector3 startLocalPosition;
    private readonly float scaleFactor;

    public ParallaxLayer(Transform transform, float scaleFactor)
    {
        this.transform = transform;
        this.startLocalPosition = transform.localPosition;
        this.scaleFactor = scaleFactor;
    }

    /// <summary>
    /// 根据相机相对基准点的偏移更新本层位置
    /// </summary>
    public void OnCameraOffsetChanged(Vector2 cameraDelta)
    {
        Vector3 offset = new Vector3(
            cameraDelta.x * scaleFactor,
            cameraDelta.y * scaleFactor,
            0f
        );

        transform.localPosition = startLocalPosition + offset;
    }
}