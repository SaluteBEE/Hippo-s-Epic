using UnityEngine;

public abstract class InteractionCollider : MonoBehaviour
{
    [Header("Entity")]
    [SerializeField] private string entityId;

    public string EntityId => entityId;

    public abstract void OnPlayerEnter();
    public abstract void OnPlayerExit();
    public abstract void OnPlayerStay();
    public abstract void OnPlayerExecute();
}
