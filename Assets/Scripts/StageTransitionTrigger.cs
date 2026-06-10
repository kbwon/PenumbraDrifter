using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StageTransitionTrigger : MonoBehaviour
{
    [Header("Target Scene")]
    public string nextSceneName = "Stage01";
    public string entryId = "Stage01_Start";

    [Header("Exit Walk")]
    public Transform exitDirectionTarget;
    public Vector3 fallbackExitDirection = Vector3.forward;

    bool used;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (used) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        used = true;

        Vector3 exitDir = GetExitDirection(player.transform);

        if (UIAudioManager.Instance != null)
            UIAudioManager.Instance.PlayStageClear();

        if (SceneTransitionDirector.Instance != null)
        {
            SceneTransitionDirector.Instance.StartStageTransition(
                nextSceneName,
                entryId,
                exitDir
            );
        }
        else
        {
            Debug.LogError("SceneTransitionDirector가 씬에 없습니다.");
        }
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