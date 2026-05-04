using UnityEngine;

public class SimpleInteractionObject : InteractionCollider
{
    [Header("Hint UI")]
    [SerializeField] protected NPCInteractHint interactHint;

    [Header("Texts")]
    [SerializeField] protected string enterText = "可以交互";
    [SerializeField] protected string enterButtonText = "按 E 交互";
    [SerializeField] protected string afterExecuteText = "";
    [SerializeField] protected string afterExecuteButtonText = "";

    [Header("Options")]
    [SerializeField] protected bool canRepeat = true;
    [SerializeField] protected bool hideHintAfterExecute = false;

    protected bool playerInside;
    protected bool hasExecuted;

    public override void OnPlayerEnter()
    {
        playerInside = true;
        RefreshHintOnEnter();
    }

    public override void OnPlayerExit()
    {
        playerInside = false;

        if (interactHint != null)
            interactHint.Hide();
    }

    public override void OnPlayerStay()
    {
    }

    public override void OnPlayerExecute()
    {
        if (!playerInside)
            return;

        if (hasExecuted && !canRepeat)
            return;

        ExecuteInteraction();
        hasExecuted = true;

        if (interactHint != null)
        {
            if (hideHintAfterExecute)
            {
                interactHint.Hide();
            }
            else
            {
                interactHint.Show(afterExecuteText, afterExecuteButtonText);
            }
        }
    }

    protected virtual void RefreshHintOnEnter()
    {
        if (interactHint == null)
            return;

        if (hasExecuted && !canRepeat)
            interactHint.Show(afterExecuteText, afterExecuteButtonText);
        else
            interactHint.Show(enterText, enterButtonText);
    }

    public bool HasBeenExecuted => hasExecuted;

    public void SetExecuted(bool executed)
    {
        hasExecuted = executed;
    }

    protected virtual void ExecuteInteraction()
    {

    }
}