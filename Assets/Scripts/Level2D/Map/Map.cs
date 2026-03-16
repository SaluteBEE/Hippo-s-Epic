using System.Collections.Generic;
using UnityEngine;


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
            int childCount = VisualRoot.transform.childCount;
            var layerList = new List<ParallaxLayer>();

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
                    layerList.Add(new ParallaxLayer(child));
                }
            }

            parallaxLayers = layerList.ToArray();
        }

        public void OnFocusMoved(Vector2 vector2)
        {
            foreach (var item in parallaxLayers)
            {
                item?.OnCameraMoved(vector2);
            }
        }

        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
