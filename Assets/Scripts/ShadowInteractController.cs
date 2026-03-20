using System;
using UnityEngine;

public class ShadowInteractController : MonoBehaviour
{
    [Header("Masks")]
    public LayerMask groundMask;
    public LayerMask occluderMask;
    public LayerMask surfaceMask;

    [Header("Directional Light")]
    public Light sun;

    [Header("Indicator")]
    public GameObject shadowIndicator;

    [Header("Shadow Mode")]
    public Transform visualRoot;
    public float sinkVisualY = -0.35f;
    public float shadowSpeedMul = 0.6f;
    public float maxDirDistance = 80f;

    [Header("Shadow Gauge")]
    public float drainFullSeconds = 5f;
    public float regenFullSeconds = 10f;
    [Range(0f, 1f)] public float gauge01 = 1f;

    [Header("Input")]
    public int mouseButton = 1;

    [Header("Anchor")]
    public float anchorSurfaceOffset = 0.08f;
    public float anchorProbeOffset = 1f;
    public float anchorProbeDistance = 2.5f;

    [Header("Colliders")]
    public CapsuleCollider normalCollider;
    public CapsuleCollider shadowCollider;

    bool inShadowMode;
    bool indicatorVisible;
    bool hasSurfaceAnchorInternal;

    Vector3 visualOriginalLocalPos;
    Vector3 anchorNormal;
    Collider anchorCollider;
    float lastGaugeValue = -1f;

    public event Action<bool> OnShadowModeChanged;
    public event Action<float> OnGaugeChanged;

    public bool IsInShadowMode => inShadowMode;
    public float SpeedMultiplier => inShadowMode ? shadowSpeedMul : 1f;
    public float Gauge01 => gauge01;
    public bool HasSurfaceAnchor => hasSurfaceAnchorInternal;
    public Vector3 AnchorNormal => anchorNormal;

    // 현재 상태에 맞는 콜라이더를 반환한다.
    public CapsuleCollider ActiveCollider
    {
        get
        {
            if (inShadowMode)
            {
                if (shadowCollider != null) return shadowCollider;
                if (normalCollider != null) return normalCollider;
            }
            else
            {
                if (normalCollider != null) return normalCollider;
                if (shadowCollider != null) return shadowCollider;
            }

            return GetComponent<CapsuleCollider>();
        }
    }

    void Awake()
    {
        CacheColliders();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterShadow(this);

        if (shadowIndicator != null)
            shadowIndicator.SetActive(false);

        if (visualRoot != null)
            visualOriginalLocalPos = visualRoot.localPosition;

        if (surfaceMask.value == 0)
            surfaceMask = groundMask;

        ApplyColliderMode(false);
        NotifyGaugeChanged(true);
    }

    void Update()
    {
        if (!TryGetCurrentSurfacePoint(out Vector3 point, out Vector3 normal))
        {
            SetIndicator(false);

            if (inShadowMode)
                ExitShadowMode();

            return;
        }

        bool onShadow = IsShadowAtPoint(point, normal);

        if (!inShadowMode)
            SetIndicator(onShadow);
        else
            SetIndicator(false);

        UpdateGauge(onShadow);

        if (Input.GetMouseButtonDown(mouseButton) && onShadow)
        {
            if (!inShadowMode)
            {
                if (gauge01 > 0f)
                    EnterShadowMode();
            }
            else
            {
                ExitShadowMode();
            }
        }
    }

