using UnityEngine;

[DefaultExecutionOrder(10000)]
public class SpecialStageAlertUIFaceLock : MonoBehaviour
{
    [Header("Refs")]
    public Transform cam;

    [Header("Lock")]
    public bool locked = true;
    public bool useCameraForward = true;

    [Header("Debug")]
    public bool debugLog = false;

    void Awake()
    {
        ResolveCamera();
    }

    void OnEnable()
    {
        ResolveCamera();
        ApplyRotation();
    }

    void LateUpdate()
    {
        if (!locked)
            return;

        ApplyRotation();
    }

    public void SetLocked(bool value)
    {
        locked = value;

        if (locked)
            ApplyRotation();
    }

    void ApplyRotation()
    {
        ResolveCamera();

        if (cam == null)
            return;

        Vector3 forward;

        if (useCameraForward)
        {
            // 카메라 위치를 바라보지 않고,
            // 카메라가 바라보는 방향의 반대 방향으로 고정
            forward = -cam.forward;
        }
        else
        {
            // 기존 FaceCameraY 방식과 비슷한 위치 추적
            forward = cam.position - transform.position;
        }

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    void ResolveCamera()
    {
        if (cam != null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.MainCameraTransform != null)
            cam = GameManager.Instance.MainCameraTransform;
        else if (Camera.main != null)
            cam = Camera.main.transform;
    }
}