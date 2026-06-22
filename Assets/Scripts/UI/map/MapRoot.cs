using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapRoot : MonoBehaviour
{
    [System.Serializable]
    public class MapNodeEntry
    {
        [Tooltip("Button reference")]
        public Button button;

        [Tooltip("Target map to switch to")]
        public SceneMapId targetMap;

        [Tooltip("Head position when player is at this map")]
        public Vector2 headPosition;

        [Tooltip("Whether this node is interactable")]
        public bool interactable = true;
    }

    [SerializeField] private MapNodeEntry[] nodes;

    public IReadOnlyList<MapNodeEntry> Nodes => nodes;

    public void Initialize(System.Action<SceneMapId> onNodeClicked)
    {
        if (nodes == null) return;

        foreach (var entry in nodes)
        {
            if (entry.button == null) continue;

            entry.button.interactable = entry.interactable;
            entry.button.onClick.RemoveAllListeners();

            if (entry.interactable)
            {
                var targetMap = entry.targetMap;
                entry.button.onClick.AddListener(() => onNodeClicked?.Invoke(targetMap));
            }
        }
    }

    public Vector2? GetHeadPosition(SceneMapId currentMap)
    {
        if (nodes == null) return null;

        foreach (var entry in nodes)
        {
            if (entry.targetMap == currentMap)
                return entry.headPosition;
        }

        return null;
    }

    public void SetNodeInteractable(SceneMapId mapId, bool interactable)
    {
        if (nodes == null) return;

        foreach (var entry in nodes)
        {
            if (entry.targetMap == mapId && entry.button != null)
            {
                entry.interactable = interactable;
                entry.button.interactable = interactable;
            }
        }
    }
}
