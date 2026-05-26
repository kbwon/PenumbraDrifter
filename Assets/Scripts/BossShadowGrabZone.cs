using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossShadowGrabZone : MonoBehaviour
{
    [Header("Refs")]
    public BossController boss;
    public string playerTag = "Player";

    [Header("Follow Shadow Position")]
    public bool followProjectedShadow = true;
    public bool detachFromParentOnAwake = true;

    [Tooltip("보스 그림자 계산 기준점입니다. 보통 ShadowCaster를 넣으면 됩니다.")]
    public Transform shadowSource;

    [Tooltip("보스 그림자를 만드는 주 광원입니다. Sun 또는 보스전 Gameplay Light를 넣으세요.")]
    public Light projectionLight;

    public LayerMask groundMask;

    [Tooltip("플레이어 탐지용 레이어입니다. 처음에는 Everything으로 두고 테스트해도 됩니다.")]
    public LayerMask playerMask = ~0;

    public float sourceHeight = 1.2f;
    public float maxProjectionDistance = 30f;
    public float groundYOffset = 0.05f;
    public Vector3 fallbackWorldOffset = Vector3.zero;

    [Tooltip("그림자 위치가 반대로 잡히면 체크하세요.")]
    public bool invertProjectionDirection = false;

    [Tooltip("부모 회전 영향을 받지 않도록 월드 회전을 고정합니다.")]
    public bool keepWorldRotation = true;

    [Header("Overlap Polling")]
    public bool useOverlapPolling = true;
    public int overlapBufferSize = 32;

    [Header("Debug")]
    public bool debugLog;
    public bool debugTriggerContacts;

    Collider myCollider;
    SphereCollider sphereCollider;
    Collider[] overlapHits;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        myCollider.isTrigger = true;

        sphereCollider = GetComponent<SphereCollider>();

        if (boss == null)
            boss = GetComponentInParent<BossController>();

        if (shadowSource == null && boss != null)
            shadowSource = boss.transform;

        if (groundMask.value == 0 && boss != null)
            groundMask = boss.groundMask;

        overlapHits = new Collider[Mathf.Max(4, overlapBufferSize)];

        // 핵심:
        // 보스 루트가 플레이어를 향해 회전해도 Zone은 월드 위치를 직접 따라가게 분리합니다.
        if (detachFromParentOnAwake)
            transform.SetParent(null, true);
    }

    void FixedUpdate()
    {
        if (followProjectedShadow)
            UpdateShadowZonePosition();

        if (useOverlapPolling)
            PollPlayerOverlap();
    }

    void LateUpdate()
    {
        if (followProjectedShadow)
            UpdateShadowZonePosition();
    }

    void UpdateShadowZonePosition()
    {
        if (shadowSource == null)
            return;

        Vector3 sourcePos = shadowSource.position + Vector3.up * sourceHeight;

        if (TryGetProjectionDirection(sourcePos, out Vector3 projectionDir) &&
            TryProjectToGround(sourcePos, projectionDir, out Vector3 projectedPoint))
        {
            transform.position = projectedPoint + Vector3.up * groundYOffset;
        }
        else
        {
            Vector3 fallbackOrigin = shadowSource.position + fallbackWorldOffset + Vector3.up * 5f;

            if (TryProjectToGround(fallbackOrigin, Vector3.down, out Vector3 groundPoint))
                transform.position = groundPoint + Vector3.up * groundYOffset;
            else
                transform.position = shadowSource.position + fallbackWorldOffset + Vector3.up * groundYOffset;
        }

        if (keepWorldRotation)
            transform.rotation = Quaternion.identity;
    }

    bool TryGetProjectionDirection(Vector3 sourcePos, out Vector3 dir)
    {
        dir = Vector3.down;

        if (projectionLight == null || !projectionLight.enabled)
            return true;

        switch (projectionLight.type)
        {
            case LightType.Directional:
                dir = projectionLight.transform.forward;
                break;

            case LightType.Point:
            case LightType.Spot:
                dir = sourcePos - projectionLight.transform.position;
                break;

            default:
                dir = Vector3.down;
                break;
        }

        if (invertProjectionDirection)
            dir = -dir;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector3.down;

        dir.Normalize();

        if (dir.y >= -0.01f)
            dir = Vector3.down;

        return true;
    }

    bool TryProjectToGround(Vector3 origin, Vector3 dir, out Vector3 point)
    {
        point = default;

        if (groundMask.value == 0)
            return false;

        if (Physics.Raycast(
            origin,
            dir.normalized,
            out RaycastHit hit,
            maxProjectionDistance,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            return true;
        }

        return false;
    }

    void PollPlayerOverlap()
    {
        Vector3 center = GetOverlapCenter();
        float radius = GetOverlapRadius();

        int count = Physics.OverlapSphereNonAlloc(
            center,
            radius,
            overlapHits,
            playerMask,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapHits[i];
            if (hit == null) continue;

            if (TryTriggerShadowGrabFromCollider(hit))
                break;
        }
    }

    Vector3 GetOverlapCenter()
    {
        if (sphereCollider != null)
            return sphereCollider.transform.TransformPoint(sphereCollider.center);

        return myCollider != null ? myCollider.bounds.center : transform.position;
    }

    float GetOverlapRadius()
    {
        if (sphereCollider != null)
        {
            Vector3 s = sphereCollider.transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            return Mathf.Max(0.01f, sphereCollider.radius * maxScale);
        }

        if (myCollider != null)
            return Mathf.Max(myCollider.bounds.extents.x, myCollider.bounds.extents.z);

        return 1f;
    }

    void OnTriggerStay(Collider other)
    {
        if (debugTriggerContacts)
            Debug.Log($"[BossShadowGrabZone] Trigger contact: {other.name}, layer={LayerMask.LayerToName(other.gameObject.layer)}", this);

        TryTriggerShadowGrabFromCollider(other);
    }

    bool TryTriggerShadowGrabFromCollider(Collider other)
    {
        if (boss == null) return false;
        if (other == null) return false;

        PlayerController playerController = other.GetComponentInParent<PlayerController>();
        if (playerController == null) return false;

        if (!playerController.CompareTag(playerTag)) return false;

        ShadowInteractController shadow = playerController.GetComponent<ShadowInteractController>();
        if (shadow == null) return false;
        if (!shadow.IsInShadowMode) return false;

        if (debugLog)
            Debug.Log($"[BossShadowGrabZone] ShadowGrab condition met: {other.name}", this);

        boss.TryStartShadowGrab(shadow);
        return true;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(GetOverlapCenter(), GetOverlapRadius());
    }
#endif
}