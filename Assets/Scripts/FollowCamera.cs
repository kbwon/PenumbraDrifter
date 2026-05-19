using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [Header("Stability")]
    public bool stableOrthographicRotation = true;
    [Header("Cinematic")]
    public bool cinematicInstantPosition = true;

    public Transform target;
    public Vector3 offset = new Vector3(0f, 10f, -15f);
    public Vector3 lookAtOffset = new Vector3(0f, 6f, 0f);

    public float followSmooth = 12f;
    public float rotateSmooth = 18f;

    public float stepAngle = 45f;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    float targetYaw;
    float currentYaw;

    Camera cachedCamera;

    // 시네마틱 중에는 플레이어 입력 대신 연출 값으로 카메라를 제어한다.
    bool isCinematic;
    bool hasFocusOverride;
    Vector3 focusOverridePoint;
    float runtimeDistanceScale = 1f;
    float runtimeOrthoSize;

    public bool IsCinematic => isCinematic;
    public Camera CachedCamera => cachedCamera;
    public float CurrentYaw => NormalizeYaw(currentYaw);

    float NormalizeYaw(float yaw)
    {
        yaw %= 360f;
        if (yaw < 0f) yaw += 360f;
        return yaw;
    }

    void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterFollowCamera(this);

        if (cachedCamera == null)
            cachedCamera = GetComponentInChildren<Camera>(true);

        if (cachedCamera != null && GameManager.Instance != null)
            GameManager.Instance.RegisterMainCamera(cachedCamera);
    }

    void Start()
    {
        currentYaw = transform.eulerAngles.y;
        targetYaw = currentYaw;

        if (target == null && GameManager.Instance != null)
            target = GameManager.Instance.PlayerTransform;

        if (cachedCamera != null)
            runtimeOrthoSize = cachedCamera.orthographicSize;

        SnapNow();
    }

    void LateUpdate()
    {
        if (target == null && GameManager.Instance != null)
            target = GameManager.Instance.PlayerTransform;

        if (!hasFocusOverride && !target) return;

        // 시네마틱이 아닐 때만 Q/E 회전을 받는다.
        if (!isCinematic)
        {
            if (Input.GetKeyDown(rotateLeftKey)) targetYaw += stepAngle;
            if (Input.GetKeyDown(rotateRightKey)) targetYaw -= stepAngle;

            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotateSmooth * Time.deltaTime);
        }

        ApplyCamera(isCinematic && cinematicInstantPosition);
    }

    Vector3 GetFocusPoint()
    {
        if (hasFocusOverride)
            return focusOverridePoint;

        return target.position + lookAtOffset;
    }

    void ApplyCamera(bool instantPosition)
    {
        Vector3 lookPoint = GetFocusPoint();

        Quaternion yawRot = Quaternion.Euler(0f, currentYaw, 0f);
        Vector3 desiredOffset = yawRot * (offset * runtimeDistanceScale);
        Vector3 desiredPos = lookPoint + desiredOffset;

        if (instantPosition)
            transform.position = desiredPos;
        else
            transform.position = Vector3.Lerp(transform.position, desiredPos, followSmooth * Time.deltaTime);

        Vector3 lookDir;

        if (stableOrthographicRotation && cachedCamera != null && cachedCamera.orthographic)
        {
            // 핵심:
            // 현재 카메라 위치에서 타깃을 다시 바라보지 않고,
            // yaw + offset으로 정해진 고정 시점 방향을 유지한다.
            lookDir = -desiredOffset;
        }
        else
        {
            lookDir = lookPoint - transform.position;
        }

        if (lookDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        if (cachedCamera != null && cachedCamera.orthographic)
            cachedCamera.orthographicSize = runtimeOrthoSize;
    }

    // 평소 플레이 시 카메라가 바라봐야 할 기준점을 반환한다.
    public Vector3 GetGameplayFocusPoint()
    {
        if (target == null && GameManager.Instance != null)
            target = GameManager.Instance.PlayerTransform;

        return target != null
            ? target.position + lookAtOffset
            : transform.position + transform.forward * 5f;
    }

    // 연출 중에는 특정 월드 좌표를 직접 바라보게 한다.
    public void SetFocusPoint(Vector3 worldPoint)
    {
        hasFocusOverride = true;
        focusOverridePoint = worldPoint;
    }

    public void ClearFocusOverride()
    {
        hasFocusOverride = false;
    }

    // 연출 시작과 종료 시 각도를 바로 고정한다.
    public void SetYawImmediate(float yaw)
    {
        currentYaw = yaw;
        targetYaw = yaw;
    }

    // overview에서는 offset 전체를 배수로 키워 더 멀리 보여준다.
    public void SetDistanceScaleImmediate(float distanceScale)
    {
        runtimeDistanceScale = Mathf.Max(0.01f, distanceScale);
    }

    // 직교 카메라일 때 화면에 더 많이 보이도록 크기를 조절한다.
    public void SetOrthoSizeImmediate(float orthoSize)
    {
        runtimeOrthoSize = Mathf.Max(0.01f, orthoSize);

        if (cachedCamera != null && cachedCamera.orthographic)
            cachedCamera.orthographicSize = runtimeOrthoSize;
    }

    public void SetCinematicMode(bool enabled)
    {
        isCinematic = enabled;

        if (!enabled)
            ClearFocusOverride();
    }

    // 연출 종료 후 플레이용 yaw 상태를 정확히 맞춘다.
    public void SetGameplayYaw(float yaw)
    {
        currentYaw = yaw;
        targetYaw = yaw;
    }

    public void SnapNow()
    {
        ApplyCamera(true);
    }

    public void SetCinematicInstantPosition(bool instant)
    {
        cinematicInstantPosition = instant;
    }
}