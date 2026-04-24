using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    public EnemyConfig config;
    public EnemyVision vision;
    public EnemyAlertUI alertUI;

    [Header("Target")]
    public string playerTag = "Player";

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
    protected CharacterController cc;

    protected Vector3 homePos;

    protected float visualAlert01;
    protected float visualLostGraceTimer;
    protected float soundAlert01;
    protected Vector3 lastHeardPos;
    protected float soundWaitTimer;

    protected float lostWaitTimer;
    protected float lastDamageTime = -999f;
    protected float notSeenTimer;

    public string CurrentStateName => state.ToString();
    public float VisualAlert01 => visualAlert01;
    public float SoundAlert01 => soundAlert01;

    protected virtual void Awake()
    {
        cc = GetComponent<CharacterController>();
        homePos = transform.position;
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
        if (!vision)
            vision = GetComponent<EnemyVision>();

        if (!alertUI)
           alertUI = GetComponentInChildren<EnemyAlertUI>(true);

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
            playerShadow = player.GetComponent<ShadowInteractController>();

        if (vision && player != null)
            vision.SetTarget(player);
    }

    protected virtual void Update()
    {
        if (!HasRequiredRefs()) return;

        vision.RefreshNow();

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

        UpdateAlertUI();
    }

    protected virtual bool HasRequiredRefs()
    {
        return config && player && vision && cc;
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

        soundAlert01 = Mathf.MoveTowards(
            soundAlert01,
            1f,
            Time.deltaTime / Mathf.Max(0.01f, config.soundAlertFillSeconds)
        );

        Vector3 moveDir = GetFlatDirectionTo(lastHeardPos, out float distance);

        if (distance > config.soundStopDistance)
        {
            FaceDirection(moveDir);
            MoveInDirection(moveDir, config.soundMoveSpeed);
            return;
        }

        StopMove();

        if (moveDir.sqrMagnitude > 0.0001f)
            FaceDirection(moveDir);

        soundWaitTimer -= Time.deltaTime;

        if (soundWaitTimer <= 0f)
        {
            soundAlert01 = 0f;

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
        visualLostGraceTimer = 0f;
    }

    protected virtual void EnterReturnState()
    {
        state = State.Return;
        soundAlert01 = 0f;
        visualAlert01 = 0f;
        notSeenTimer = 0f;
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

        if (distance <= config.stopDistance)
        {
            StopMove();
            return;
        }

        MoveInDirection(moveDir, config.moveSpeed);
    }

    protected virtual void ReturnHome()
    {
        Vector3 moveDir = GetFlatDirectionTo(homePos, out float distance);

        if (distance <= 0.1f)
        {
            transform.position = new Vector3(homePos.x, transform.position.y, homePos.z);
            EnterIdleState();
            return;
        }

        FaceDirection(moveDir);
        MoveInDirection(moveDir, config.returnSpeed);
    }

    protected virtual void HandleNoise(GameNoise noise)
    {
        if (!isActiveAndEnabled) return;
        if (!config) return;

        if (state == State.Chase && vision != null && vision.CanSeeNow)
            return;

        if (noise.source == transform)
            return;

        float distance = Vector3.Distance(transform.position, noise.position);
        float effectiveRadius = noise.radius * Mathf.Max(0.01f, config.hearingSensitivity) * Mathf.Max(0.01f, noise.strength);

        if (distance > effectiveRadius)
            return;

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
        if (dir.sqrMagnitude <= 0.0001f) return;
        cc.Move(dir * (speed * Time.deltaTime));
    }

    protected virtual void StopMove()
    {
        // CharacterController는 Move를 호출하지 않으면 멈춘다.
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

    protected virtual void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!config) return;
        if (!hit.collider) return;
        if (Time.time - lastDamageTime < config.contactDamageCooldown) return;

        PlayerHealth hp = hit.collider.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;
        if (!hp.CompareTag(playerTag)) return;

        lastDamageTime = Time.time;
        hp.TakeDamage(config.contactDamagePips);
    }

    public virtual void KillByAssassination()
    {
        if (!config || !config.canBeAssassinated) return;
        gameObject.SetActive(false);
    }
}