
using UnityEngine;

public class MockLevelObject : LevelObject
{
    public override void OnLevelInitialized()
    {
        Debug.Log($"Level: LevelObject \"{gameObject.name}\" Initialized.", this);
    }
}