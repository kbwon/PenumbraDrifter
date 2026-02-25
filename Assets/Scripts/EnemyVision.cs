using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public Transform eye;               // 비우면 this.transform
    public LayerMask obstacleMask;      // 시야를 막는 레이어(벽/오브젝트 등)
    public EnemyConfig config;

    Transform target;
    ShadowInteractController targetShadow;

    float visibleTimer;
    float loseGraceTimer;

    public bool IsDetected { get; private set; }  // “발각 완료” 상태
    public bool CanSeeNow { get; private set; }   // 현재 프레임 시야에 보이는지(그림자면 false)

    public void SetTarget(Transform t)
    {
        target = t;
        targetShadow = t ? t.GetComponent<ShadowInteractController>() : null;
    }

    void Awake()
    {
        if (!eye) eye = transform;
    }

    public void ResetDetection()
    {
        visibleTimer = 0f;
        loseGraceTimer = 0f;
        IsDetected = false;
        CanSeeNow = false;
    }

    void Update()
    {
        if (!config || !target)
        {
            CanSeeNow = false;
            return;
        }

        // 플레이어가 “그림자 모드”이면 적은 못 봄
        if (targetShadow && targetShadow.IsInShadowMode)
        {
            CanSeeNow = false;
            // 감지 중이었다면 바로 끊기지 않게 grace로 처리
            loseGraceTimer += Time.deltaTime;
            if (loseGraceTimer >= config.loseSightGrace)
            {
                visibleTimer = 0f;
            }
            return;
        }

        Vector3 origin = eye.position;
        Vector3 toT = target.position - origin;
        toT.y = 0f;

        float dist = toT.magnitude;
        if (dist > config.viewDistance || dist < 0.001f)
        {
            CanSeeNow = false;
            DecayDetection();
            return;
        }

        // 각도 체크
        Vector3 fwd = eye.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 dir = toT / dist;

        float ang = Vector3.Angle(fwd, dir);
        if (ang > config.viewAngle * 0.5f)
        {
            CanSeeNow = false;
            DecayDetection();
            return;
        }

        // 시야 가림(LOS)
        // Raycast는 높이가 너무 낮으면 바닥/오브젝트에 걸릴 수 있으니 eye 위치를 적당히 잡아주세요.
        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // 중간에 장애물이 있으면 못 봄
            CanSeeNow = false;
            DecayDetection();
            return;
        }

        // 여기까지 오면 “현재 보임”
        CanSeeNow = true;
        loseGraceTimer = 0f;

        if (!IsDetected)
        {
            visibleTimer += Time.deltaTime;
            if (visibleTimer >= config.detectTimeRequired)
            {
                IsDetected = true;
            }
        }
    }

    void DecayDetection()
    {
        CanSeeNow = false;
        loseGraceTimer += Time.deltaTime;

        if (loseGraceTimer >= config.loseSightGrace)
        {
            visibleTimer = 0f;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!config) return;
        Transform e = eye ? eye : transform;
        Vector3 pos = e.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, config.viewDistance);

        Vector3 fwd = e.forward; fwd.y = 0f; fwd.Normalize();
        Quaternion left = Quaternion.Euler(0, -config.viewAngle * 0.5f, 0);
        Quaternion right = Quaternion.Euler(0, config.viewAngle * 0.5f, 0);
        Gizmos.DrawLine(pos, pos + (left * fwd) * config.viewDistance);
        Gizmos.DrawLine(pos, pos + (right * fwd) * config.viewDistance);
    }
#endif
}
