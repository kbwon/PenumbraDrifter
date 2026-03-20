using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public Transform eye;
    public LayerMask obstacleMask;
    public EnemyConfig config;

    Transform target;
    ShadowInteractController targetShadow;

    float visibleTimer;
    float loseGraceTimer;

    public bool IsDetected { get; private set; }
    public bool CanSeeNow { get; private set; }

    public void SetTarget(Transform t)
    {
        target = t;
        targetShadow = t ? t.GetComponent<ShadowInteractController>() : null;
    }

    void Awake()
    {
        if (!eye)
            eye = transform;
    }

    void Start()
    {
        if (target == null && GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
            SetTarget(GameManager.Instance.PlayerTransform);
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

        // 그림자 모드의 플레이어는 보지 못한다.
        if (targetShadow && targetShadow.IsInShadowMode)
        {
            CanSeeNow = false;
            loseGraceTimer += Time.deltaTime;
            if (loseGraceTimer >= config.loseSightGrace)
                visibleTimer = 0f;
            return;
        }

        Vector3 origin = eye.position;
        Vector3 toTarget = target.position - origin;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        if (distance > config.viewDistance || distance < 0.001f)
        {
            CanSeeNow = false;
            DecayDetection();
            return;
        }

        Vector3 forward = eye.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 dir = toTarget / distance;
        float angle = Vector3.Angle(forward, dir);
        if (angle > config.viewAngle * 0.5f)
        {
            CanSeeNow = false;
            DecayDetection();
            return;
        }

        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            CanSeeNow = false;
            DecayDetection();
            return;
        }

        CanSeeNow = true;
        loseGraceTimer = 0f;

        if (!IsDetected)
        {
            visibleTimer += Time.deltaTime;
            if (visibleTimer >= config.detectTimeRequired)
                IsDetected = true;
        }
    }

    void DecayDetection()
    {
        CanSeeNow = false;
        loseGraceTimer += Time.deltaTime;

        if (loseGraceTimer >= config.loseSightGrace)
            visibleTimer = 0f;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!config) return;

        Transform eyeTransform = eye ? eye : transform;
        Vector3 pos = eyeTransform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, config.viewDistance);

        Vector3 forward = eyeTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion left = Quaternion.Euler(0f, -config.viewAngle * 0.5f, 0f);
        Quaternion right = Quaternion.Euler(0f, config.viewAngle * 0.5f, 0f);
        Gizmos.DrawLine(pos, pos + (left * forward) * config.viewDistance);
        Gizmos.DrawLine(pos, pos + (right * forward) * config.viewDistance);
    }
#endif
}
