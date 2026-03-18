using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
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
    public bool artFacesRight = true;

    Rigidbody rb;
    CapsuleCollider bodyCol;
    ShadowInteractController shadowCtrl;
    PlayerHealth health;

    Vector2 moveInput;
    Vector3 moveDir;
    bool isGrounded;

    public float BodyRadius => bodyCol != null ? bodyCol.radius : 0.35f;
    public bool IsGrounded => isGrounded;
    public Vector3 MoveDirection => moveDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bodyCol = GetComponent<CapsuleCollider>();
        shadowCtrl = GetComponent<ShadowInteractController>();
        health = GetComponent<PlayerHealth>();

        if (!cam && Camera.main) cam = Camera.main.transform;
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!flipRoot) flipRoot = visualBillboard ? visualBillboard : transform;

        if (groundMask.value == 0 && shadowCtrl != null)
            groundMask = shadowCtrl.groundMask;

        SetupRigidbody();
        ApplyFlip(true);
    }

    void Update()
    {
        if (health != null && health.isDead)
        {
            moveInput = Vector2.zero;
            UpdateAnim(false);
            UpdateFlip(Vector3.zero);
            return;
        }

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveDir = BuildMoveDirection(moveInput);

        bool isRun = moveDir.sqrMagnitude > 0.0001f;
        UpdateAnim(isRun);
        UpdateFlip(moveDir);

        if (Input.GetKeyDown(KeyCode.K) && health != null)
            health.TakeDamage(1);
    }

    void FixedUpdate()
    {
        if (health != null && health.isDead)
        {
            Vector3 deadVel = rb.linearVelocity;
            deadVel.x = 0f;
            deadVel.z = 0f;
            rb.linearVelocity = deadVel;
            return;
        }

        UpdateGroundState();

        bool inShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;
        bool anchored = inShadow && shadowCtrl.HasSurfaceAnchor;

        float speed = moveSpeed * (shadowCtrl != null ? shadowCtrl.SpeedMultiplier : 1f);
        Vector3 horizontalVelocity = moveDir * speed;

        if (anchored)
        {
            rb.linearVelocity = horizontalVelocity;
            shadowCtrl.SnapToAnchoredSurface(rb);
        }
        else
        {
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

        if (inShadow && shadowCtrl != null)
        {
            float margin = BodyRadius * 0.9f;
            if (!shadowCtrl.IsShadowSafeAtWorldPos(rb.position, margin))
                shadowCtrl.ForceExitShadowMode();
        }
    }

    void LateUpdate()
    {
        if (!visualBillboard || !cam) return;

        Vector3 toCam = cam.position - visualBillboard.position;
        toCam.y = 0f;

        if (toCam.sqrMagnitude > 0.0001f)
            visualBillboard.forward = toCam.normalized;
    }

    Vector3 BuildMoveDirection(Vector2 input)
    {
        bool inShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;
        bool anchored = inShadow && shadowCtrl.HasSurfaceAnchor;

        if (anchored)
        {
            Vector3 n = shadowCtrl.AnchorNormal.normalized;

            Vector3 rightOnSurface = cam ? cam.right : Vector3.right;
            rightOnSurface = Vector3.ProjectOnPlane(rightOnSurface, n);
            if (rightOnSurface.sqrMagnitude < 0.0001f)
                rightOnSurface = Vector3.Cross(Vector3.up, n);
            rightOnSurface.Normalize();

            Vector3 upOnSurface = Vector3.ProjectOnPlane(Vector3.up, n);
            if (upOnSurface.sqrMagnitude < 0.0001f)
            {
                upOnSurface = cam ? cam.forward : Vector3.forward;
                upOnSurface = Vector3.ProjectOnPlane(upOnSurface, n);
            }
            upOnSurface.Normalize();

            Vector3 dir = rightOnSurface * input.x + upOnSurface * input.y;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }
        else
        {
            Vector3 camF = cam ? cam.forward : Vector3.forward;
            Vector3 camR = cam ? cam.right : Vector3.right;
            camF.y = 0f;
            camR.y = 0f;
            camF.Normalize();
            camR.Normalize();

            Vector3 dir = camR * input.x + camF * input.y;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }
    }

    void UpdateGroundState()
    {
        if (!bodyCol)
        {
            isGrounded = false;
            return;
        }

        Vector3 center = transform.TransformPoint(bodyCol.center);
        float halfBody = Mathf.Max(bodyCol.height * 0.5f - bodyCol.radius, 0f);
        Vector3 sphereOrigin = center + Vector3.down * halfBody + Vector3.up * 0.05f;
        float sphereRadius = Mathf.Max(0.01f, bodyCol.radius * 0.95f);

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
        if (dir.sqrMagnitude < 0.0001f || !cam) return;

        Vector3 camRight = cam.right;
        camRight.y = 0f;
        camRight.Normalize();

        float lr = Vector3.Dot(dir, camRight);
        if (Mathf.Abs(lr) <= 0.001f) return;

        ApplyFlip(lr > 0f);
    }

    void ApplyFlip(bool faceRight)
    {
        if (!flipRoot) return;

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
