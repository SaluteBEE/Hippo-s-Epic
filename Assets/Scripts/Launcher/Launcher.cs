using UnityEngine;

public class Launcher : MonoBehaviour
{
    [SerializeField] public LaunchConfig launchConfig;

    private void Awake()
    {
        if (launchConfig != null)
        {
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