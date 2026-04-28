using UnityEngine;

[CreateAssetMenu(fileName = "InitDialogSystemTask", menuName = "Launcher/InitDialogSystemTask")]
public class InitDialogSystemTask : LaunchTask
{
    public override void Execute()
    {
        var dtm = ManagerRegistry.Get<DataTableManager>();
        if (dtm == null || !dtm.IsLoaded)
        {
            Debug.LogWarning("[InitDialogSystemTask] DataTableManager 未就绪，跳过");
            return;
        }

        ManagerRegistry.GetOrCreate<DialogManager>();
        ManagerRegistry.GetOrCreate<AnimationStateManager>();
        ManagerRegistry.GetOrCreate<DialogCharacterManager>();

        Debug.Log("[InitDialogSystemTask] 对话系统管理类已就绪");
    }
}
