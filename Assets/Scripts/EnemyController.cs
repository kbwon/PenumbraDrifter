using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    public EnemyConfig config;
    public EnemyVision vision;

    [Header("Target")]
    public string playerTag = "Player";

    enum State { Idle, Chase, LostWait, Return }
    State state = State.Idle;

    Transform player;
    CharacterController cc;

    Vector3 homePos;
    float lostWaitTimer;
    float lastDamageTime = -999f;
    float notSeenTimer;
    void Awake()
    {
        cc = GetComponent<CharacterController>();
        homePos = transform.position;
    }

    void Start()
    {
        if (!vision) vision = GetComponent<EnemyVision>();
        if (vision) vision.config = config;

        var pObj = GameObject.FindGameObjectWithTag(playerTag);
        if (pObj)
        {
            player = pObj.transform;
            if (vision) vision.SetTarget(player);
        }
    }

    void Update()
    {
        if (!config || !player || !vision) return;

        switch (state)
        {
            case State.Idle:
                // 발각되면 추적
                if (vision.IsDetected)
                {
                    state = State.Chase;
                    // (확장 포인트) TODO: 발견 연출/사운드
                }
                break;

            case State.Chase:
                {
                    if (vision.CanSeeNow)
                    {
                        // 보이면 타이머 리셋
                        notSeenTimer = 0f;
                        ChasePlayer();
                        break;
                    }

                    // 안 보이면 누적
                    notSeenTimer += Time.deltaTime;

                    // ✅ N초 이상 못 보면 그때 LostWait로
                    if (notSeenTimer >= config.loseChaseAfterNotSeenSeconds)
                    {
                        state = State.LostWait;
                        lostWaitTimer = config.waitAfterLost;
                        notSeenTimer = 0f;

                        StopMove();
                        vision.ResetDetection(); // 중요: Return/재발각 흐름 정상화
                    }
                    else
                    {
                        // 잠깐 가려진 동안은 "마지막 동작 유지" 느낌을 원하면 여기서 약간 전진/정지 등 선택 가능
                        StopMove();
                    }
                    break;
                }

            case State.LostWait:
                if (vision.IsDetected)   // 다시 1~2초 머물러야 발각
                {
                    state = State.Chase;
                    break;
                }

                lostWaitTimer -= Time.deltaTime;
                if (lostWaitTimer <= 0f)
                {
                    state = State.Return;
                }
                break;

            case State.Return:
                if (vision.IsDetected)   // 다시 발각되면 추적
                {
                    state = State.Chase;
                    break;
                }

                ReturnHome();
                break;
        }
    }

    void ChasePlayer()
    {
        Vector3 toP = player.position - transform.position;
        toP.y = 0f;
        float dist = toP.magnitude;

        if (dist > 0.001f)
        {
            // 바라보기
            transform.rotation = Quaternion.LookRotation(toP.normalized, Vector3.up);
        }

        if (dist <= config.stopDistance)
        {
            StopMove();
            return;
        }

        Vector3 move = toP.normalized * (config.moveSpeed * Time.deltaTime);
        cc.Move(move);
    }

    void ReturnHome()
    {
        Vector3 toH = homePos - transform.position;
        toH.y = 0f;
        float dist = toH.magnitude;

        if (dist <= 0.1f)
        {
            // 초기화
            transform.position = new Vector3(homePos.x, transform.position.y, homePos.z);
            state = State.Idle;
            vision.ResetDetection();
            return;
        }

        if (dist > 0.001f)
            transform.rotation = Quaternion.LookRotation(toH.normalized, Vector3.up);

        Vector3 move = toH.normalized * (config.returnSpeed * Time.deltaTime);
        cc.Move(move);
    }

    void StopMove()
    {
        // CharacterController는 별도 속도 없으면 그냥 Move를 안 하면 멈춤
    }

    // ✅ 접촉 데미지: 적 CharacterController가 다른 콜라이더에 부딪힐 때 호출됨
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!config) return;
        if (!hit.collider || !hit.collider.CompareTag(playerTag)) return;

        // 쿨다운
        if (Time.time - lastDamageTime < config.contactDamageCooldown) return;
        lastDamageTime = Time.time;

        var hp = hit.collider.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            hp.TakeDamage(config.contactDamagePips);
            // TODO: Damage animation / hit feedback on enemy here (원하면)
        }
    }
}
