using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableObject : PlayerInteractable
{
    [Header("Refs")]
    public Rigidbody rb;
    public Collider mainCollider;

    [Header("Range")]
    public float interactDistance = 1.2f;

    [Header("Push")]
    public float pushSpeed = 1.25f;
    public float minInputMagnitude = 0.15f;
    [Range(-1f, 1f)] public float minTowardDot = 0.05f;
    public bool snapToWorldAxis = true;

    [Header("Locking")]
    public bool freezeHorizontalWhenIdle = true;

    [Header("Push Camera Assist")]
    public PushCameraAssist cameraAssist;
    public bool useCameraAssist = true;

    [Tooltip("카메라가 회전해도 밀기 방향이 바뀌지 않도록, 처음 결정한 축을 유지합니다.")]
    public bool lockPushAxisWhileHolding = true;

    bool hasLockedPushAxis;
    Vector3 lockedPushAxis;

    [Header("Visual Occlusion Proxy")]
    public PushOcclusionProxyGroup occlusionProxy;
    public bool useOcclusionProxy = true;

    PlayerInteractController currentInteractor;

    static readonly RigidbodyConstraints FreezeRotAll =
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationY |
        RigidbodyConstraints.FreezeRotationZ;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!mainCollider) mainCollider = GetComponent<Collider>();
        if (!cameraAssist) cameraAssist = GetComponent<PushCameraAssist>();

        ApplyIdleConstraints();
    }

    public override bool CanInteract(PlayerInteractController interactor)
    {
        if (interactor == null || interactor.Player == null)
            return false;

        Vector3 playerPos = interactor.Player.transform.position;
        Vector3 point = GetInteractionPoint(interactor);
        point.y = playerPos.y;

        return (point - playerPos).sqrMagnitude <= interactDistance * interactDistance;
    }

    public override Vector3 GetInteractionPoint(PlayerInteractController interactor)
    {
        if (mainCollider == null || interactor == null || interactor.Player == null)
            return transform.position;

        return mainCollider.ClosestPoint(interactor.Player.transform.position);
    }

    public override void BeginInteract(PlayerInteractController interactor)
    {
        currentInteractor = interactor;
        hasLockedPushAxis = false;
        lockedPushAxis = Vector3.zero;

        ApplyActiveConstraints();
        StopHorizontalMotion();

        if (useCameraAssist && cameraAssist != null)
            cameraAssist.BeginPush();
    }

    public override void TickInteract(PlayerInteractController interactor)
    {
        currentInteractor = interactor;
    }

    public override void EndInteract(PlayerInteractController interactor)
    {
        if (interactor != null && currentInteractor != interactor)
            return;

        if (useCameraAssist && cameraAssist != null)
            cameraAssist.EndPush();

        hasLockedPushAxis = false;
        lockedPushAxis = Vector3.zero;

        currentInteractor = null;
        StopHorizontalMotion();
        ApplyIdleConstraints();
    }

    void FixedUpdate()
    {
        if (currentInteractor == null || !currentInteractor.IsHoldingInteract)
        {
            if (freezeHorizontalWhenIdle)
                StopHorizontalMotion();

            return;
        }

        if (!CanInteract(currentInteractor))
        {
            EndInteract(currentInteractor);
            return;
        }

        Vector3 inputMoveDir = currentInteractor.Player.MoveDirection;
        inputMoveDir.y = 0f;

        if (inputMoveDir.sqrMagnitude < minInputMagnitude * minInputMagnitude)
        {
            StopHorizontalMotion();
            return;
        }

        inputMoveDir.Normalize();

        if (snapToWorldAxis)
            inputMoveDir = SnapToWorldAxis(inputMoveDir);

        Vector3 toObject = GetInteractionPoint(currentInteractor) - currentInteractor.Player.transform.position;
        toObject.y = 0f;

        if (toObject.sqrMagnitude > 0.0001f)
        {
            float dot = Vector3.Dot(inputMoveDir, toObject.normalized);
            if (dot < minTowardDot)
            {
                StopHorizontalMotion();
                return;
            }
        }

        Vector3 moveDir = inputMoveDir;

        if (lockPushAxisWhileHolding)
        {
            if (!hasLockedPushAxis)
            {
                lockedPushAxis = inputMoveDir;
                hasLockedPushAxis = true;

                if (useCameraAssist && cameraAssist != null)
                    cameraAssist.LockForPushAxis(lockedPushAxis);
            }
            else
            {
                // 반대 방향 입력이면 밀지 않음.
                // 카메라 회전으로 입력 방향이 조금 달라지는 정도는 허용.
                float sameDirection = Vector3.Dot(inputMoveDir, lockedPushAxis);

                if (sameDirection < -0.25f)
                {
                    StopHorizontalMotion();
                    return;
                }

                moveDir = lockedPushAxis;
            }
        }
        else
        {
            if (useCameraAssist && cameraAssist != null)
                cameraAssist.LockForPushAxis(moveDir);
        }

        Vector3 delta = moveDir * (pushSpeed * Time.fixedDeltaTime);
        Vector3 nextPos = rb.position + new Vector3(delta.x, 0f, delta.z);
        rb.MovePosition(nextPos);
    }

    Vector3 SnapToWorldAxis(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.z))
            return new Vector3(Mathf.Sign(dir.x), 0f, 0f);

        return new Vector3(0f, 0f, Mathf.Sign(dir.z));
    }

    void ApplyIdleConstraints()
    {
        rb.constraints = freezeHorizontalWhenIdle
            ? FreezeRotAll | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ
            : FreezeRotAll;
    }

    void ApplyActiveConstraints()
    {
        rb.constraints = FreezeRotAll;
    }

    void StopHorizontalMotion()
    {
        Vector3 v = rb.linearVelocity;
        v.x = 0f;
        v.z = 0f;
        rb.linearVelocity = v;
    }
}