using System.IO;
using Luban;

class TestDataLoader
{
    public static string StreamingAssetsPath => Path.Combine(
        Directory.GetParent(UnityEngine.Application.dataPath).FullName,
        "Assets", "StreamingAssets"
    );

    public static string BinDir => Path.Combine(StreamingAssetsPath, "Gen", "bin");

    public static cfg.Tables LoadTables()
    {
        return new cfg.Tables(tableName =>
        {
            string path = Path.Combine(BinDir, tableName + ".bytes");
            byte[] bytes = File.ReadAllBytes(path);
            return new ByteBuf(bytes);
        });
    }

    public static ByteBuf LoadBinaryTable(string tableName)
    {
        string path = Path.Combine(BinDir, tableName + ".bytes");
        byte[] bytes = File.ReadAllBytes(path);
        return new ByteBuf(bytes);
    }
}
