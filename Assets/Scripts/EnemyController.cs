using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    public EnemyConfig config;
    public EnemyVision vision;

    [Header("Target")]
    public string playerTag = "Player";

    // 적의 기본 상태이다.
    enum State { Idle, Chase, LostWait, Return }

    State state = State.Idle;
    Transform player;
    CharacterController cc;

    // 처음 위치는 복귀 지점으로 사용한다.
    Vector3 homePos;

    // 플레이어를 놓친 뒤 잠깐 대기하는 시간이다.
    float lostWaitTimer;

    // 접촉 데미지 쿨다운 관리용 시간이다.
    float lastDamageTime = -999f;

    // 추적 중 플레이어를 못 본 시간이다.
    float notSeenTimer;

    void Awake()
    {
        // CharacterController와 시작 위치를 저장한다.
        cc = GetComponent<CharacterController>();
        homePos = transform.position;
    }

    void Start()
    {
        // EnemyVision 참조가 없으면 같은 오브젝트에서 찾는다.
        if (!vision)
            vision = GetComponent<EnemyVision>();

        // Vision이 사용할 설정을 맞춘다.
        if (vision)
            vision.config = config;

        // 플레이어 Transform을 우선 GameManager에서 찾는다.
        if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
            player = GameManager.Instance.PlayerTransform;
        else
        {
            // GameManager가 없으면 태그로 찾는다.
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject)
                player = playerObject.transform;
        }

        // 시야 시스템에 플레이어를 전달한다.
        if (vision && player != null)
            vision.SetTarget(player);
    }

    void Update()
    {
        // 필수 참조가 없으면 동작하지 않는다.
        if (!config || !player || !vision) return;

        switch (state)
        {
            case State.Idle:
                // 발각되면 추적으로 전환한다.
                if (vision.IsDetected)
                    state = State.Chase;
                break;

            case State.Chase:
                // 지금 실제로 플레이어가 보이면 계속 추적한다.
                if (vision.CanSeeNow)
                {
                    notSeenTimer = 0f;
                    ChasePlayer();
                    break;
                }

                // 안 보이기 시작하면 시간을 누적한다.
                notSeenTimer += Time.deltaTime;

                // 일정 시간 이상 못 보면 잠깐 대기 상태로 넘어간다.
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
                    // 아직은 놓치지 않았으므로 제자리에서 대기한다.
                    StopMove();
                }
                break;

            case State.LostWait:
                // 다시 발각되면 바로 추적으로 복귀한다.
                if (vision.IsDetected)
                {
                    state = State.Chase;
                    break;
                }

                // 대기 시간이 끝나면 복귀 상태로 전환한다.
                lostWaitTimer -= Time.deltaTime;
                if (lostWaitTimer <= 0f)
                    state = State.Return;
                break;

            case State.Return:
                // 복귀 중 다시 발각되면 추적으로 전환한다.
                if (vision.IsDetected)
                {
                    state = State.Chase;
                    break;
                }

                // 시작 위치로 돌아간다.
                ReturnHome();
                break;
        }
    }

    void ChasePlayer()
    {
        // 적에서 플레이어로 가는 방향을 구한다.
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        // 플레이어 쪽을 바라보게 만든다.
        if (distance > 0.001f)
            transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);

        // 너무 가까우면 멈춘다.
        if (distance <= config.stopDistance)
        {
            StopMove();
            return;
        }

        // 플레이어를 향해 이동한다.
        Vector3 move = toPlayer.normalized * (config.moveSpeed * Time.deltaTime);
        cc.Move(move);
    }

    void ReturnHome()
    {
        // 원래 위치로 돌아가는 방향을 구한다.
        Vector3 toHome = homePos - transform.position;
        toHome.y = 0f;
        float distance = toHome.magnitude;

        // 거의 도착했으면 Idle 상태로 돌아간다.
        if (distance <= 0.1f)
        {
            transform.position = new Vector3(homePos.x, transform.position.y, homePos.z);
            state = State.Idle;
            vision.ResetDetection();
            return;
        }

        // 복귀 방향을 바라보게 만든다.
        if (distance > 0.001f)
            transform.rotation = Quaternion.LookRotation(toHome.normalized, Vector3.up);

        // 시작 위치를 향해 이동한다.
        Vector3 move = toHome.normalized * (config.returnSpeed * Time.deltaTime);
        cc.Move(move);
    }

    void StopMove()
    {
        // CharacterController는 Move를 호출하지 않으면 멈춘다.
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 설정이 없으면 처리하지 않는다.
        if (!config) return;

        // 충돌한 대상이 없으면 처리하지 않는다.
        if (!hit.collider) return;

        // 쿨다운 중이면 데미지를 주지 않는다.
        if (Time.time - lastDamageTime < config.contactDamageCooldown) return;

        // 플레이어의 자식 콜라이더에 닿아도 부모 PlayerHealth를 찾는다.
        PlayerHealth hp = hit.collider.GetComponentInParent<PlayerHealth>();
        if (hp == null) return;

        // 플레이어 태그가 아니면 무시한다.
        if (!hp.CompareTag(playerTag)) return;

        // 마지막 데미지 시간을 기록하고 체력을 깎는다.
        lastDamageTime = Time.time;
        hp.TakeDamage(config.contactDamagePips);
    }

    public void KillByAssassination()
    {
        // 암살 불가 적이면 제거하지 않는다.
        if (!config || !config.canBeAssassinated) return;

        // 필요하면 여기서 사망 이펙트나 사운드를 넣을 수 있다.
        gameObject.SetActive(false);
    }
}