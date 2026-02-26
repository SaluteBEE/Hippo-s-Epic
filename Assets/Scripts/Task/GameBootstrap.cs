using UnityEngine;
using QuestSystem;

public class GameBootstrap : MonoBehaviour
{
    public static TaskManager Tasks;

    private void Awake()
    {
        Tasks = new TaskManager();
        Tasks.InitializeFromCsv();

        // 订阅回调（用于 UI 刷新/弹窗）
        Tasks.OnTaskAdded += t => Debug.Log($"Task Added: {t.Def.Title}");
        Tasks.OnTaskUpdated += t => Debug.Log($"Task Updated: {t.Def.Title}");
        Tasks.OnTaskCompleted += t => Debug.Log($"Task Completed: {t.Def.Title}");

        Tasks.CreateTask(1);
        // 尝试读档，读不到就按剧情创建初始任务
        // if (!TaskSaveSystem.Load(Tasks))
        // {
        //     Tasks.CreateTask(1);
        // }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) TaskSaveSystem.Save(Tasks);
    }

    private void OnApplicationQuit()
    {
        TaskSaveSystem.Save(Tasks);
    }
}