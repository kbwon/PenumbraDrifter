using UnityEngine;

public class PushCameraAssist : MonoBehaviour
{
    [Header("Refs")]
    public FollowCamera followCamera;

    [Header("Cardinal Push Camera")]
    public bool useAssist = true;

    [Tooltip("밀기 중 Q/E 입력 등으로 카메라가 바뀌어도 다시 고정 각도로 돌립니다.")]
    public bool keepLockedWhilePushing = true;

    [Tooltip("F를 다시 눌러 밀기 모드를 종료했을 때 이전 카메라 각도로 돌아갈지 여부입니다.")]
    public bool restoreYawOnEnd = true;

    [Tooltip("잠금 순간 카메라 위치까지 즉시 맞춥니다. 부드러운 보정을 원하면 꺼두세요.")]
    public bool snapImmediatelyOnLock = false;

    [Tooltip("종료 시 이전 각도로 즉시 복귀할지 여부입니다. 부드러운 복귀를 원하면 꺼두세요.")]
    public bool snapImmediatelyOnEnd = false;

    [Header("Axis To Yaw")]
    public Vector2 xAxisYaws = new Vector2(0f, 180f);
    public Vector2 zAxisYaws = new Vector2(90f, 270f);

    [Header("Restore")]
    public bool roundPreviousYawToStep = true;
    public float restoreStepAngle = 45f;

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

        ApplyYaw(lockedYaw, false);
    }

    public void BeginPush()
    {
        if (!useAssist) return;

        active = true;
        hasLockedYaw = false;

        previousYaw = GetCurrentYaw();

        if (roundPreviousYawToStep)
            previousYaw = RoundToStep(previousYaw, GetRestoreStep());

        hasPreviousYaw = true;
    }

    public void LockForPushAxis(Vector3 pushAxis)
    {
        if (!useAssist) return;
        if (!followCamera) return;

        pushAxis.y = 0f;

        if (pushAxis.sqrMagnitude <= 0.0001f)
            return;

        pushAxis = SnapToWorldAxis(pushAxis);

        lockedYaw = ChooseCardinalYaw(pushAxis);
        hasLockedYaw = true;

        ApplyYaw(lockedYaw, snapImmediatelyOnLock);

        if (debugLog)
            Debug.Log($"[PushCameraAssist] Axis={pushAxis}, LockedYaw={lockedYaw}");
    }

    public void EndPush()
    {
        if (!useAssist) return;

        active = false;

        if (followCamera != null && restoreYawOnEnd && hasPreviousYaw)
            ApplyYaw(previousYaw, snapImmediatelyOnEnd);

        hasLockedYaw = false;
        hasPreviousYaw = false;
    }

    float ChooseCardinalYaw(Vector3 axis)
    {
        axis = SnapToWorldAxis(axis);

        float a;
        float b;

        if (Mathf.Abs(axis.x) >= Mathf.Abs(axis.z))
        {
            a = xAxisYaws.x;
            b = xAxisYaws.y;
        }
        else
        {
            a = zAxisYaws.x;
            b = zAxisYaws.y;
        }

        float currentYaw = GetCurrentYaw();

        float da = Mathf.Abs(Mathf.DeltaAngle(currentYaw, a));
        float db = Mathf.Abs(Mathf.DeltaAngle(currentYaw, b));

        return NormalizeYaw(da <= db ? a : b);
    }

    void ApplyYaw(float yaw, bool immediate)
    {
        if (!followCamera) return;

        yaw = NormalizeYaw(yaw);

        // 중요:
        // SetExternalYawLock을 쓰지 않는다.
        // SetGameplayYaw만 사용해야 FollowCamera의 위치 보간이 살아서 자연스럽게 이동한다.
        followCamera.SetGameplayYaw(yaw);

        if (immediate)
        {
            followCamera.SetYawImmediate(yaw);
            followCamera.SnapNow();
        }
    }

    Vector3 SnapToWorldAxis(Vector3 dir)
    {
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.z))
            return new Vector3(Mathf.Sign(dir.x), 0f, 0f);

        return new Vector3(0f, 0f, Mathf.Sign(dir.z));
    }

    float GetCurrentYaw()
    {
        if (followCamera != null)
            return NormalizeYaw(followCamera.CurrentYaw);

        return 0f;
    }

    float GetRestoreStep()
    {
        if (followCamera != null && followCamera.stepAngle > 0f)
            return followCamera.stepAngle;

        return restoreStepAngle;
    }

    float NormalizeYaw(float yaw)
    {
        yaw %= 360f;
        if (yaw < 0f) yaw += 360f;
        return yaw;
    }

    float RoundToStep(float yaw, float step)
    {
        if (step <= 0f) return NormalizeYaw(yaw);
        return NormalizeYaw(Mathf.Round(yaw / step) * step);
    }
}