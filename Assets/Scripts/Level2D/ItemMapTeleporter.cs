using UnityEngine;
public class ItemMapTeleporter : InteractionCollider
{
    public string targetMapName;
    public Vector2 targetEntrance;

    public override void OnPlayerExecute()
    {
        Debug.Log($"正在传送至地图: {targetMapName}");
        // 这里调用你之前的 MapRepository 加载新地图
        //MapManager.Instance.SwitchMap(targetMapName); 
    }

    public override void OnPlayerEnter()
    {
        Debug.Log($"即将传送到: {targetMapName}");
    }
    public override void OnPlayerExit()
    {}
    public override void OnPlayerStay()
    {}
}