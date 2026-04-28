using System;
using System.Collections;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    public LaunchConfig launchConfig;

    public void Run(LaunchConfig config, Action onCompleted = null)
    {
        if (config == null || config.LaunchTasks == null || config.LaunchTasks.Length == 0)
        {
            onCompleted?.Invoke();
            return;
        }

        StartCoroutine(RunTasksCoroutine(config.LaunchTasks, onCompleted));
    }

    private IEnumerator RunTasksCoroutine(LaunchTask[] tasks, Action onCompleted)
    {
        foreach (var task in tasks)
        {
            if (task == null) continue;
            Debug.Log($"[Launcher] 执行启动任务: {task.name}");
            task.Execute();
            yield return null;
        }

        Debug.Log("[Launcher] 所有启动任务完成");
        onCompleted?.Invoke();
    }
}
