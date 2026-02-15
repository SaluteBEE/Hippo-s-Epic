using System;
using UnityEngine;

public partial class Level : MonoBehaviour
{
    [SerializeField]
    private MapBehaviour currentMap;
    [SerializeField]
    private PlayerCharacter playerCharacter;
    private void OnMapTeleported(MapTeleportContext context)
    {
        // Handle the map teleportation logic here, such as loading the target map and positioning the player
        /*
        - Disable player control during teleportation
        - Unsubscribe from the current map's MapTeleported event
        - Disable or unload the current map
        - Load the target map based on context.TargetMapIndex
        - Subscribe to the new map's MapTeleported event
        - Enable the new map and perform any necessary initialization
        - Position the player at the target teleport position based on context.TargetMapTeleportPositionIndex
        */
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
