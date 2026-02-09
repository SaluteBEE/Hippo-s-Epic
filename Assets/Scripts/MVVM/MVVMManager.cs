using UnityEngine;

public static class MVVMManager
{
    private static bool isInitialized = false;
    private static bool IsInitialized
    {
        get { return isInitialized; }
        set
        {
            if (isInitialized && value)
            {
                throw new System.Exception("MVVMManager is already initialized. Multiple initializations are not allowed.");
            }
            isInitialized = value;
        }
    }
    
    private static Transform viewsGameObjectParent;
    public static Transform ViewsGameObjectParent
    {
        get
        {
            if (viewsGameObjectParent == null)
            {
                throw new System.Exception("MVVMManager.ViewsParent is not set. Please set it before accessing.");
            }
            return viewsGameObjectParent;
        }
        private set { viewsGameObjectParent = value; }
    }

    public static void Initialize()
    {
        GameObject viewsParentGO = new GameObject("MVVM_Views");
        Initialize(viewsParentGO.transform);
    }

    public static void Initialize(Transform viewsGameObjectParent)
    {
        ViewsGameObjectParent = viewsGameObjectParent;
    }
}
