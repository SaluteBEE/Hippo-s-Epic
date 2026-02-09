using System;
using UnityEngine;
using Map;

public class Level : MonoBehaviour
{
    private MapBehaviour currentMap;
    private void OnMapTeleported(MapTeleportContext context)
    {
        // Handle the map teleportation logic here, such as loading the target map and positioning the player
    }

    private void SubscribeToMapTeleportEvent(MapBehaviour map)
    {
        map.MapTeleported += OnMapTeleported;
    }

    private void UnsubscribeFromMapTeleportEvent(MapBehaviour map)
    {
        map.MapTeleported -= OnMapTeleported;
    }
}
