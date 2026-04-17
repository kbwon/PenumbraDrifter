using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    public EnemyConfig config;
    public EnemyVision vision;

    [Header("Target")]
    public string playerTag = "Player";

    // 기본 상태
    protected enum State { Idle, Chase, LostWait, Return }

    protected State state = State.Idle;
    protected Transform player;
    protected CharacterController cc;

    // 복귀 지점
    protected Vector3 homePos;

    // 놓친 뒤 대기
    protected float lostWaitTimer;

    // 접촉 데미지 쿨다운
    protected float lastDamageTime = -999f;

    // 추적 중 플레이어를 못 본 시간
    protected float notSeenTimer;

    protected virtual void Awake()
    {
        cc = GetComponent<CharacterController>();
        homePos = transform.position;
    }

    protected virtual void Start()
    {
        if (!vision)
            vision = GetComponent<EnemyVision>();

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

        if (vision && player != null)
            vision.SetTarget(player);
    }

    protected virtual void Update()
    {
        if (!HasRequiredRefs()) return;

        switch (state)
        {
            case State.Idle:
                UpdateIdleState();
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
    }

    protected virtual bool HasRequiredRefs()
    {
        return config && player && vision;
    }

    protected virtual void UpdateIdleState()
    {
        if (vision.IsDetected)
        {
            state = State.Chase;
            notSeenTimer = 0f;
        }
    }

    protected virtual void UpdateChaseState()
    {
        if (vision.CanSeeNow)
        {
            notSeenTimer = 0f;
            ChasePlayer();
            return;
        }

        notSeenTimer += Time.deltaTime;

        if (notSeenTimer >= config.loseChaseAfterNotSeenSeconds)
        {
            state = State.LostWait;
            lostWaitTimer = config.waitAfterLost;
            notSeenTimer = 0f;
            StopMove();
            vision.ResetDetection();
        }
        else
        {
            StopMove();
        }
    }

    protected virtual void UpdateLostWaitState()
    {
        if (vision.IsDetected)
        {
            state = State.Chase;
            return;
        }

        lostWaitTimer -= Time.deltaTime;
        if (lostWaitTimer <= 0f)
            state = State.Return;
    }

    protected virtual void UpdateReturnState()
    {
        if (vision.IsDetected)
        {
            state = State.Chase;
            return;
        }

        ReturnHome();
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
            state = State.Idle;
            vision.ResetDetection();
            return;
        }

        FaceDirection(moveDir);
        MoveInDirection(moveDir, config.returnSpeed);
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
        if (dir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
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