using UnityEngine;

public enum NPCInteractionType
{
    HintOnly,
    Dialogue
}

public class NPCInteractionCollider : SimpleInteractionObject
{
    [Header("Interaction")]
    [SerializeField] private NPCInteractionType interactionType = NPCInteractionType.HintOnly;

    [Header("Hint Only")]
    [SerializeField] private string hintOnlyText = "......";
    [SerializeField] private string hintOnlyButtonText = "好吧";

    [Header("Dialogue")]
    [SerializeField] private int startDialogId = 1001001;

    private const int StateDialogDone = 1;

    protected override void ExecuteInteraction()
    {
        if (!string.IsNullOrEmpty(EntityId))
            EntityConfigLoader.OnEntityInteracted(EntityId, StateDialogDone);

        switch (interactionType)
        {
            case NPCInteractionType.HintOnly:
                ExecuteHintOnly();
                break;

            case NPCInteractionType.Dialogue:
                ExecuteDialogue();
                break;
        }
    }

    private void ExecuteHintOnly()
    {
        afterExecuteText = hintOnlyText;
        afterExecuteButtonText = hintOnlyButtonText;
    }

    private void ExecuteDialogue()
    {
        if (interactHint != null)
            interactHint.Hide();

        afterExecuteText = "";
        afterExecuteButtonText = "";

        var uiManager = ManagerRegistry.Get<UIManager>();
        if (uiManager != null)
        {
            uiManager.Open<DialogWindow>(startDialogId);
        }
    }
}
