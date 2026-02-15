
using UnityEngine;

public class MockLevelObject : LevelObject
{
    public override void Initialize()
    {
        Debug.Log($"Level: LevelObject \"{gameObject.name}\" Initialized.", this);
    }
}