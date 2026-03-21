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

    void Update()
    {
        if (!config || !player || !vision) return;

        switch (state)
        {
            case State.Idle:
                if (vision.IsDetected)
                    state = State.Chase;
                break;

            case State.Chase:
                if (vision.CanSeeNow)
                {
                    notSeenTimer = 0f;
                    ChasePlayer();
                    break;
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
                break;

            case State.LostWait:
                if (vision.IsDetected)
                {
                    state = State.Chase;
                    break;
                }

                lostWaitTimer -= Time.deltaTime;
                if (lostWaitTimer <= 0f)
                    state = State.Return;
                break;

            case State.Return:
                if (vision.IsDetected)
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
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > 0.001f)
            transform.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);

        if (distance <= config.stopDistance)
        {
            StopMove();
            return;
        }

        Vector3 move = toPlayer.normalized * (config.moveSpeed * Time.deltaTime);
        cc.Move(move);
    }

    void ReturnHome()
    {
        Vector3 toHome = homePos - transform.position;
        toHome.y = 0f;
        float distance = toHome.magnitude;

        if (distance <= 0.1f)
        {
            transform.position = new Vector3(homePos.x, transform.position.y, homePos.z);
            state = State.Idle;
            vision.ResetDetection();
            return;
        }

        if (distance > 0.001f)
            transform.rotation = Quaternion.LookRotation(toHome.normalized, Vector3.up);

        Vector3 move = toHome.normalized * (config.returnSpeed * Time.deltaTime);
        cc.Move(move);
    }

    void StopMove()
    {
        // CharacterController는 Move를 호출하지 않으면 멈춘다.
    }

    // 적이 플레이어와 닿으면 접촉 데미지를 준다.
    void OnControllerColliderHit(ControllerColliderHit hit)
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
}
