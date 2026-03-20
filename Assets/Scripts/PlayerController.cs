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

    public bool IsGrounded => isGrounded;
    public Vector3 MoveDirection => moveDir;

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
            // 벽과 천장에서는 표면 기준으로만 이동한다.
            rb.linearVelocity = moveDir * speed;
            shadowCtrl.SnapToAnchoredSurface(rb);
        }
        else
        {
            Vector3 horizontalVelocity = moveDir * speed;
            float y = rb.linearVelocity.y;

            if (inShadow)
            {
                y = 0f;
            }
            else
            {
                if (isGrounded && y < 0f)
                    y = groundedStickVelocity;

                y += gravity * Time.fixedDeltaTime;
                y = Mathf.Max(y, maxFallSpeed);
            }

            rb.linearVelocity = new Vector3(horizontalVelocity.x, y, horizontalVelocity.z);
        }

        // 그림자 모드에서는 항상 안전한 그림자 안에 있는지 확인한다.
        if (inShadow && shadowCtrl != null)
        {
            float margin = shadowCtrl.GetActiveMargin(0.9f);
            if (!shadowCtrl.IsShadowSafeAtWorldPos(rb.position, margin))
                shadowCtrl.ForceExitShadowMode();
        }
    }

    void LateUpdate()
    {
        // 캐릭터가 카메라를 계속 향하도록 맞춘다.
        if (visualBillboard != null && cam != null)
        {
            Vector3 toCam = cam.position - visualBillboard.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.0001f)
                visualBillboard.forward = toCam.normalized;
        }
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

    void SetupRigidbody()
    {
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }
}
