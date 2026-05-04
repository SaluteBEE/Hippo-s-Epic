using UnityEngine;

public class ItemPickup : SimpleInteractionObject
{
    [Header("Item")]
    [SerializeField] private string itemId;
    [SerializeField] private bool destroyOnPickup = true;

    private const int StatePickedUp = 2;

    public string ItemId => itemId;

    protected override void ExecuteInteraction()
    {
        Debug.Log($"获得了道具: {itemId}");

        if (!string.IsNullOrEmpty(EntityId))
            EntityConfigLoader.OnEntityInteracted(EntityId, StatePickedUp);

        if (destroyOnPickup)
        {
            if (interactHint != null)
                interactHint.Hide();

            Destroy(gameObject);
        }
    }
}
