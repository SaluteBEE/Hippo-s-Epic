using  UnityEngine;
public class ItemPickup : InteractionCollider
{
    public string itemId;

    public override void OnPlayerExecute()
    {
        Debug.Log($"获得了道具: {itemId}");
        // TODO: 销毁物体或放入背包
        Destroy(gameObject);
    }

    public override void OnPlayerEnter()
    {
        Debug.Log($"碰到了可拾取物体: {itemId}");

    }
    public override void OnPlayerExit()
    {
    }

    public override void OnPlayerStay()
    {
        
    }
}