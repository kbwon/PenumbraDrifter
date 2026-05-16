using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Stage1GoalGate : MonoBehaviour
{
    [Header("Requirement")]
    public bool requireKeyItem = true;

    [Header("Next Scene")]
    public bool transitionToNextScene = true;
    public string nextSceneName = "SpecialStage";
    public string nextEntryId = "SpecialStage_Start";

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

        if (requireKeyItem)
        {
            if (Stage1ObjectiveState.Instance == null ||
                !Stage1ObjectiveState.Instance.hasKeyItem)
            {
                Debug.Log("[Stage1] 카드키가 없어 목표 건물에 들어갈 수 없습니다.");
                return;
            }
        }

        used = true;

        Debug.Log("[Stage1] 스테이지 클리어");

        if (!transitionToNextScene)
            return;

        if (SceneTransitionDirector.Instance == null)
        {
            Debug.LogWarning("[Stage1GoalGate] SceneTransitionDirector가 없습니다.");
            return;
        }

        Vector3 exitDir = GetExitDirection(player.transform);

        SceneTransitionDirector.Instance.StartStageTransition(
            nextSceneName,
            nextEntryId,
            exitDir
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