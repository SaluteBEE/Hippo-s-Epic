using UnityEngine;

public class ItemPickup : SimpleInteractionObject
{
    [Header("Item")]
    [SerializeField] private string itemId;
    [SerializeField] private bool destroyOnPickup = true;

    public string ItemId => itemId;

    protected override void ExecuteInteraction()
    {
        Debug.Log($"获得了道具: {itemId}");

        // TODO: 接入背包系统
        // InventorySystem.Instance.AddItem(itemId);

        if (destroyOnPickup)
        {
            if (interactHint != null)
                interactHint.Hide();

            Destroy(gameObject);
        }
    }
}