using System;
using UnityEngine;

namespace Core.Level2D.Maps
{
    public sealed class Map : MonoBehaviour
    {
        public Vector2 mainEntrance;

        public void Initialize()
        {
            MapObject[] mapObjects = GetComponentsInChildren<MapObject>();
            for (int i = mapObjects.Length - 1; i >= 0; i--)
            {
                mapObjects[i].Initialize();
            }
        }

        public void Dispose()
        {
            Destroy(gameObject);
        }
    }
}