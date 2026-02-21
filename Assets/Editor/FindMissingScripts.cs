using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/查找缺失脚本")]
    public static void FindMissing()
    {
        List<GameObject> objectsWithMissing = new List<GameObject>();
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    objectsWithMissing.Add(go);
                    Debug.LogWarning("GameObject 包含缺失脚本: " + GetFullPath(go), go);
                    break;
                }
            }
        }
        Debug.Log($"发现 {objectsWithMissing.Count} 个 GameObject 包含缺失脚本");
    }

    private static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}