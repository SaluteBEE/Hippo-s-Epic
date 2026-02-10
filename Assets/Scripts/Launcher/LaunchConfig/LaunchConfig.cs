using UnityEngine;

[CreateAssetMenu(fileName = "LaunchConfig", menuName = "Launcher/LaunchConfig")]
public class LaunchConfig : ScriptableObject
{
    // TODO: 后续有空修改为私有字段，并提供只读属性访问
    public LaunchTask[] LaunchTasks;
}