using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Stage1GoalGate : MonoBehaviour
{
    [Header("Requirement")]
    public bool requireKeyItem = true;

    [Header("Dialogue")]
    public DialogueLine[] keyMissingLines =
    {
        new DialogueLine("System", "Access denied. Keycard required."),
        new DialogueLine("Drifter", "So the card key comes first.")
    };

    public bool showUnlockedDialogueBeforeTransition = false;

    public DialogueLine[] keyAcceptedLines =
    {
        new DialogueLine("System", "Keycard accepted."),
        new DialogueLine("Drifter", "All right. I'm in.")
    };

    [Header("Next Scene")]
    public bool transitionToNextScene = true;
    public string nextSceneName = "SpecialStage";
    public string nextEntryId = "SpecialStage_Start";

    [Header("Exit Walk")]
    public Transform exitDirectionTarget;
    public Vector3 fallbackExitDirection = Vector3.forward;

    bool used;
    bool busy;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (busy) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        StartCoroutine(GateRoutine(player));
    }

    IEnumerator GateRoutine(PlayerController player)
    {
        busy = true;

        bool hasKeyItem =
            Stage1ObjectiveState.Instance != null &&
            Stage1ObjectiveState.Instance.hasKeyItem;

        if (requireKeyItem && !hasKeyItem)
        {
            yield return ShowDialogue(keyMissingLines);
            busy = false;
            yield break;
        }

        used = true;

        if (showUnlockedDialogueBeforeTransition)
            yield return ShowDialogue(keyAcceptedLines);

        Debug.Log("[Stage1] Stage clear.");

        if (!transitionToNextScene)
        {
            busy = false;
            yield break;
        }

        if (SceneTransitionDirector.Instance == null)
        {
            Debug.LogWarning("[Stage1GoalGate] SceneTransitionDirector is missing.");
            busy = false;
            yield break;
        }

        Vector3 exitDir = GetExitDirection(player.transform);

        if (UIAudioManager.Instance != null)
            UIAudioManager.Instance.PlayStageClear();

        SceneTransitionDirector.Instance.StartStageTransition(
            nextSceneName,
            nextEntryId,
            exitDir
        );

        busy = false;
    }

    IEnumerator ShowDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            yield break;

        DialogueManager dialogue = DialogueManager.Instance;

        if (dialogue == null)
            dialogue = FindFirstObjectByType<DialogueManager>();

        if (dialogue == null)
        {
            Debug.LogWarning("[Stage1GoalGate] DialogueManager is missing.");
            yield break;
        }

        yield return dialogue.Show(
            lines,
            lockPlayer: true,
            forceExitShadow: true,
            pauseGame: true
        );
    }

    Vector3 GetExitDirection(Transform player)
    {
        if (exitDirectionTarget != null)
        {
            Vector3 dir = exitDirectionTarget.position - player.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                return dir.normalized;
        }

        Vector3 fallback = fallbackExitDirection;
        fallback.y = 0f;

        if (fallback.sqrMagnitude <= 0.0001f)
            fallback = transform.forward;

        return fallback.normalized;
    }
}