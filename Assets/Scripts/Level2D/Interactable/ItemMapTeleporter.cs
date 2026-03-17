using UnityEngine;

public class ItemMapTeleporter : InteractionCollider
{
    [SerializeField] private string targetMapName;
    [SerializeField] private Vector2 targetEntrance;

    public override void OnPlayerExecute()
    {
        
    }

    public override void OnPlayerEnter()
    {
        if (LevelController.Instance == null || LevelController.Instance.MapManager == null)
            return;

        LevelController.Instance.MapManager.SwitchMap(targetMapName, targetEntrance, true);
    }

    public override void OnPlayerExit()
    {
    }

    public override void OnPlayerStay()
    {
    }
}