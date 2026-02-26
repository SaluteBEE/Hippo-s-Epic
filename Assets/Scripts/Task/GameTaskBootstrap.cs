using System;
using UnityEngine;
using QuestSystem;

public class GameTaskBootstrap : MonoBehaviour
{
    public static TaskManager Tasks;

    private void Awake()
    {
        //导入任务csv，当前默认为StreamingAssets目录中的tasks.csv
        Tasks = new TaskManager();
        Tasks.InitializeFromCsv();

        // 订阅回调（用于 UI 刷新/弹窗）
        Tasks.OnTaskAdded += t => Debug.Log($"Task Added: {t.Def.Title}");
        Tasks.OnTaskUpdated += t => Debug.Log($"Task Updated: {t.Def.Title}");
        Tasks.OnTaskCompleted += t => Debug.Log($"Task Completed: {t.Def.Title}");

        
        // 创建任务接口：通过id数字创建任务
        Tasks.CreateTask(1);
        
        // 尝试读档，读不到就按剧情创建初始任务
        // if (!TaskSaveSystem.Load(Tasks))
        // {
        //     Tasks.CreateTask(1);
        // }
    }

    private void Start()
    {
        UIManager.Instance.OpenDialogue("dialogueTest.csv", startId: 1);
        
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