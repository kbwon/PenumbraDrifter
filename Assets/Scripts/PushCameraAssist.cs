using UnityEngine;

public class PushCameraAssist : MonoBehaviour
{
    [Header("Refs")]
    public FollowCamera followCamera;

    [Header("Assist")]
    public bool useAssist = true;
    public bool keepLockedWhilePushing = true;

    [Tooltip("F를 뗐을 때 이전 카메라 각도로 되돌릴지 여부입니다. 처음에는 끄는 것을 추천합니다.")]
    public bool restoreYawOnEnd = false;

    [Tooltip("현재 카메라 각도와 가까운 후보를 약간 선호하게 하는 값입니다.")]
    public float closeYawWeight = 0.08f;

    [Tooltip("FollowCamera의 stepAngle을 못 찾을 때 사용할 기본 회전 단위입니다.")]
    public float fallbackStepAngle = 45f;

    [Tooltip("카메라 yaw와 실제 화면 오른쪽 방향이 어긋나면 90 또는 -90으로 조정해보세요.")]
    public float rightVectorYawOffset = 0f;

    [Header("Debug")]
    public bool debugLog;

    bool active;
    bool hasLockedYaw;
    bool hasPreviousYaw;

    float lockedYaw;
    float previousYaw;

    void Awake()
    {
        if (!followCamera)
        {
            if (GameManager.Instance != null)
                followCamera = GameManager.Instance.followCamera;

            if (!followCamera)
                followCamera = FindFirstObjectByType<FollowCamera>();
        }
    }

    void LateUpdate()
    {
        if (!useAssist) return;
        if (!active) return;
        if (!hasLockedYaw) return;
        if (!keepLockedWhilePushing) return;
        if (!followCamera) return;

        // 밀기 중에는 다른 카메라 입력이 들어와도 다시 고정 yaw로 돌려놓는다.
        followCamera.SetGameplayYaw(lockedYaw);
    }

    public void BeginPush()
    {
        if (!useAssist) return;

        active = true;
        hasLockedYaw = false;

        previousYaw = GetApproxCurrentYaw();
        hasPreviousYaw = true;
    }

    public void LockForPushAxis(Vector3 pushAxis)
    {
        if (!useAssist) return;
        if (!followCamera) return;

        pushAxis.y = 0f;

        if (pushAxis.sqrMagnitude <= 0.0001f)
            return;

        pushAxis.Normalize();

        lockedYaw = ChooseBestYaw(pushAxis);
        hasLockedYaw = true;

        followCamera.SetGameplayYaw(lockedYaw);

        if (debugLog)
            Debug.Log($"[PushCameraAssist] PushAxis={pushAxis}, LockedYaw={lockedYaw}");
    }

    public void EndPush()
    {
        if (!useAssist) return;

        active = false;

        if (restoreYawOnEnd && hasPreviousYaw && followCamera)
            followCamera.SetGameplayYaw(previousYaw);

        hasLockedYaw = false;
        hasPreviousYaw = false;
    }

    float ChooseBestYaw(Vector3 pushAxis)
    {
        float step = GetStepAngle();
        float currentYaw = GetApproxCurrentYaw();

        int count = Mathf.Max(1, Mathf.RoundToInt(360f / step));

        float bestYaw = RoundToStep(currentYaw, step);
        float bestScore = -999f;

        for (int i = 0; i < count; i++)
        {
            float candidateYaw = NormalizeYaw(i * step);

            // 이 yaw에서 화면 오른쪽 방향이라고 가정되는 월드 방향
            Vector3 candidateRight =
                Quaternion.Euler(0f, candidateYaw + rightVectorYawOffset, 0f) * Vector3.right;

            candidateRight.y = 0f;
            candidateRight.Normalize();

            // pushAxis가 화면 좌우 방향과 얼마나 가까운지
            float horizontalScore = Mathf.Abs(Vector3.Dot(pushAxis, candidateRight));

            // 너무 멀리 도는 것을 약하게 방지
            float yawDelta = Mathf.Abs(Mathf.DeltaAngle(currentYaw, candidateYaw));
            float closePenalty = yawDelta / 180f * closeYawWeight;

            float score = horizontalScore - closePenalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestYaw = candidateYaw;
            }
        }

        return bestYaw;
    }

    float GetStepAngle()
    {
        if (followCamera != null && followCamera.stepAngle > 0f)
            return Mathf.Abs(followCamera.stepAngle);

        return Mathf.Max(1f, Mathf.Abs(fallbackStepAngle));
    }

    float GetApproxCurrentYaw()
    {
        if (followCamera != null)
            return NormalizeYaw(followCamera.transform.eulerAngles.y);

        return 0f;
    }

    float RoundToStep(float yaw, float step)
    {
        return NormalizeYaw(Mathf.Round(yaw / step) * step);
    }

    float NormalizeYaw(float yaw)
    {
        yaw %= 360f;
        if (yaw < 0f) yaw += 360f;
        return yaw;
    }
}