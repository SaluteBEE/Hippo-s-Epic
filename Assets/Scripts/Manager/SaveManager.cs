using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance ??= new SaveManager();

    private const string SaveFileName = "save.json";

    private Dictionary<string, int> _entityStates = new Dictionary<string, int>();
    private int _currentMapId = -1;
    private readonly List<int> _completedBattles = new List<int>();
    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public IReadOnlyDictionary<string, int> EntityStates => _entityStates;

    public IReadOnlyList<int> CompletedBattles => _completedBattles;

    public int CurrentMapId
    {
        get => _currentMapId;
        set => _currentMapId = value;
    }

    public int GetEntityState(string entityId)
    {
        return _entityStates.TryGetValue(entityId, out int state) ? state : -1;
    }

    public void SetEntityState(string entityId, int state)
    {
        _entityStates[entityId] = state;
    }

    public bool HasEntityState(string entityId)
    {
        return _entityStates.ContainsKey(entityId);
    }

    public void RecordCompletedBattle(int battleId)
    {
        if (!_completedBattles.Contains(battleId))
            _completedBattles.Add(battleId);
    }

    public bool IsBattleCompleted(int battleId, BattleResult result)
    {
        if (result != BattleResult.Win) return false;
        return _completedBattles.Contains(battleId);
    }

    public void Save()
    {
        var wrapper = new SaveDataWrapper
        {
            entities = new List<EntityStateEntry>(_entityStates.Count),
            bag = BagManager.Instance.BuildSaveData(),
            equip = EquipManager.Instance.BuildSaveData(),
            quest = QuestManager.Instance.BuildSaveData(),
            characterStats = CharacterStatsManager.Instance.BuildSaveData(),
            buffs = BuffManager.Instance.BuildSaveData(),
            finishedDialogs = new List<int>(DialogManager.FinishedDialogs),
            metConditions = ConditionSystem.Instance.BuildMetConditionIds(),
            completedBattles = new List<int>(_completedBattles),
            currentMapId = (int)_currentMapId
        };

        foreach (var kvp in _entityStates)
        {
            wrapper.entities.Add(new EntityStateEntry { id = kvp.Key, state = kvp.Value });
        }

        string json = JsonUtility.ToJson(wrapper, true);
        string dir = Path.GetDirectoryName(SavePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] 存档已保存: {SavePath} ({_entityStates.Count} 条记录, {wrapper.bag?.Count ?? 0} 种物品, {wrapper.equip?.Count ?? 0} 件装备, {wrapper.quest?.entries?.Count ?? 0} 个任务)");
    }

    public void Load()
    {
        _entityStates.Clear();

        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] 无存档文件，使用默认状态");
            return;
        }

        string json = File.ReadAllText(SavePath);
        var wrapper = JsonUtility.FromJson<SaveDataWrapper>(json);
        if (wrapper?.entities != null)
        {
            foreach (var entry in wrapper.entities)
            {
                _entityStates[entry.id] = entry.state;
            }
        }

        BagManager.Instance.RestoreFromSaveData(wrapper?.bag);
        EquipManager.Instance.RestoreFromSaveData(wrapper?.equip);
        QuestManager.Instance.RestoreFromSaveData(wrapper?.quest);
        CharacterStatsManager.Instance.RestoreFromSaveData(wrapper?.characterStats);
        BuffManager.Instance.RestoreFromSaveData(wrapper?.buffs);
        DialogManager.RestoreFinishedDialogs(wrapper?.finishedDialogs);
        ConditionSystem.Instance.RestoreMetConditions(wrapper?.metConditions);

        _completedBattles.Clear();
        if (wrapper?.completedBattles != null)
        {
            foreach (int id in wrapper.completedBattles)
            {
                if (!_completedBattles.Contains(id))
                    _completedBattles.Add(id);
            }
        }

        if (wrapper != null && wrapper.currentMapId >= 0)
            _currentMapId = wrapper.currentMapId;

        Debug.Log($"[SaveManager] 存档已加载: {_entityStates.Count} 条记录");
    }

    public void ClearSave()
    {
        _entityStates.Clear();
        _completedBattles.Clear();
        BagManager.Instance.Clear();
        EquipManager.Instance.Clear();
        QuestManager.Instance.Clear();
        BuffManager.Instance.Clear();
        CharacterStatsManager.Instance.Clear();
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        Debug.Log("[SaveManager] 存档已清除");
    }

    [System.Serializable]
    private class SaveDataWrapper
    {
        public List<EntityStateEntry> entities;
        public List<BagManager.BagSaveEntry> bag;
        public List<EquipManager.EquipSaveEntry> equip;
        public QuestSaveData quest;
        public CharacterStats.StatsSaveData characterStats;
        public List<BuffManager.BuffSaveEntry> buffs;
        public List<int> finishedDialogs;
        public List<int> metConditions;
        public List<int> completedBattles;
        public int currentMapId;
    }

    [System.Serializable]
    private class EntityStateEntry
    {
        public string id;
        public int state;
    }
}
