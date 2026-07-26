using System;
using System.Collections.Generic;
using UnityEngine;

public enum EquipSlot
{
    Head = 1,
    Body = 2,
    Hand = 3,
    Foot = 4
}

public class EquipManager
{
    private static EquipManager _instance;
    public static EquipManager Instance => _instance ??= new EquipManager();

    private readonly Dictionary<EquipSlot, EquipEntry> _equips = new Dictionary<EquipSlot, EquipEntry>();

    public IReadOnlyDictionary<EquipSlot, EquipEntry> Equips => _equips;

    public event Action<EquipSlot, int> OnEquipChanged;

    public int GetEquippedItemId(EquipSlot slot)
    {
        return _equips.TryGetValue(slot, out var e) ? e.itemId : 0;
    }

    public bool IsEquipped(EquipSlot slot)
    {
        return _equips.ContainsKey(slot) && _equips[slot].itemId > 0;
    }

    public bool IsInstanceEquipped(int instanceId)
    {
        foreach (var kvp in _equips)
        {
            if (kvp.Value.instanceId == instanceId)
                return true;
        }
        return false;
    }

    public EquipSlot GetEquipSlotByInstance(int instanceId)
    {
        foreach (var kvp in _equips)
        {
            if (kvp.Value.instanceId == instanceId)
                return kvp.Key;
        }
        return (EquipSlot)(-1);
    }

    public bool EquipByInstance(int instanceId)
    {
        var entry = BagManager.Instance.GetByInstanceId(instanceId);
        if (entry == null)
        {
            Debug.LogWarning($"[EquipManager] 实例不存在: {instanceId}");
            return false;
        }

        int itemId = entry.itemId;

        var itemCfg = GetItemConfig(itemId);
        if (itemCfg == null)
        {
            Debug.LogWarning($"[EquipManager] 物品配置不存在: {itemId}");
            return false;
        }

        if (itemCfg.Type != 4)
        {
            Debug.LogWarning($"[EquipManager] 物品 {itemId} 不是装备类型");
            return false;
        }

        EquipSlot slot = (EquipSlot)itemCfg.Childtype;
        if (slot < EquipSlot.Head || slot > EquipSlot.Foot)
        {
            Debug.LogWarning($"[EquipManager] 物品 {itemId} 的装备槽位无效: {itemCfg.Childtype}");
            return false;
        }

        if (!BagManager.Instance.RemoveByInstanceId(instanceId))
            return false;

        if (_equips.TryGetValue(slot, out var oldEquip) && oldEquip.itemId > 0)
        {
            var oldItemCfg = GetItemConfig(oldEquip.itemId);
            if (oldItemCfg != null)
            {
                int oldBuffId = BuffManager.ParseBuffId(oldItemCfg.Param1);
                if (oldBuffId > 0)
                    BuffManager.Instance.RemoveBuff(oldBuffId);
            }
            BagManager.Instance.AddItem(oldEquip.itemId);
        }

        _equips[slot] = new EquipEntry { instanceId = instanceId, itemId = itemId };

        int buffId = BuffManager.ParseBuffId(itemCfg.Param1);
        if (buffId > 0)
            BuffManager.Instance.ApplyBuff(buffId, fromEquip: true);

        OnEquipChanged?.Invoke(slot, itemId);
        return true;
    }

    public bool Unequip(EquipSlot slot)
    {
        if (!_equips.TryGetValue(slot, out var equip) || equip.itemId <= 0)
            return false;

        int itemId = equip.itemId;

        var itemCfg = GetItemConfig(itemId);
        if (itemCfg != null)
        {
            int buffId = BuffManager.ParseBuffId(itemCfg.Param1);
            if (buffId > 0)
                BuffManager.Instance.RemoveBuff(buffId);
        }

        _equips.Remove(slot);
        BagManager.Instance.AddItem(itemId);
        OnEquipChanged?.Invoke(slot, 0);
        return true;
    }

    public void Clear()
    {
        _equips.Clear();
    }

    public List<EquipSaveEntry> BuildSaveData()
    {
        var result = new List<EquipSaveEntry>(_equips.Count);
        foreach (var kvp in _equips)
        {
            if (kvp.Value.itemId > 0)
                result.Add(new EquipSaveEntry
                {
                    slot = (int)kvp.Key,
                    instanceId = kvp.Value.instanceId,
                    itemId = kvp.Value.itemId
                });
        }
        return result;
    }

    public void RestoreFromSaveData(List<EquipSaveEntry> data)
    {
        _equips.Clear();
        if (data == null) return;

        foreach (var entry in data)
        {
            if (entry.itemId > 0)
            {
                _equips[(EquipSlot)entry.slot] = new EquipEntry
                {
                    instanceId = entry.instanceId > 0 ? entry.instanceId : -entry.itemId,
                    itemId = entry.itemId
                };
            }
        }
    }

    private cfg.cfg.item.Item GetItemConfig(int itemId)
    {
        if (ManagerRegistry.TryGet<DataTableManager>(out var dtm) && dtm.Tables != null)
        {
            return dtm.Tables.TbItem.GetOrDefault(itemId);
        }
        return null;
    }

    public struct EquipEntry
    {
        public int instanceId;
        public int itemId;
    }

    [Serializable]
    public class EquipSaveEntry
    {
        public int slot;
        public int instanceId;
        public int itemId;
    }
}
