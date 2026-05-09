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

    [Header("Push Axis Rule")]
    public bool decidePushAxisFromTouchedFace = true;

    [Tooltip("카메라가 밀기 각도로 바뀐 뒤에는 입력 방향이 변할 수 있으므로, 반대 입력일 때만 멈추게 합니다.")]
    [Range(-1f, 0f)] public float oppositeInputStopDot = -0.65f;

    [Header("Face Push Filter")]
    public float faceAxisMargin = 0.12f;

    [Header("Toggle Push Mode")]
    [Tooltip("옮기기 모드에 들어간 직후, 카메라가 먼저 정렬되도록 이동 입력을 잠깐 무시합니다.")]
    public float beginPushInputDelay = 0.12f;

    [Header("Stable Push Face")]
    [Tooltip("면의 끝부분에 있어도 같은 면으로 판정하기 위한 여유 거리입니다. 플레이어 반지름보다 약간 크게 두세요.")]
    public float faceSideExtension = 0.5f;

    [Tooltip("옮기기 모드 시작 시 정한 면/축을 끝날 때까지 유지합니다.")]
    public bool lockFaceAxisOnBegin = true;

    bool hasModePushAxis;
    Vector3 modePushAxis;

    float pushModeStartTime;

    [Range(0f, 1f)]
    public float minInputIntoFaceDot = 0.35f;

    public bool allowCornerInputDisambiguation = false;

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

        hasModePushAxis = false;
        modePushAxis = Vector3.zero;

        pushModeStartTime = Time.time;

        ApplyActiveConstraints();
        StopHorizontalMotion();

        if (TryGetStableFacePushAxis(interactor.Player.transform.position, out Vector3 faceAxis))
        {
            modePushAxis = faceAxis;
            hasModePushAxis = true;

            // 시작 시점부터 이 면의 축으로 고정한다.
            if (lockFaceAxisOnBegin)
            {
                lockedPushAxis = modePushAxis;
                hasLockedPushAxis = true;
            }

            // 플레이어가 물체 쪽으로 손을 뻗도록 flip 방향을 고정한다.
            interactor.Player.SetPushFacingDirection(modePushAxis);
        }

        if (useCameraAssist && cameraAssist != null)
        {
            cameraAssist.BeginPush();

            if (hasModePushAxis)
                cameraAssist.LockForPushAxis(modePushAxis);
        }
    }

    public override void TickInteract(PlayerInteractController interactor)
    {
        currentInteractor = interactor;
    }

    public override void EndInteract(PlayerInteractController interactor)
    {
        if (interactor != null && currentInteractor != interactor)
            return;

        if (currentInteractor != null && currentInteractor.Player != null)
            currentInteractor.Player.ClearPushFacingDirection();

        if (useCameraAssist && cameraAssist != null)
            cameraAssist.EndPush();

        hasLockedPushAxis = false;
        lockedPushAxis = Vector3.zero;

        hasModePushAxis = false;
        modePushAxis = Vector3.zero;

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

        if (!hasModePushAxis)
        {
            StopHorizontalMotion();
            return;
        }

        if (Time.time - pushModeStartTime < beginPushInputDelay)
        {
            StopHorizontalMotion();
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

        Vector3 moveDir = modePushAxis;

        // 입력이 물체를 미는 방향일 때만 움직인다.
        float inputDot = Vector3.Dot(inputMoveDir, moveDir);

        if (inputDot < minInputIntoFaceDot)
        {
            StopHorizontalMotion();
            return;
        }

        lockedPushAxis = moveDir;
        hasLockedPushAxis = true;

        if (useCameraAssist && cameraAssist != null)
            cameraAssist.LockForPushAxis(lockedPushAxis);

        currentInteractor.Player.SetPushFacingDirection(lockedPushAxis);

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

    bool TryGetStableFacePushAxis(Vector3 playerPosition, out Vector3 axis)
    {
        axis = Vector3.zero;

        if (mainCollider == null)
            return false;

        Bounds b = mainCollider.bounds;

        Vector3 rel = playerPosition - b.center;
        rel.y = 0f;

        if (rel.sqrMagnitude <= 0.0001f)
            return false;

        float absX = Mathf.Abs(rel.x);
        float absZ = Mathf.Abs(rel.z);

        float extentX = Mathf.Max(0.0001f, b.extents.x);
        float extentZ = Mathf.Max(0.0001f, b.extents.z);

        // 표면에서 얼마나 바깥쪽에 있는지
        float outsideX = absX - extentX;
        float outsideZ = absZ - extentZ;

        // 면의 끝부분에 있어도 같은 면으로 잡기 위한 확장 범위
        bool withinXFaceBand = absX <= extentX + faceSideExtension;
        bool withinZFaceBand = absZ <= extentZ + faceSideExtension;

        bool nearOrOutsideXFace = outsideX > -faceSideExtension * 0.35f;
        bool nearOrOutsideZFace = outsideZ > -faceSideExtension * 0.35f;

        // 앞/뒤 면 쪽에 있고, 좌우로 조금 삐져나간 정도라면 Z면으로 유지한다.
        if (withinXFaceBand && nearOrOutsideZFace && (!nearOrOutsideXFace || outsideZ >= outsideX))
        {
            axis = new Vector3(0f, 0f, rel.z >= 0f ? -1f : 1f);
            return true;
        }

        // 좌/우 면 쪽에 있고, 위아래로 조금 삐져나간 정도라면 X면으로 유지한다.
        if (withinZFaceBand && nearOrOutsideXFace)
        {
            axis = new Vector3(rel.x >= 0f ? -1f : 1f, 0f, 0f);
            return true;
        }

        // fallback: 중심 기준이 아니라 extents로 정규화해서 더 바깥쪽인 축 선택
        float normX = absX / extentX;
        float normZ = absZ / extentZ;

        if (normX >= normZ)
            axis = new Vector3(rel.x >= 0f ? -1f : 1f, 0f, 0f);
        else
            axis = new Vector3(0f, 0f, rel.z >= 0f ? -1f : 1f);

        return axis.sqrMagnitude > 0.0001f;
    }
}