using UnityEngine;

public class SpecialStageAlertUIFaceLock : MonoBehaviour
{
    public bool locked = true;

    [Header("Camera")]
    public Transform targetCamera;
    public bool useCameraForward = true;

    [Header("Rotation Offset")]
    public float yawOffset = 180f;
    public bool keepUpright = true;

    Quaternion lockedRotation;
    bool hasLockedRotation;

    void OnEnable()
    {
        CacheCamera();
        ApplyLockRotation();
    }

    void Start()
    {
        CacheCamera();
        ApplyLockRotation();
    }

    void LateUpdate()
    {
        if (!locked)
            return;

        if (!hasLockedRotation)
            ApplyLockRotation();

        transform.rotation = lockedRotation;
    }

    void CacheCamera()
    {
        if (targetCamera != null)
            return;

        if (GameManager.Instance != null && GameManager.Instance.MainCameraTransform != null)
            targetCamera = GameManager.Instance.MainCameraTransform;
        else if (Camera.main != null)
            targetCamera = Camera.main.transform;
    }

    void ApplyLockRotation()
    {
        CacheCamera();

        if (targetCamera == null)
            return;

        Vector3 forward = useCameraForward
            ? -targetCamera.forward
            : targetCamera.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            return;

        forward.Normalize();

        Quaternion baseRotation = Quaternion.LookRotation(forward, Vector3.up);
        lockedRotation = baseRotation * Quaternion.Euler(0f, yawOffset, 0f);

        if (keepUpright)
        {
            Vector3 euler = lockedRotation.eulerAngles;
            lockedRotation = Quaternion.Euler(0f, euler.y, 0f);
        }

        hasLockedRotation = true;
    }

    public void Refresh()
    {
        hasLockedRotation = false;
        ApplyLockRotation();
    }
}