using System.IO;
using UnityEngine;

public class DataTableManager : MonoBehaviour
{
    public cfg.Tables Tables { get; private set; }

    public bool IsLoaded => Tables != null;

    public void LoadTables()
    {
        string binDir = Path.Combine(Application.streamingAssetsPath, "Gen", "bin");

        Tables = new cfg.Tables(tableName =>
        {
            string path = Path.Combine(binDir, tableName + ".bytes");
            byte[] bytes = File.ReadAllBytes(path);
            return new Luban.ByteBuf(bytes);
        });

        ManagerRegistry.SetTables(Tables);
    }
}
