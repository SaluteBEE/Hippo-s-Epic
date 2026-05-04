using UnityEngine;

public class ItemMapTeleporter : InteractionCollider
{
    [Header("同场景内传送")]
    [SerializeField] private string targetMapName;
    [SerializeField] private Vector2 targetEntrance;

    [Header("跨场景传送")]
    [SerializeField] private string targetSceneName;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool debugView = true;
    [SerializeField] private Color debugColor = new Color(0, 1, 1, 0.3f);
#endif

    public string TargetSceneName => targetSceneName;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!debugView) return;

        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Color baseColor = !string.IsNullOrEmpty(targetSceneName)
            ? new Color(1f, 0.5f, 0f, 0.35f)
            : new Color(0f, 1f, 0.5f, 0.35f);

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = baseColor;
        Vector2 size = col.size;
        Vector2 offset = col.offset;
        Gizmos.DrawCube(offset, size);
        Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.8f);
        Gizmos.DrawWireCube(offset, size);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (size.y * 0.5f + 0.3f),
            !string.IsNullOrEmpty(targetSceneName) ? $"→ {targetSceneName}" : $"→ {targetMapName}",
            new GUIStyle { normal = new GUIStyleState { textColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.9f) }, fontSize = 10, alignment = TextAnchor.LowerCenter }
        );
    }
#endif

    public override void OnPlayerExecute()
    {
    }

    public override void OnPlayerEnter()
    {
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            if (GameApp.Instance != null)
            {
                EntityConfigLoader.OnEntityInteracted(
                    !string.IsNullOrEmpty(EntityId) ? EntityId : "",
                    EntityConfigLoader.StateDisabled);
                GameApp.Instance.StartGame(targetSceneName);
            }
            return;
        }

        if (LevelController.Instance == null || LevelController.Instance.MapManager == null)
            return;

        LevelController.Instance.MapManager.SwitchMap(targetMapName, targetEntrance, true);
    }

    public override void OnPlayerExit()
    {
    }

    public override void OnPlayerStay()
    {
    }
}