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
    [SerializeField] private string dialogueFileName = "dialogues/npc_001.csv";
    [SerializeField] private int startId = 1;
    [SerializeField] private bool autoAdvanceFirstLine = true;

    protected override void ExecuteInteraction()
    {
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

        //GameApp.Instance.StartDialogue(dialogueFileName, startId, autoAdvanceFirstLine);
    }
}