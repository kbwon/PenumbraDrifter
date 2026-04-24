using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public Transform eye;
    public LayerMask obstacleMask;
    public EnemyConfig config;

    Transform target;
    ShadowInteractController targetShadow;

    public bool CanSeeAlert { get; private set; }
    public bool CanSeeAttack { get; private set; }
    public bool CanSeeNow => CanSeeAlert || CanSeeAttack;
    public bool IsDetected => CanSeeAttack;
    public bool TargetInShadow => targetShadow != null && targetShadow.IsInShadowMode;
    public float TargetDistance { get; private set; }

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

    void Update()
    {
        RefreshNow();
    }

    public void RefreshNow()
    {
        CanSeeAlert = false;
        CanSeeAttack = false;
        TargetDistance = Mathf.Infinity;

        if (!config || !target)
            return;

        if (TargetInShadow)
            return;

        Vector3 origin = eye.position;
        Vector3 targetBody = target.position;
        Vector3 targetPoint = target.position + Vector3.up * config.targetPointYOffset;

        Vector3 flatToTarget = targetBody - origin;
        flatToTarget.y = 0f;

        float distance = flatToTarget.magnitude;
        TargetDistance = distance;

        if (distance > config.viewDistance || distance < 0.001f)
            return;

        Vector3 forward = eye.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 flatDir = flatToTarget / distance;
        float angle = Vector3.Angle(forward, flatDir);

        if (angle > config.viewAngle * 0.5f)
            return;

        if (IsBlocked(origin, targetPoint))
            return;

        CanSeeAlert = true;

        float attackDistance = Mathf.Min(config.attackViewDistance, config.viewDistance);
        CanSeeAttack = distance <= attackDistance;
    }

    bool IsBlocked(Vector3 origin, Vector3 targetPoint)
    {
        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;

        if (distance <= 0.001f)
            return false;

        Vector3 dir = toTarget / distance;

        return Physics.Raycast(
            origin,
            dir,
            distance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    public void ResetDetection()
    {
        CanSeeAlert = false;
        CanSeeAttack = false;
        TargetDistance = Mathf.Infinity;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!config) return;

        Transform eyeTransform = eye ? eye : transform;
        Vector3 pos = eyeTransform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, config.viewDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, config.attackViewDistance);

        Vector3 forward = eyeTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion left = Quaternion.Euler(0f, -config.viewAngle * 0.5f, 0f);
        Quaternion right = Quaternion.Euler(0f, config.viewAngle * 0.5f, 0f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pos, pos + (left * forward) * config.viewDistance);
        Gizmos.DrawLine(pos, pos + (right * forward) * config.viewDistance);
    }
#endif
}