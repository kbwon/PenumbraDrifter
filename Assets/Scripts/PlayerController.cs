using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 4.5f;
    public float gravity = -25f;
    public float maxFallSpeed = -35f;
    public float groundedStickVelocity = -2f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask;

    [Header("View")]
    public Transform cam;
    public Transform visualBillboard;
    public Transform flipRoot;
    public Animator anim;

    [Header("Facing")]
    public bool artFacesRight = true;

    Rigidbody rb;
    ShadowInteractController shadowCtrl;
    PlayerHealth health;

    Vector2 moveInput;
    Vector3 moveDir;
    bool isGrounded;

    // 연출 중에는 플레이어 입력을 막는다.
    bool inputLocked;

    // 인트로 중에는 입력 잠금과 별개로 빌보드 회전을 따로 제어한다.
    bool billboardLocked;

    public bool IsGrounded => isGrounded;
    public Vector3 MoveDirection => moveDir;
    public bool InputLocked => inputLocked;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        shadowCtrl = GetComponent<ShadowInteractController>();
        health = GetComponent<PlayerHealth>();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterPlayer(this);

        if (cam == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.MainCameraTransform != null)
                cam = GameManager.Instance.MainCameraTransform;
            else if (Camera.main != null)
                cam = Camera.main.transform;
        }

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (flipRoot == null)
            flipRoot = visualBillboard != null ? visualBillboard : transform;

        if (groundMask.value == 0 && shadowCtrl != null)
            groundMask = shadowCtrl.groundMask;

        SetupRigidbody();
        ApplyFlip(true);
    }

    void Update()
    {
        // 사망 시 입력과 이동 방향을 멈춘다.
        if (health != null && health.isDead)
        {
            moveInput = Vector2.zero;
            moveDir = Vector3.zero;
            UpdateAnim(false);
            return;
        }

        // 연출 중에는 입력을 막고 제자리 상태를 유지한다.
        if (inputLocked)
        {
            moveInput = Vector2.zero;
            moveDir = Vector3.zero;
            UpdateAnim(false);
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        bool inShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;
        bool anchored = inShadow && shadowCtrl != null && shadowCtrl.HasSurfaceAnchor;

        moveDir = anchored
            ? BuildAnchoredMoveDirection(moveInput, shadowCtrl.AnchorNormal.normalized)
            : BuildGroundMoveDirection(moveInput);

        bool isRun = moveDir.sqrMagnitude > 0.0001f;
        UpdateAnim(isRun);
        UpdateFlip(moveDir);
    }

    void FixedUpdate()
    {
        if (health != null && health.isDead)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        UpdateGroundState();

        bool inShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;
        bool anchored = inShadow && shadowCtrl != null && shadowCtrl.HasSurfaceAnchor;
        float speed = moveSpeed * (shadowCtrl != null ? shadowCtrl.SpeedMultiplier : 1f);

        if (anchored)
        {
            rb.linearVelocity = moveDir * speed;
            shadowCtrl.SnapToAnchoredSurface(rb);
        }
        else
        {
            Vector3 horizontalVelocity = moveDir * speed;
            float y = rb.linearVelocity.y;

            if (inShadow && shadowCtrl != null)
            {
                y = 0f;

                float margin = shadowCtrl.GetActiveMargin(0.9f);
                float searchRadius =
                    shadowCtrl.GetActiveRadiusWorld() * 1.5f +
                    speed * Time.fixedDeltaTime * 2f;

                // 1) 그림자 자체가 움직여서 현재 위치가 unsafe가 된 경우
                //    근처의 새로운 safe 위치로 먼저 붙여 준다.
                if (!shadowCtrl.IsShadowSafeAtWorldPos(rb.position, margin))
                {
                    if (TrySnapToNearbyShadow(rb.position, margin, searchRadius, out Vector3 snappedPos))
                    {
                        rb.position = snappedPos;
                    }
                    else
                    {
                        shadowCtrl.ForceExitShadowMode();
                        rb.linearVelocity = Vector3.zero;
                        return;
                    }
                }

                // 2) 입력으로 경계 밖으로 나가려는 경우는 벽처럼 막는다.
                Vector3 desiredDelta = horizontalVelocity * Time.fixedDeltaTime;
                Vector3 resolvedDelta = ClampDeltaToShadow(rb.position, desiredDelta, margin);

                rb.linearVelocity = resolvedDelta / Time.fixedDeltaTime;
            }
            else
            {
                if (isGrounded && y < 0f)
                    y = groundedStickVelocity;

                y += gravity * Time.fixedDeltaTime;
                y = Mathf.Max(y, maxFallSpeed);

                rb.linearVelocity = new Vector3(horizontalVelocity.x, y, horizontalVelocity.z);
            }
        }
    }

    void LateUpdate()
    {
        // 시작 연출 중에는 필요할 때만 빌보드 방향을 갱신한다.
        if (billboardLocked) return;

        // 캐릭터가 카메라를 계속 향하도록 맞춘다.
        if (visualBillboard != null && cam != null)
        {
            Vector3 toCam = cam.position - visualBillboard.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.0001f)
                visualBillboard.forward = toCam.normalized;
        }
    }

    // 인트로 구간에 따라 빌보드 회전을 잠그거나 다시 허용한다.
    public void SetBillboardLocked(bool locked)
    {
        billboardLocked = locked;
    }

    Vector3 BuildGroundMoveDirection(Vector2 input)
    {
        Vector3 camF = cam ? cam.forward : Vector3.forward;
        Vector3 camR = cam ? cam.right : Vector3.right;
        camF.y = 0f;
        camR.y = 0f;
        camF.Normalize();
        camR.Normalize();

        Vector3 dir = camR * input.x + camF * input.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        return dir;
    }

    Vector3 BuildAnchoredMoveDirection(Vector2 input, Vector3 surfaceNormal)
    {
        if (input.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        // 벽 이동은 카메라 오른쪽과 월드 위쪽을 표면에 투영해 사용한다.
        Vector3 rightOnSurface = cam ? cam.right : Vector3.right;
        rightOnSurface = rightOnSurface - Vector3.Dot(rightOnSurface, surfaceNormal) * surfaceNormal;
        if (rightOnSurface.sqrMagnitude < 0.0001f)
            rightOnSurface = Vector3.Cross(Vector3.up, surfaceNormal);
        rightOnSurface.Normalize();

        Vector3 upOnSurface = Vector3.up - Vector3.Dot(Vector3.up, surfaceNormal) * surfaceNormal;
        if (upOnSurface.sqrMagnitude < 0.0001f)
        {
            upOnSurface = cam ? cam.forward : Vector3.forward;
            upOnSurface = upOnSurface - Vector3.Dot(upOnSurface, surfaceNormal) * surfaceNormal;
        }
        upOnSurface.Normalize();

        Vector3 dir = rightOnSurface * input.x + upOnSurface * input.y;
        if (dir.sqrMagnitude > 1f) dir.Normalize();
        return dir;
    }

    void UpdateGroundState()
    {
        if (shadowCtrl == null)
        {
            isGrounded = false;
            return;
        }

        float radius = shadowCtrl.GetActiveRadiusWorld();
        float halfH = shadowCtrl.GetActiveHeightWorld() * 0.5f;
        Vector3 centerWorld = shadowCtrl.GetActiveCenterWorld();

        float halfBody = Mathf.Max(halfH - radius, 0f);
        Vector3 sphereOrigin = centerWorld + Vector3.down * halfBody + Vector3.up * 0.05f;
        float sphereRadius = Mathf.Max(0.01f, radius * 0.95f);

        isGrounded = Physics.SphereCast(
            sphereOrigin,
            sphereRadius,
            Vector3.down,
            out _,
            groundCheckDistance + 0.05f,
            groundMask,
            QueryTriggerInteraction.Ignore);
    }

    void UpdateAnim(bool isRun)
    {
        if (anim != null)
            anim.SetBool("isRun", isRun);
    }

    void UpdateFlip(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector3 camRight = cam ? cam.right : Vector3.right;
        camRight.y = 0f;
        camRight.Normalize();

        float lr = Vector3.Dot(dir, camRight);
        if (Mathf.Abs(lr) > 0.001f)
            ApplyFlip(lr > 0f);
    }

    void ApplyFlip(bool faceRight)
    {
        if (flipRoot == null) return;

        if (!artFacesRight)
            faceRight = !faceRight;

        Vector3 scale = flipRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * (faceRight ? 1f : -1f);
        flipRoot.localScale = scale;
    }

    Vector3 ClampDeltaToShadow(Vector3 startPos, Vector3 desiredDelta, float margin)
    {
        desiredDelta.y = 0f;

        if (desiredDelta.sqrMagnitude <= 0.0000001f)
            return Vector3.zero;

        // 목표 위치가 안전하면 그대로 이동
        if (shadowCtrl.IsShadowSafeAtWorldPos(startPos + desiredDelta, margin))
            return desiredDelta;

        // 먼저 전체 방향으로 최대한 갈 수 있는 지점 찾기
        Vector3 clamped = BinarySearchSafeDelta(startPos, desiredDelta, margin);
        if (clamped.sqrMagnitude > 0.0000001f)
            return clamped;

        // 전체 이동이 안 되면 축 분리로 약간의 슬라이드 허용
        Vector3 xOnly = BinarySearchSafeDelta(startPos, new Vector3(desiredDelta.x, 0f, 0f), margin);
        Vector3 zOnly = BinarySearchSafeDelta(startPos, new Vector3(0f, 0f, desiredDelta.z), margin);

        return xOnly.sqrMagnitude >= zOnly.sqrMagnitude ? xOnly : zOnly;
    }

    Vector3 BinarySearchSafeDelta(Vector3 startPos, Vector3 desiredDelta, float margin)
    {
        float lo = 0f;
        float hi = 1f;

        for (int i = 0; i < 10; i++)
        {
            float mid = (lo + hi) * 0.5f;
            Vector3 testPos = startPos + desiredDelta * mid;

            if (shadowCtrl.IsShadowSafeAtWorldPos(testPos, margin))
                lo = mid;
            else
                hi = mid;
        }

        // 경계에 너무 딱 붙지 않도록 아주 조금 안쪽으로
        float safeT = Mathf.Max(0f, lo - 0.02f);
        return desiredDelta * safeT;
    }

    bool TrySnapToNearbyShadow(Vector3 startPos, float margin, float searchRadius, out Vector3 snappedPos)
    {
        snappedPos = startPos;

        if (shadowCtrl.IsShadowSafeAtWorldPos(startPos, margin))
            return true;

        float bestDistSqr = float.MaxValue;
        bool found = false;

        const int rings = 3;
        const int samplesPerRing = 16;

        for (int ring = 1; ring <= rings; ring++)
        {
            float r = searchRadius * ring / rings;

            for (int i = 0; i < samplesPerRing; i++)
            {
                float angle = (Mathf.PI * 2f * i) / samplesPerRing;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * r;
                Vector3 candidate = startPos + offset;

                if (!shadowCtrl.IsShadowSafeAtWorldPos(candidate, margin))
                    continue;

                float d = (candidate - startPos).sqrMagnitude;
                if (d < bestDistSqr)
                {
                    bestDistSqr = d;
                    snappedPos = candidate;
                    found = true;
                }
            }
        }

        return found;
    }

    // StageIntroDirector가 시작 연출 동안 입력을 잠근다.
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (!locked) return;

        moveInput = Vector2.zero;
        moveDir = Vector3.zero;
        UpdateAnim(false);

        if (rb != null)
        {
            Vector3 v = rb.linearVelocity;
            v.x = 0f;
            v.z = 0f;
            rb.linearVelocity = v;
        }
    }

    void SetupRigidbody()
    {
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }
}