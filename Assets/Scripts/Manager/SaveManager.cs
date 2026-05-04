using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager
{
    private static SaveManager _instance;
    public static SaveManager Instance => _instance ??= new SaveManager();

    private const string SaveFileName = "save.json";

    private Dictionary<string, int> _entityStates = new Dictionary<string, int>();
    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public IReadOnlyDictionary<string, int> EntityStates => _entityStates;

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

    public void Save()
    {
        var wrapper = new SaveDataWrapper { entities = new List<EntityStateEntry>(_entityStates.Count) };
        foreach (var kvp in _entityStates)
        {
            wrapper.entities.Add(new EntityStateEntry { id = kvp.Key, state = kvp.Value });
        }

        string json = JsonUtility.ToJson(wrapper, true);
        string dir = Path.GetDirectoryName(SavePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] 存档已保存: {SavePath} ({_entityStates.Count} 条记录)");
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
        if (wrapper?.entities == null)
            return;

        foreach (var entry in wrapper.entities)
        {
            _entityStates[entry.id] = entry.state;
        }

        Debug.Log($"[SaveManager] 存档已加载: {_entityStates.Count} 条记录");
    }

    public void ClearSave()
    {
        _entityStates.Clear();
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        Debug.Log("[SaveManager] 存档已清除");
    }

    [System.Serializable]
    private class SaveDataWrapper
    {
        public List<EntityStateEntry> entities;
    }

    [System.Serializable]
    private class EntityStateEntry
    {
        public string id;
        public int state;
    }
}
