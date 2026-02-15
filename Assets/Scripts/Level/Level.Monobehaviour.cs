using UnityEngine;

public partial class Level : MonoBehaviour
{
    private void Awake()
    {
        if (currentMap != null) currentMap = transform.GetComponentInChildren<MapBehaviour>();
        if (playerCharacter != null) playerCharacter = transform.GetComponentInChildren<PlayerCharacter>();

        SubscribeToMapTeleportEvent(currentMap);
    }
}