using UnityEngine;

public class Launcher : MonoBehaviour
{
    public LaunchConfig launchConfig;

    private void Awake()
    {
        if (launchConfig != null)
        {
            // TODO: 对 launchConfig 进行校验，确保其中的 LaunchTask 不为 null
            Launch(launchConfig);
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogError("LaunchConfig is not assigned in the Launcher.");
        }
#endif
    }

    private void Launch(LaunchConfig config)
    {
        foreach (var task in config.LaunchTasks)
        {
            task.Execute();
        }
    }
}