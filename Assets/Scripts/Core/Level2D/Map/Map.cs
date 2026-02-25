using UnityEngine;

namespace Core.Level2D.Maps
{
    public sealed class Map : MonoBehaviour
    {
        /// <summary>
        /// 默认入口
        /// </summary>
        public Vector2 MainEntrance;

        /// <summary>
        /// 摄像机 X 轴位移限制（x 为最小值，y 为最大值）
        /// </summary>
        public Vector2 CameraClampX;

        /// <summary>
        /// 摄像机 Y 轴位移限制（x 为最小值，y 为最大值）
        /// </summary>
        public Vector2 CameraClampY;

        /// <summary>
        /// 视觉层根节点，对应名为 Visual 的子 GameObject
        /// </summary>
        [SerializeField]
        public GameObject VisualRoot;

        private ParallaxLayer[] parallaxLayers;

        private void Reset()
        {
            if (VisualRoot == null)
            {
                Transform visualTransform = transform.Find("Visual");
                if (visualTransform != null)
                {
                    VisualRoot = visualTransform.gameObject;
                }
                else
                {
                    VisualRoot = new GameObject("Visual");
                    VisualRoot.transform.SetParent(transform, false);
                }
            }
        }

        public void Initialize()
        {
            // TODO: #11 parallaxLayers 的长度用 childCount - 1 计算，并假设恰好存在且仅存在一个名为 "Main" 的子层。若 VisualRoot 下没有 Main（或有多个 Main/命名不同），这里会出现数组长度为负、IndexOutOfRange 或数组中残留 null，随后 OnFocusMoved 遍历时会触发 NullReference。建议先统计非 Main 层数量（或用 List 动态收集）并在遍历时跳过 null。
            int childCount = VisualRoot.transform.childCount;
            parallaxLayers = new ParallaxLayer[childCount - 1];
            int layerIndex = 0;

            for (int i = 0; i < childCount; i++)
            {
                Transform child = VisualRoot.transform.GetChild(i);

                // 初始化该层下所有 MapObject
                MapObject[] mapObjects = child.GetComponentsInChildren<MapObject>();
                for (int j = mapObjects.Length - 1; j >= 0; j--)
                {
                    mapObjects[j].Initialize();
                }

                // Main 层不创建视差层
                if (child.name != "Main")
                {
                    parallaxLayers[layerIndex++] = new ParallaxLayer(child);
                }
            }
        }

        public void OnFocusMoved(Vector2 vector2)
        {
            foreach (var item in parallaxLayers)
            {
                item.OnCameraMoved(vector2);
            }
        }

        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
}