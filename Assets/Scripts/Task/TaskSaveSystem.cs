using UnityEngine;

namespace QuestSystem
{
    public static class TaskSaveSystem
    {
        private const string Key = "TASK_SAVE_JSON";

        public static void Save(TaskManager mgr)
        {
            var data = mgr.BuildSaveData();
            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }

        public static bool Load(TaskManager mgr)
        {
            if (!PlayerPrefs.HasKey(Key)) return false;
            var json = PlayerPrefs.GetString(Key);
            var data = JsonUtility.FromJson<TaskSaveData>(json);
            mgr.RestoreFromSaveData(data);
            return true;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
        }
    }
}