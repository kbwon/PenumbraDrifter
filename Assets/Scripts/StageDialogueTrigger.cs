using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StageDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueLine[] lines;

    [Header("Trigger")]
    public bool playOnce = true;
    public string playerTag = "Player";

    [Header("Behavior")]
    public bool lockPlayer = true;
    public bool forceExitShadow = true;
    public bool pauseGameWhileReading = true;

    bool played;
    bool playing;
    Collider triggerCollider;

    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (playing) return;
        if (playOnce && played) return;
        if (!IsPlayer(other)) return;

        StartCoroutine(PlayRoutine());
    }

    bool IsPlayer(Collider other)
    {
        if (other == null) return false;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null) return true;

        return !string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag);
    }

    IEnumerator PlayRoutine()
    {
        playing = true;
        played = true;

        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue == null)
            dialogue = FindFirstObjectByType<DialogueManager>();

        if (dialogue != null)
        {
            yield return dialogue.Show(
                lines,
                lockPlayer,
                forceExitShadow,
                pauseGameWhileReading
            );
        }
        else
        {
            Debug.LogWarning($"[StageDialogueTrigger] DialogueManager가 없습니다: {name}", this);
        }

        playing = false;
    }

    public void ResetPlayed()
    {
        played = false;
    }
}
