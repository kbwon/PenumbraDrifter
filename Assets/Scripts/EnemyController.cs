using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class EnemyController : MonoBehaviour
{
    public EnemyConfig config;
    public EnemyVision vision;
    public EnemyAlertUI alertUI;

    [Header("Target")]
    public string playerTag = "Player";

    [Header("Physics")]
    public Rigidbody rb;
    public CapsuleCollider bodyCollider;
    public LayerMask groundMask;
    public bool useCustomGravity = true;
    public float gravity = -25f;
    public float maxFallSpeed = -35f;
    public float groundedStickVelocity = -2f;
    public float groundCheckDistance = 0.2f;

    [Header("View")]
    public Transform cam;
    public Transform visualBillboard;
    public Transform flipRoot;
    public Animator anim;
    public bool artFacesRight = true;

    [Header("Billboard")]
    public bool useVisualBillboard = true;

    // true 추천: 적 위치와 상관없이 모든 적이 카메라 시선 방향에 맞춰 같은 각도로 정렬된다.
    public bool useCameraViewDirection = true;

    // StageIntro 같은 연출 중 적 스프라이트 회전을 멈추기 위한 잠금값
    public bool billboardLocked = false;

    // 0이면 즉시 회전, 0보다 크면 부드럽게 회전
    public float billboardSmoothSpeed = 0f;

    [Header("Animation")]
    public string walkBoolName = "isWalk";
    public string attackTriggerName = "attack";

    protected enum State
    {
        Idle,
        VisualAlert,
        SoundAlert,
        Chase,
        LostWait,
        Return
    }

    protected State state = State.Idle;
    protected Transform player;
    protected ShadowInteractController playerShadow;
    protected PlayerHealth playerHealth;

    protected Vector3 homePos;
    protected Vector3 desiredVelocity;
    protected Vector3 lastMoveDir;
    protected bool isGrounded;

    protected float visualAlert01;
    protected float visualLostGraceTimer;
    protected float soundAlert01;
    protected Vector3 lastHeardPos;
    protected float soundWaitTimer;
    protected bool reachedSoundPoint;
    protected bool decayingSoundAlert;
    protected Transform currentNoiseSource;
    protected float lastNoiseAcceptTime = -999f;

    protected float lostWaitTimer;
    protected float lastDamageTime = -999f;
    protected float notSeenTimer;

    protected bool isAttacking;
    protected float attackEndTime;
    protected float attackHitTime;
    protected PlayerHealth attackTarget;
    protected bool attackDamageDone;

    public string CurrentStateName => state.ToString();
    public float VisualAlert01 => visualAlert01;
    public float SoundAlert01 => soundAlert01;

    protected virtual void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!bodyCollider) bodyCollider = GetComponent<CapsuleCollider>();

        if (!vision) vision = GetComponent<EnemyVision>();
        if (!alertUI) alertUI = GetComponentInChildren<EnemyAlertUI>(true);
        if (!anim) anim = GetComponentInChildren<Animator>();

        if (!flipRoot)
            flipRoot = visualBillboard != null ? visualBillboard : transform;

        if (!cam)
        {
            if (GameManager.Instance != null && GameManager.Instance.MainCameraTransform != null)
                cam = GameManager.Instance.MainCameraTransform;
            else if (Camera.main != null)
                cam = Camera.main.transform;
        }

        homePos = transform.position;
        SetupRigidbody();
        ApplyFlip(true);
    }

    protected virtual void OnEnable()
    {
        NoiseSystem.OnNoise += HandleNoise;
    }

    protected virtual void OnDisable()
    {
        NoiseSystem.OnNoise -= HandleNoise;
    }

    protected virtual void Start()
    {
        if (vision)
            vision.config = config;

        if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
            player = GameManager.Instance.PlayerTransform;
        else
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject)
                player = playerObject.transform;
        }

        if (player != null)
        {
            playerShadow = player.GetComponent<ShadowInteractController>();
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        if (vision && player != null)
            vision.SetTarget(player);
    }

    protected virtual void Update()
    {
        if (!HasRequiredRefs()) return;

        desiredVelocity = Vector3.zero;

        vision.RefreshNow();

        if (isAttacking)
        {
            UpdateAttackLock();
            UpdateAnim();
            UpdateFlip(lastMoveDir);
            UpdateAlertUI();
            return;
        }

        switch (state)
        {
            case State.Idle:
                UpdateIdleState();
                break;

            case State.VisualAlert:
                UpdateVisualAlertState();
                break;

            case State.SoundAlert:
                UpdateSoundAlertState();
                break;

            case State.Chase:
                UpdateChaseState();
                break;

            case State.LostWait:
                UpdateLostWaitState();
                break;

            case State.Return:
                UpdateReturnState();
                break;
        }

        UpdateAnim();
        UpdateFlip(lastMoveDir);
        UpdateAlertUI();
    }

    protected virtual void FixedUpdate()
    {
        if (!rb) return;

        UpdateGroundState();
        ApplyMovement();
    }

    protected virtual void LateUpdate()
    {
        UpdateVisualBillboard();
    }

    protected virtual void UpdateVisualBillboard()
    {
        if (!useVisualBillboard) return;
        if (billboardLocked) return;
        if (visualBillboard == null) return;

        if (cam == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.MainCameraTransform != null)
                cam = GameManager.Instance.MainCameraTransform;
            else if (Camera.main != null)
                cam = Camera.main.transform;
        }

        if (cam == null) return;

        Vector3 targetForward;

        if (useCameraViewDirection)
        {
            // 핵심:
            // 적의 위치에서 카메라를 바라보는 것이 아니라,
            // 카메라가 바라보는 방향의 반대 방향으로 모든 적을 동일하게 정렬한다.
            targetForward = -cam.forward;
        }
        else
        {
            // 기존 방식: 각 적이 카메라 위치를 직접 바라봄
            targetForward = cam.position - visualBillboard.position;
        }

        targetForward.y = 0f;

        if (targetForward.sqrMagnitude <= 0.0001f)
            return;

        targetForward.Normalize();

        if (billboardSmoothSpeed <= 0f)
        {
            visualBillboard.forward = targetForward;
        }
        else
        {
            float k = 1f - Mathf.Exp(-billboardSmoothSpeed * Time.deltaTime);
            visualBillboard.forward = Vector3.Slerp(
                visualBillboard.forward,
                targetForward,
                k
            );
        }
    }

    public void SetBillboardLocked(bool locked)
    {
        billboardLocked = locked;
    }

    protected virtual bool HasRequiredRefs()
    {
        return config && player && vision && rb && bodyCollider;
    }

    protected virtual void UpdateIdleState()
    {
        StopMove();
        DecayVisualAlert();

        if (vision.CanSeeAttack)
        {
            EnterChaseState();
            return;
        }

        if (vision.CanSeeAlert)
        {
            EnterVisualAlertState(0.05f);
            return;
        }
    }

    protected virtual void UpdateVisualAlertState()
    {
        StopMove();

        if (vision.CanSeeAttack)
        {
            EnterChaseState();
            return;
        }

        if (vision.CanSeeAlert)
        {
            visualLostGraceTimer = 0f;

            Vector3 dir = GetFlatDirectionTo(player.position, out _);
            FaceDirection(dir);

            IncreaseVisualAlert();

            if (visualAlert01 >= 1f)
            {
                EnterChaseState();
                return;
            }

            return;
        }

        visualLostGraceTimer += Time.deltaTime;

        if (visualLostGraceTimer >= config.loseSightGrace)
            DecayVisualAlert();

        if (visualAlert01 <= 0f)
        {
            if (ShouldReturnHome())
                EnterReturnState();
            else
                EnterIdleState();
        }
    }

    protected virtual void UpdateSoundAlertState()
    {
        if (vision.CanSeeAttack)
        {
            EnterChaseState();
            return;
        }

        if (vision.CanSeeAlert)
        {
            EnterVisualAlertState(0.2f);
            return;
        }

        Vector3 moveDir = GetFlatDirectionTo(lastHeardPos, out float distance);

        if (!reachedSoundPoint)
        {
            decayingSoundAlert = false;

            soundAlert01 = Mathf.MoveTowards(
                soundAlert01,
                1f,
                Time.deltaTime / Mathf.Max(0.01f, config.soundAlertFillSeconds)
            );

            if (distance > config.soundStopDistance)
            {
                FaceDirection(moveDir);
                MoveInDirection(moveDir, config.soundMoveSpeed);
                return;
            }

            reachedSoundPoint = true;
            soundWaitTimer = config.soundInvestigateWait;
            StopMove();
            return;
        }

        StopMove();

        if (moveDir.sqrMagnitude > 0.0001f)
            FaceDirection(moveDir);

        if (!decayingSoundAlert)
        {
            soundAlert01 = Mathf.MoveTowards(
                soundAlert01,
                1f,
                Time.deltaTime / Mathf.Max(0.01f, config.soundAlertFillSeconds)
            );

            soundWaitTimer -= Time.deltaTime;

            if (soundWaitTimer <= 0f)
                decayingSoundAlert = true;

            return;
        }

        soundAlert01 = Mathf.MoveTowards(
            soundAlert01,
            0f,
            Time.deltaTime / Mathf.Max(0.01f, config.soundAlertDecaySeconds)
        );

        if (soundAlert01 <= 0f)
        {
            if (ShouldReturnHome())
                EnterReturnState();
            else
                EnterIdleState();
        }
    }

    protected virtual void UpdateChaseState()
    {
        if (vision.CanSeeAlert)
        {
            notSeenTimer = 0f;
            visualAlert01 = 1f;
            ChasePlayer();
            return;
        }

        if (IsPlayerHiddenByShadow())
        {
            EnterVisualAlertState(config.lostAlertStart01);
            return;
        }

        notSeenTimer += Time.deltaTime;
        StopMove();

        if (notSeenTimer >= config.loseChaseAfterNotSeenSeconds)
        {
            EnterVisualAlertState(config.lostAlertStart01);
            notSeenTimer = 0f;
        }
    }

    protected virtual void UpdateLostWaitState()
    {
        StopMove();

        if (vision.CanSeeAttack)
        {
            EnterChaseState();
            return;
        }

        if (vision.CanSeeAlert)
        {
            EnterVisualAlertState(config.lostAlertStart01);
            return;
        }

        lostWaitTimer -= Time.deltaTime;

        if (lostWaitTimer <= 0f)
            EnterReturnState();
    }

    protected virtual void UpdateReturnState()
    {
        if (vision.CanSeeAttack)
        {
            EnterChaseState();
            return;
        }

        if (vision.CanSeeAlert)
        {
            EnterVisualAlertState(0.05f);
            return;
        }

        ReturnHome();
    }

    protected virtual void EnterIdleState()
    {
        state = State.Idle;
        visualAlert01 = 0f;
        soundAlert01 = 0f;
        notSeenTimer = 0f;
        visualLostGraceTimer = 0f;

        reachedSoundPoint = false;
        decayingSoundAlert = false;
        currentNoiseSource = null;

        StopMove();
    }

    protected virtual void EnterVisualAlertState(float startAlert01)
    {
        state = State.VisualAlert;
        visualAlert01 = Mathf.Max(visualAlert01, startAlert01);
        visualLostGraceTimer = 0f;
        notSeenTimer = 0f;
        soundAlert01 = 0f;
        StopMove();
    }

    protected virtual void EnterChaseState()
    {
        state = State.Chase;
        visualAlert01 = 1f;
        soundAlert01 = 0f;
        notSeenTimer = 0f;
        visualLostGraceTimer = 0f;
    }

    protected virtual void EnterSoundAlertState(Vector3 heardPosition)
    {
        state = State.SoundAlert;
        lastHeardPos = heardPosition;

        soundAlert01 = 0f;
        soundWaitTimer = config.soundInvestigateWait;
        reachedSoundPoint = false;
        decayingSoundAlert = false;

        visualLostGraceTimer = 0f;
    }

    protected virtual void EnterReturnState()
    {
        state = State.Return;
        soundAlert01 = 0f;
        visualAlert01 = 0f;
        notSeenTimer = 0f;

        reachedSoundPoint = false;
        decayingSoundAlert = false;
        currentNoiseSource = null;
    }

    protected virtual void IncreaseVisualAlert()
    {
        visualAlert01 += Time.deltaTime / Mathf.Max(0.01f, config.detectTimeRequired);
        visualAlert01 = Mathf.Clamp01(visualAlert01);
    }

    protected virtual void DecayVisualAlert()
    {
        visualAlert01 -= Time.deltaTime / Mathf.Max(0.01f, config.alertDecaySeconds);
        visualAlert01 = Mathf.Clamp01(visualAlert01);
    }

    protected virtual void ChasePlayer()
    {
        Vector3 moveDir = GetFlatDirectionTo(player.position, out float distance);

        FaceDirection(moveDir);

        float attackStartRange = Mathf.Max(config.stopDistance, config.attackHitRange);

        if (distance <= attackStartRange)
        {
            StopMove();
            ZeroHorizontalVelocity();

            if (playerHealth != null && !playerHealth.isDead)
                StartAttack(playerHealth);

            return;
        }

        MoveInDirection(moveDir, config.moveSpeed);
    }

    protected virtual void ReturnHome()
    {
        Vector3 moveDir = GetFlatDirectionTo(homePos, out float distance);

        if (distance <= 0.1f)
        {
            Vector3 pos = rb.position;
            pos.x = homePos.x;
            pos.z = homePos.z;
            rb.position = pos;
            EnterIdleState();
            return;
        }

        FaceDirection(moveDir);
        MoveInDirection(moveDir, config.returnSpeed);
    }

    protected virtual void HandleNoise(GameNoise noise)
    {
        Debug.Log($"[Enemy Noise Received] enemy={name}, kind={noise.kind}, noisePos={noise.position}");
        if (!isActiveAndEnabled) return;
        if (!config) return;

        if (state == State.Chase && vision != null && vision.CanSeeNow)
            return;

        if (noise.source == transform)
            return;

        float distance = Vector3.Distance(transform.position, noise.position);
        float effectiveRadius =
            noise.radius
            * Mathf.Max(0.01f, config.hearingSensitivity)
            * Mathf.Max(0.01f, noise.strength);

        Debug.Log($"[Enemy Noise Check] enemy={name}, distance={distance}, effectiveRadius={effectiveRadius}");

        if (distance > effectiveRadius)
            return;

        if (currentNoiseSource != null && noise.source == currentNoiseSource)
        {
            float timeSinceLastNoise = Time.time - lastNoiseAcceptTime;
            float movedDistance = Vector3.Distance(lastHeardPos, noise.position);

            bool tooSoon = timeSinceLastNoise < config.sameNoiseIgnoreSeconds;
            bool almostSamePosition = movedDistance < config.sameNoiseUpdateDistance;

            if (tooSoon && almostSamePosition)
                return;
        }

        currentNoiseSource = noise.source;
        lastNoiseAcceptTime = Time.time;

        EnterSoundAlertState(noise.position);
    }

    protected bool IsPlayerHiddenByShadow()
    {
        return playerShadow != null && playerShadow.IsInShadowMode;
    }

    protected bool ShouldReturnHome()
    {
        Vector3 flat = homePos - transform.position;
        flat.y = 0f;
        return flat.sqrMagnitude > 0.15f * 0.15f;
    }

    protected Vector3 GetFlatDirectionTo(Vector3 targetPos, out float distance)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;
        distance = dir.magnitude;

        if (distance <= 0.001f)
            return Vector3.zero;

        return dir.normalized;
    }

    protected void FaceDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

        if (config != null && config.turnSpeed > 0f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                config.turnSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.rotation = targetRot;
        }
    }

    protected void MoveInDirection(Vector3 dir, float speed)
    {
        if (dir.sqrMagnitude <= 0.0001f)
        {
            StopMove();
            return;
        }

        Vector3 flatDir = dir.normalized;
        desiredVelocity = flatDir * speed;
        lastMoveDir = flatDir;
    }

    protected virtual void StopMove()
    {
        desiredVelocity = Vector3.zero;
    }

    protected virtual void ApplyMovement()
    {
        Vector3 v = rb.linearVelocity;

        float y = v.y;

        if (useCustomGravity)
        {
            if (isGrounded && y < 0f)
                y = groundedStickVelocity;

            y += gravity * Time.fixedDeltaTime;
            y = Mathf.Max(y, maxFallSpeed);
        }
        else
        {
            y = 0f;
        }

        rb.linearVelocity = new Vector3(desiredVelocity.x, y, desiredVelocity.z);
    }

    protected virtual void UpdateGroundState()
    {
        if (!useCustomGravity || groundMask.value == 0 || bodyCollider == null)
        {
            isGrounded = true;
            return;
        }

        float radius = GetColliderRadiusWorld(bodyCollider);
        float halfH = GetColliderHeightWorld(bodyCollider) * 0.5f;
        Vector3 centerWorld = bodyCollider.transform.TransformPoint(bodyCollider.center);

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
            QueryTriggerInteraction.Ignore
        );
    }

    protected virtual void UpdateAnim()
    {
        if (!anim) return;

        if (isAttacking)
        {
            anim.SetBool(walkBoolName, false);
            return;
        }

        bool isMoving = desiredVelocity.sqrMagnitude > 0.0001f;
        anim.SetBool(walkBoolName, isMoving);
    }

    protected virtual void TriggerAttackAnim()
    {
        if (!anim) return;
        if (string.IsNullOrEmpty(attackTriggerName)) return;

        anim.SetTrigger(attackTriggerName);
    }

    protected virtual void UpdateFlip(Vector3 dir)
    {
        if (flipRoot == null) return;
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector3 camRight = cam ? cam.right : Vector3.right;
        camRight.y = 0f;
        camRight.Normalize();

        float lr = Vector3.Dot(dir, camRight);
        if (Mathf.Abs(lr) > 0.001f)
            ApplyFlip(lr > 0f);
    }

    protected void ApplyFlip(bool faceRight)
    {
        if (flipRoot == null) return;

        if (!artFacesRight)
            faceRight = !faceRight;

        Vector3 scale = flipRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * (faceRight ? 1f : -1f);
        flipRoot.localScale = scale;
    }

    protected virtual void OnCollisionStay(Collision collision)
    {
        //TryDamagePlayer(collision.collider);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        //TryDamagePlayer(collision.collider);
    }

    protected virtual void TryDamagePlayer(Collider hitCollider)
    {
        if (!config) return;
        if (!hitCollider) return;
        if (isAttacking) return;

        if (Time.time - lastDamageTime < config.contactDamageCooldown) return;

        PlayerHealth hp = hitCollider.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;
        if (!hp.CompareTag(playerTag)) return;

        StartAttack(hp);
    }

    protected virtual void UpdateAlertUI()
    {
        if (!alertUI) return;

        switch (state)
        {
            case State.VisualAlert:
            case State.LostWait:
                alertUI.Show(EnemyAwarenessDisplay.VisualAlert, visualAlert01);
                break;

            case State.Chase:
                alertUI.Show(EnemyAwarenessDisplay.Attack, 1f);
                break;

            case State.SoundAlert:
                alertUI.Show(EnemyAwarenessDisplay.SoundAlert, soundAlert01);
                break;

            default:
                alertUI.Show(EnemyAwarenessDisplay.Hidden, 0f);
                break;
        }
    }

    public virtual void KillByAssassination()
    {
        if (!config || !config.canBeAssassinated) return;
        gameObject.SetActive(false);
    }

    void SetupRigidbody()
    {
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    float GetColliderHeightWorld(CapsuleCollider col)
    {
        Vector3 scale = col.transform.lossyScale;
        return col.height * Mathf.Abs(scale.y);
    }

    float GetColliderRadiusWorld(CapsuleCollider col)
    {
        Vector3 scale = col.transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        return col.radius * radiusScale;
    }

    protected virtual void StartAttack(PlayerHealth hp)
    {
        if (hp == null) return;

        lastDamageTime = Time.time;

        isAttacking = true;
        attackTarget = hp;
        attackDamageDone = false;

        float lockSeconds = Mathf.Max(0.01f, config.attackLockSeconds);
        float hitDelay = Mathf.Clamp(config.attackHitDelay, 0f, lockSeconds);

        attackEndTime = Time.time + lockSeconds;
        attackHitTime = Time.time + hitDelay;

        StopMove();

        if (rb != null)
        {
            Vector3 v = rb.linearVelocity;
            v.x = 0f;
            v.z = 0f;
            rb.linearVelocity = v;
        }

        if (anim != null)
            anim.SetBool(walkBoolName, false);

        TriggerAttackAnim();
    }

    protected virtual void UpdateAttackLock()
    {
        StopMove();
        ZeroHorizontalVelocity();

        if (player != null)
        {
            Vector3 dir = GetFlatDirectionTo(player.position, out _);
            FaceDirection(dir);
        }

        // 데미지는 Animation Event: Anim_AttackHit()에서만 처리한다.
        // 이 타이머는 Animation Event가 누락됐을 때 공격 상태가 영원히 유지되는 것만 방지한다.
        if (Time.time >= attackEndTime)
            EndAttack();
    }

    protected virtual void ApplyAttackDamage()
    {
        if (attackDamageDone) return;

        attackDamageDone = true;

        if (attackTarget == null) return;
        if (attackTarget.isDead) return;

        if (config.checkRangeOnAttackHit)
        {
            Vector3 enemyPos = transform.position;
            Vector3 targetPos = attackTarget.transform.position;

            enemyPos.y = 0f;
            targetPos.y = 0f;

            float distance = Vector3.Distance(enemyPos, targetPos);

            if (distance > config.attackHitRange)
                return;
        }

        attackTarget.TakeDamage(config.contactDamagePips);
    }

    protected virtual void EndAttack()
    {
        isAttacking = false;
        attackTarget = null;
        attackDamageDone = false;
    }

    public void Anim_AttackHit()
    {
        if (!isAttacking) return;
        ApplyAttackDamage();
    }

    public void Anim_AttackEnd()
    {
        if (!isAttacking) return;
        EndAttack();
    }

    protected void ZeroHorizontalVelocity()
    {
        if (rb == null) return;

        Vector3 v = rb.linearVelocity;
        v.x = 0f;
        v.z = 0f;
        rb.linearVelocity = v;
    }
}