using System.Collections.Generic;
using cfg.cfg.entity;
using UnityEngine;

public static class EntityConfigLoader
{
    public const int EntityTypeNPC = 0;
    public const int EntityTypeItem = 1;
    public const int EntityTypeTeleporter = 2;
    public const int EntityTypeDoor = 3;

    public const int StateNormal = 0;
    public const int StateDisabled = 1;
    public const int StateHidden = 2;

    public static void InitializeScene(string sceneName)
    {
        var tables = ManagerRegistry.Get<DataTableManager>()?.Tables;
        if (tables == null)
        {
            Debug.LogWarning("[EntityConfigLoader] Tables 未加载，跳过实体初始化");
            return;
        }

        SaveManager.Instance.Load();

        var colliders = Object.FindObjectsOfType<InteractionCollider>(true);
        var entityIdMap = new Dictionary<string, InteractionCollider>();
        foreach (var collider in colliders)
        {
            if (!string.IsNullOrEmpty(collider.EntityId))
                entityIdMap[collider.EntityId] = collider;
        }

        foreach (Entity entity in tables.TbEntity.DataList)
        {
            if (!entityIdMap.TryGetValue(entity.Id, out InteractionCollider collider))
                continue;

            int finalState = GetFinalState(entity);
            ApplyState(collider, finalState);
        }

        Debug.Log($"[EntityConfigLoader] 场景 {sceneName} 初始化完成，匹配 {entityIdMap.Count} 个实体");
    }

    public static int GetFinalState(Entity entity)
    {
        if (SaveManager.Instance.HasEntityState(entity.Id))
            return SaveManager.Instance.GetEntityState(entity.Id);

        return entity.InitialState;
    }

    public static void ApplyState(InteractionCollider collider, int state)
    {
        switch (state)
        {
            case StateNormal:
                collider.gameObject.SetActive(true);
                break;
            case StateDisabled:
                collider.gameObject.SetActive(true);
                var simple = collider as SimpleInteractionObject;
                if (simple != null)
                    simple.SetExecuted(true);
                break;
            case StateHidden:
                collider.gameObject.SetActive(false);
                break;
        }
    }

    public static void OnEntityInteracted(string entityId, int newState)
    {
        SaveManager.Instance.SetEntityState(entityId, newState);
        Debug.Log($"[EntityConfigLoader] 实体状态更新: {entityId} -> {newState}");
    }

    public static Entity GetEntityConfig(string entityId)
    {
        var tables = ManagerRegistry.Get<DataTableManager>()?.Tables;
        if (tables == null) return null;
        return tables.TbEntity.GetOrDefault(entityId);
    }
}