    void CacheColliders()
    {
        if (normalCollider != null && shadowCollider != null)
            return;

        CapsuleCollider[] cols = GetComponentsInChildren<CapsuleCollider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            CapsuleCollider col = cols[i];
            if (col == null) continue;

            if (col.gameObject == gameObject)
            {
                if (normalCollider == null)
                    normalCollider = col;
                continue;
            }

            if (normalCollider == null)
            {
                normalCollider = col;
                continue;
            }

            if (shadowCollider == null && col != normalCollider)
                shadowCollider = col;
        }
    }

    void UpdateGauge(bool onShadow)
    {
        if (inShadowMode)
        {
            gauge01 -= Time.deltaTime / Mathf.Max(0.01f, drainFullSeconds);
            gauge01 = Mathf.Clamp01(gauge01);

            if (gauge01 <= 0f)
            {
                gauge01 = 0f;
                ExitShadowMode();
            }
            else if (!onShadow)
            {
                ExitShadowMode();
            }
        }
        else
        {
            gauge01 += Time.deltaTime / Mathf.Max(0.01f, regenFullSeconds);
            gauge01 = Mathf.Clamp01(gauge01);
        }

        NotifyGaugeChanged(false);
    }

    void EnterShadowMode()
    {
        inShadowMode = true;
        ApplyColliderMode(true);

        if (visualRoot != null)
        {
            Vector3 pos = visualOriginalLocalPos;
            pos.y += sinkVisualY;
            visualRoot.localPosition = pos;
        }

        SetIndicator(false);
        OnShadowModeChanged?.Invoke(true);
    }

    void ExitShadowMode()
    {
        inShadowMode = false;
        ApplyColliderMode(false);

        if (visualRoot != null)
            visualRoot.localPosition = visualOriginalLocalPos;

        ClearSurfaceAnchor();
        OnShadowModeChanged?.Invoke(false);
    }

    void ApplyColliderMode(bool shadowMode)
    {
        if (normalCollider != null && normalCollider != shadowCollider)
            normalCollider.enabled = !shadowMode;

        if (shadowCollider != null)
            shadowCollider.enabled = shadowMode;

        if (normalCollider != null && shadowCollider == null)
            normalCollider.enabled = true;
    }

    void NotifyGaugeChanged(bool force)
    {
        if (force || Mathf.Abs(lastGaugeValue - gauge01) > 0.0001f)
        {
            lastGaugeValue = gauge01;
            OnGaugeChanged?.Invoke(gauge01);
        }
    }

    void SetIndicator(bool visible)
    {
        if (shadowIndicator == null) return;
        if (indicatorVisible == visible) return;

        indicatorVisible = visible;
        shadowIndicator.SetActive(visible);
    }

    // 현재 표면의 점과 법선을 구한다.
    public bool TryGetCurrentSurfacePoint(out Vector3 point, out Vector3 normal)
    {
        if (hasSurfaceAnchorInternal && anchorCollider != null)
        {
            Vector3 origin = transform.position + anchorNormal * anchorProbeOffset;
            Vector3 dir = -anchorNormal;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, anchorProbeDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == anchorCollider)
                {
                    point = hit.point;
                    normal = hit.normal;
                    return true;
                }
            }

            ClearSurfaceAnchor();
        }

        Vector3 originDown = transform.position + Vector3.up * 2f;
        if (Physics.Raycast(originDown, Vector3.down, out RaycastHit hitDown, 10f, surfaceMask, QueryTriggerInteraction.Ignore))
        {
            point = hitDown.point;
            normal = hitDown.normal;
            return true;
        }

        point = default;
        normal = Vector3.up;
        return false;
    }

    public bool IsShadowAtWorldPos(Vector3 worldPos)
    {
        Vector3 origin = worldPos + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, surfaceMask, QueryTriggerInteraction.Ignore))
            return IsShadowAtPoint(hit.point, hit.normal);

        return false;
    }

    // 표면의 한 점이 그림자인지 검사한다.
    public bool IsShadowAtPoint(Vector3 point, Vector3 normal)
    {
        return ShadowQuery.IsPointInShadow(point, normal, sun, occluderMask, maxDirDistance: maxDirDistance);
    }

    public bool IsShadowSafeAtWorldPos(Vector3 worldPos, float margin)
    {
        if (hasSurfaceAnchorInternal && anchorCollider != null)
        {
            Vector3 origin = worldPos + anchorNormal * anchorProbeOffset;
            Vector3 dir = -anchorNormal;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, anchorProbeDistance, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider == anchorCollider)
            {
                return IsShadowSafeAtPoint(hit.point, hit.normal, margin);
            }
        }

        Vector3 originDown = worldPos + Vector3.up * 2f;
        if (!Physics.Raycast(originDown, Vector3.down, out RaycastHit hitDown, 10f, surfaceMask, QueryTriggerInteraction.Ignore))
            return false;

        return IsShadowSafeAtPoint(hitDown.point, hitDown.normal, margin);
    }

    // 중심과 주변 점을 함께 검사해 안전한 그림자인지 확인한다.
    public bool IsShadowSafeAtPoint(Vector3 point, Vector3 normal, float margin)
    {
        normal = normal.normalized;
        BuildTangents(normal, out Vector3 t1, out Vector3 t2);

        Vector3[] offsets =
        {
            Vector3.zero,
            t1 * margin,
            -t1 * margin,
            t2 * margin,
            -t2 * margin,
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 test = point + offsets[i];
            if (!IsShadowAtPoint(test, normal))
                return false;
        }

        return true;
    }

    static void BuildTangents(Vector3 normal, out Vector3 t1, out Vector3 t2)
    {
        Vector3 axis = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
        t1 = Vector3.Cross(normal, axis).normalized;
        t2 = Vector3.Cross(normal, t1).normalized;
    }

    public void ForceExitShadowMode()
    {
        if (inShadowMode)
            ExitShadowMode();
    }

    public void SetSurfaceAnchor(Vector3 normal, Collider col)
    {
        hasSurfaceAnchorInternal = true;
        anchorNormal = normal.normalized;
        anchorCollider = col;
    }

    public void ClearSurfaceAnchor()
    {
        hasSurfaceAnchorInternal = false;
        anchorCollider = null;
    }

    // 붙은 표면을 다시 찾아 플레이어를 표면에 맞춘다.
    public void SnapToAnchoredSurface(Transform actor, float snapDistance = 2f)
    {
        if (actor == null || !hasSurfaceAnchorInternal || anchorCollider == null) return;

        Vector3 origin = actor.position + anchorNormal * anchorProbeOffset;
        Vector3 dir = -anchorNormal;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, snapDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != anchorCollider) return;
            actor.position = GetRootPositionForSurfaceHit(hit.point, hit.normal, anchorSurfaceOffset);
        }
    }

    public void SnapToAnchoredSurface(Rigidbody body, float snapDistance = 2f)
    {
        if (body == null || !hasSurfaceAnchorInternal || anchorCollider == null) return;

        Vector3 origin = body.position + anchorNormal * anchorProbeOffset;
        Vector3 dir = -anchorNormal;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, snapDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != anchorCollider) return;
            body.position = GetRootPositionForSurfaceHit(hit.point, hit.normal, anchorSurfaceOffset);
        }
    }

    // 표면 hit 정보를 루트 위치로 바꾼다.
    public Vector3 GetRootPositionForSurfaceHit(Vector3 point, Vector3 normal, float extraOffset)
    {
        CapsuleCollider col = ActiveCollider;
        if (col == null) return transform.position;

        Vector3 newPos = transform.position;
        float radius = GetColliderRadiusWorld(col);
        float halfH = GetColliderHeightWorld(col) * 0.5f;
        float centerY = GetColliderCenterRootLocal(col).y;

        if (normal.y > 0.7f)
        {
            newPos.x = point.x;
            newPos.z = point.z;
            newPos.y = point.y + (halfH - centerY) + extraOffset;
        }
        else if (normal.y < -0.7f)
        {
            newPos.x = point.x;
            newPos.z = point.z;
            newPos.y = point.y - (centerY + halfH) - extraOffset;
        }
        else
        {
            newPos.x = point.x;
            newPos.z = point.z;
            newPos.y = point.y + (halfH - centerY);
            newPos += normal.normalized * (radius + extraOffset);
        }

        return newPos;
    }

    public float GetActiveRadiusWorld()
    {
        CapsuleCollider col = ActiveCollider;
        return col != null ? GetColliderRadiusWorld(col) : 0.35f;
    }

    public float GetActiveHeightWorld()
    {
        CapsuleCollider col = ActiveCollider;
        return col != null ? GetColliderHeightWorld(col) : 2f;
    }

    public Vector3 GetActiveCenterWorld()
    {
        CapsuleCollider col = ActiveCollider;
        if (col == null) return transform.position;
        return col.transform.TransformPoint(col.center);
    }

    public Vector3 GetActiveCenterRootLocal()
    {
        CapsuleCollider col = ActiveCollider;
        if (col == null) return Vector3.zero;
        return GetColliderCenterRootLocal(col);
    }

    public float GetActiveMargin(float factor = 0.9f)
    {
        return GetActiveRadiusWorld() * factor;
    }

    Vector3 GetColliderCenterRootLocal(CapsuleCollider col)
    {
        Vector3 worldCenter = col.transform.TransformPoint(col.center);
        return transform.InverseTransformPoint(worldCenter);
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
}
