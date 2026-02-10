using UnityEngine;

[CreateAssetMenu(fileName = "LaunchConfig", menuName = "Launcher/LaunchConfig")]
public class LaunchConfig : ScriptableObject
{
    public LaunchTask[] LaunchTasks;
}