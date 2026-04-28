using UnityEngine;

[CreateAssetMenu(fileName = "LoadTablesTask", menuName = "Launcher/LoadTablesTask")]
public class LoadTablesTask : LaunchTask
{
    public override void Execute()
    {
        var dtm = ManagerRegistry.Get<DataTableManager>();
        if (dtm == null)
        {
            Debug.LogError("[LoadTablesTask] DataTableManager 未注册");
            return;
        }

        dtm.LoadTables();
        Debug.Log("[LoadTablesTask] 配置表加载完成");
    }
}
