using UnityEngine;

public class Launcher : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void InitializeAfterAssembliesLoaded()
    {
        MVVMManager.Initialize();
        MVVMCoordinator.Initialize();
    }
}