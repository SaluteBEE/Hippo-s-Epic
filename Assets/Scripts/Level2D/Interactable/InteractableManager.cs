using UnityEngine;

public static class InteractableManager
{
    public static void InitializeMap(Map map)
    {
        if (map == null)
            return;

        var interactables = map.InteractableList;
        int matchCount = 0;

        foreach (var interactable in interactables)
        {
            if (interactable == null)
                continue;

            var firstPhase = interactable.GetPhase(0);
            if (firstPhase == null)
                continue;

            string eid = interactable.EntityId;
            int state;

            if (SaveManager.Instance.HasEntityState(eid))
            {
                state = SaveManager.Instance.GetEntityState(eid);
            }
            else
            {
                state = firstPhase.state;
            }

            interactable.ApplyState(state);
            interactable.InitializeHint();
            matchCount++;
        }

        Debug.Log($"[InteractableManager] 地图 {map.name} 初始化完成，匹配 {matchCount} 个交互实体");
    }

    public static void InitializeScene(Map[] sceneMaps)
    {
        if (sceneMaps == null || sceneMaps.Length == 0)
            return;

        int totalCount = 0;
        foreach (var map in sceneMaps)
        {
            if (map == null) continue;
            InitializeMap(map);
            totalCount += map.InteractableList.Count;
        }

        Debug.Log($"[InteractableManager] 场景初始化完成，共 {sceneMaps.Length} 个地图，{totalCount} 个交互实体");
    }

    public static void InitializeScene()
    {
        var maps = Object.FindObjectsOfType<Map>(true);
        InitializeScene(maps);
    }
}
