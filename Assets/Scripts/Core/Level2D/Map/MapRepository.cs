using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Core.Level2D.Maps
{
    [AddComponentMenu("Level 2D/Map Repository")]
    public class MapRepository : MonoBehaviour
    {
        private static MapRepository _instance;
        public static MapRepository Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        [SerializeField]
        private Map[] mapPrefabs;
        private Dictionary<string, Map> mapCache = new();

        private void Awake()
        {
            // initialize map cache
            if (Instance == null)
            {
                Instance = this;

                // register maps
                foreach (var item in mapPrefabs)
                {
                    if (item != null)
                    {
                        mapCache.Add(item.name, item);
                    }
                }
                return;
            }
            throw new System.ApplicationException("MapRepository: An instance already exists during Awake initialization, which suggests a duplicate startup may have occurred");
        }

        /// <summary>
        /// 根据地图名称实例化一个地图对象
        /// </summary>
        /// <param name="mapName">要实例化的地图名称</param>
        /// <param name="levelRoot">地图的父级变换</param>
        /// <returns>实例化后的Map组件</returns>
        /// <exception cref="System.NullReferenceException">当地图预制体不包含Map组件或找不到指定名称的地图时抛出</exception>
        /// <exception cref="System.InvalidOperationException">当MapRepository实例未初始化时抛出</exception>
        public static Map InstantiateMap(string mapName, Transform levelRoot = null)
        {
            if (Instance != null)
            {
                Dictionary<string, Map> mapCache = Instance.mapCache;
                // get map from cache
                if (mapCache.TryGetValue(mapName, out Map map))
                {
                    GameObject mapGameObject = Instantiate(map.gameObject);
                    mapGameObject.transform.parent = levelRoot;
                    if (mapGameObject.TryGetComponent(out Map component))
                    {
                        return component;
                    }
                    throw new System.NullReferenceException("MapRepository: The Map object to instantiate does not have a Map component");
                }

                Map cache;
                bool flag = false;
                Map[] maps = Instance.mapPrefabs;

                foreach (var item in maps)
                {
                    if (item.name == mapName)
                    {
                        cache = item;
                        Instance.mapCache.Add(mapName, item);
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    GameObject mapGameObject = Instantiate(map.gameObject);
                    mapGameObject.transform.parent = levelRoot;
                    if (mapGameObject.TryGetComponent(out Map component))
                    {
                        return component;
                    }
                    throw new System.NullReferenceException("MapRepository: The Map object to instantiate does not have a Map component");
                }

                throw new System.NullReferenceException($"MapRepository: Could not find a Map prefab with the name {mapName}");
            }
            throw new System.InvalidOperationException("MapRepository: MapRepository instance is not initialized");
        }
    }
}