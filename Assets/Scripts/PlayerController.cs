using UnityEngine;
using UnityEngine.UI;

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

    [Header("Fall Respawn")]
    public Transform respawnPoint;
    public float fallRespawnDistance = 8f;
    public bool updateRespawnFromGround = true;
    public float respawnRecordMinInterval = 0.1f;

    Vector3 lastSafeRespawnPos;
    float lastRespawnRecordTime = -999f;

    Rigidbody rb;
    ShadowInteractController shadowCtrl;
    PlayerHealth health;

    Vector2 moveInput;
    Vector3 moveDir;
    bool isGrounded;

    bool inputLocked;
    bool billboardLocked;

    bool isShadowTransitionPlaying;
    bool prevInShadow;

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
        lastSafeRespawnPos = respawnPoint != null ? respawnPoint.position : transform.position;
        prevInShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;
    }

    void Update()
    {
        if (health != null && health.isDead)
        {
            moveInput = Vector2.zero;
            moveDir = Vector3.zero;
            UpdateAnim(false, false);
            return;
        }

        if (CheckFallRespawn())
            return;

        bool inShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;

        if (inShadow != prevInShadow)
        {
            BeginShadowTransition(inShadow);
            prevInShadow = inShadow;
            return;
        }

        if (isShadowTransitionPlaying)
        {
            moveInput = Vector2.zero;
            moveDir = Vector3.zero;
            return;
        }

        if (inputLocked)
        {
            moveInput = Vector2.zero;
            moveDir = Vector3.zero;
            UpdateAnim(false, false);
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        bool anchored = inShadow && shadowCtrl != null && shadowCtrl.HasSurfaceAnchor;

        moveDir = anchored
            ? BuildAnchoredMoveDirection(moveInput, shadowCtrl.AnchorNormal.normalized)
            : BuildGroundMoveDirection(moveInput);

        bool isMoving = moveDir.sqrMagnitude > 0.0001f;
        UpdateAnim(isMoving, inShadow);
        UpdateFlip(moveDir);
    }

    void FixedUpdate()
    {
        if (health != null && health.isDead)
        {
            rb.linearVelocity = Vector3.zero;
            shadowCtrl?.ClearMovingShadowHost();
            return;
        }

        if (isShadowTransitionPlaying)
        {
            Vector3 v = rb.linearVelocity;
            v.x = 0f;
            v.z = 0f;
            rb.linearVelocity = v;

            shadowCtrl?.ClearMovingShadowHost();
            return;
        }

        UpdateGroundState();
        RecordSafeRespawnPoint();

        bool inShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;
        bool anchored = inShadow && shadowCtrl != null && shadowCtrl.HasSurfaceAnchor;
        float speed = moveSpeed * (shadowCtrl != null ? shadowCtrl.SpeedMultiplier : 1f);

        if (anchored)
        {
            rb.linearVelocity = moveDir * speed;
            shadowCtrl.SnapToAnchoredSurface(rb);
            shadowCtrl.ClearMovingShadowHost();
            return;
        }

        Vector3 horizontalVelocity = moveDir * speed;
        float y = rb.linearVelocity.y;

        if (inShadow && shadowCtrl != null)
        {
            float margin = shadowCtrl.GetActiveMargin(0.9f);

            bool hasHostOrGrace = shadowCtrl.RefreshMovingShadowHost(rb.position, margin);
            Vector3 hostDelta = shadowCtrl.ConsumeMovingShadowHostDelta();
            hostDelta.y = 0f;

            Vector3 basePos = rb.position + hostDelta;
            bool baseSafe = shadowCtrl.IsShadowSafeAtWorldPos(basePos, margin);

            if (!baseSafe && !hasHostOrGrace)
            {
                shadowCtrl.ForceExitShadowMode();
                rb.linearVelocity = Vector3.zero;
                return;
            }

            Vector3 inputDelta = Vector3.zero;

            if (baseSafe)
            {
                Vector3 desiredInputDelta = horizontalVelocity * Time.fixedDeltaTime;
                inputDelta = ClampDeltaToShadow(basePos, desiredInputDelta, margin);
            }

            Vector3 finalDelta = hostDelta + inputDelta;
            Vector3 predictedPos = rb.position + finalDelta;

            if (!shadowCtrl.IsShadowSafeAtWorldPos(predictedPos, margin) && !hasHostOrGrace)
            {
                shadowCtrl.ForceExitShadowMode();
                rb.linearVelocity = Vector3.zero;
                return;
            }

            rb.linearVelocity = new Vector3(
                finalDelta.x / Time.fixedDeltaTime,
                0f,
                finalDelta.z / Time.fixedDeltaTime);
        }
        else
        {
            shadowCtrl?.ClearMovingShadowHost();

            if (isGrounded && y < 0f)
                y = groundedStickVelocity;

            y += gravity * Time.fixedDeltaTime;
            y = Mathf.Max(y, maxFallSpeed);

            rb.linearVelocity = new Vector3(horizontalVelocity.x, y, horizontalVelocity.z);
        }
    }

    void LateUpdate()
    {
        if (billboardLocked) return;

        if (visualBillboard != null && cam != null)
        {
            Vector3 toCam = cam.position - visualBillboard.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.0001f)
                visualBillboard.forward = toCam.normalized;
        }
    }

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

    void UpdateAnim(bool isMoving, bool inShadow)
    {
        if (anim == null) return;
        if (isShadowTransitionPlaying) return;
        anim.SetBool("isRun", !inShadow && isMoving);
        anim.SetBool("Idle", !inShadow && !isMoving);
        anim.SetBool("isShadowWalk", inShadow && isMoving);
        anim.SetBool("ShadowIdle", inShadow && !isMoving);
    }


    public void BeginShadowTransition(bool nowInShadow)
    {
        if (anim == null) return;

        isShadowTransitionPlaying = true;
        SetInputLocked(true);

        moveInput = Vector2.zero;
        moveDir = Vector3.zero;

        if (rb != null)
        {
            Vector3 v = rb.linearVelocity;
            v.x = 0f;
            v.z = 0f;
            rb.linearVelocity = v;
        }

        // 이동 관련 애니메이션 끄기
        anim.SetBool("isRun", false);
        anim.SetBool("isShadowWalk", false);

        // 현재 Animator 조건 유지
        if (nowInShadow)
        {
            // 그림자 모드 진입 애니메이션
            anim.SetBool("Idle", false);
            anim.SetBool("ShadowIdle", true);
        }
        else
        {
            // 그림자 모드 해제 애니메이션
            anim.SetBool("ShadowIdle", false);
            anim.SetBool("Idle", true);
        }
    }

    public void EndShadowTransition()
    {
        isShadowTransitionPlaying = false;
        SetInputLocked(false);
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

        if (shadowCtrl.IsShadowSafeAtWorldPos(startPos + desiredDelta, margin))
            return desiredDelta;

        Vector3 clamped = BinarySearchSafeDelta(startPos, desiredDelta, margin);
        if (clamped.sqrMagnitude > 0.0000001f)
            return clamped;

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

        float safeT = Mathf.Max(0f, lo - 0.02f);
        return desiredDelta * safeT;
    }

    bool CheckFallRespawn()
    {
        float minY = lastSafeRespawnPos.y - Mathf.Abs(fallRespawnDistance);
        if (transform.position.y > minY)
            return false;

        RespawnToSafePoint();
        return true;
    }

    void RecordSafeRespawnPoint()
    {
        if (!updateRespawnFromGround)
            return;

        if (!isGrounded)
            return;

        if (Time.time - lastRespawnRecordTime < respawnRecordMinInterval)
            return;

        // 너무 빠르게 떨어지거나 점프 중인 찰나는 저장하지 않도록 약하게 제한
        if (rb != null && Mathf.Abs(rb.linearVelocity.y) > 1f)
            return;

        lastRespawnRecordTime = Time.time;

        if (respawnPoint != null)
        {
            lastSafeRespawnPos = respawnPoint.position;
            return;
        }

        lastSafeRespawnPos = transform.position;
    }

    void RespawnToSafePoint()
    {
        Vector3 targetPos = respawnPoint != null ? respawnPoint.position : lastSafeRespawnPos;

        if (shadowCtrl != null)
        {
            shadowCtrl.ForceExitShadowMode();
            shadowCtrl.ClearSurfaceAnchor();

            // 이전에 제가 드린 moving shadow host 버전을 쓰는 경우만 필요
            // 현재 ShadowInteractController에 이 메서드가 없으면 이 줄은 빼세요.
            // shadowCtrl.ClearMovingShadowHost();
        }

        moveInput = Vector2.zero;
        moveDir = Vector3.zero;
        UpdateAnim(false, false);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = targetPos;
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (!locked) return;

        moveInput = Vector2.zero;
        moveDir = Vector3.zero;
        UpdateAnim(false, false);
        shadowCtrl?.ClearMovingShadowHost();

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