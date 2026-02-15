using UnityEngine;

public partial class Level : MonoBehaviour
{
    private void Awake()
    {
        currentMap = transform.GetComponentInChildren<MapBehaviour>();
        SubscribeToMapTeleportEvent(currentMap);
        playerCharacter = transform.GetComponentInChildren<PlayerCharacter>();
    }
}