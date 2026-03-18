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
    public bool hasSurfaceAnchor => hasSurfaceAnchorInternal;
    public Vector3 AnchorNormal => anchorNormal;

    void Awake()
    {
        if (shadowIndicator) shadowIndicator.SetActive(false);

        if (visualRoot != null)
            visualOriginalLocalPos = visualRoot.localPosition;

        if (surfaceMask.value == 0)
            surfaceMask = groundMask;

        NotifyGaugeChanged(true);
    }

    void Update()
    {
        if (!TryGetCurrentSurfacePoint(out var surfacePoint, out var surfaceNormal))
        {
            SetIndicator(false);

            if (inShadowMode)
                ExitShadowMode();

            return;
        }

        bool onShadow = IsShadowAtPoint(surfacePoint, surfaceNormal);

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

        if (visualRoot != null)
            visualRoot.localPosition = visualOriginalLocalPos;

        ClearSurfaceAnchor();
        OnShadowModeChanged?.Invoke(false);
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
        if (!shadowIndicator) return;
        if (indicatorVisible == visible) return;

        indicatorVisible = visible;
        shadowIndicator.SetActive(visible);
    }

    public bool TryGetCurrentSurfacePoint(out Vector3 point, out Vector3 normal)
    {
        if (hasSurfaceAnchorInternal && anchorCollider != null)
        {
            Vector3 origin = transform.position + anchorNormal * 1.0f;
            Vector3 dir = -anchorNormal;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Ignore))
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

    public bool IsShadowAtPoint(Vector3 point, Vector3 normal)
    {
        return ShadowQueryDirectional.IsInShadow(point, normal, sun, occluderMask, maxDistance: maxDirDistance);
    }

    public bool IsShadowSafeAtWorldPos(Vector3 worldPos, float margin)
    {
        if (hasSurfaceAnchorInternal && anchorCollider != null)
        {
            Vector3 origin = worldPos + anchorNormal * 1.0f;
            Vector3 dir = -anchorNormal;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)
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

    public bool IsShadowSafeAtPoint(Vector3 point, Vector3 normal, float margin)
    {
        normal = normal.normalized;
        BuildTangents(normal, out var t1, out var t2);

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

    public void SnapToAnchoredSurface(Rigidbody body, float snapDistance = 2f)
    {
        if (!body || !hasSurfaceAnchorInternal || anchorCollider == null) return;

        Vector3 origin = body.position + anchorNormal * 1.0f;
        Vector3 dir = -anchorNormal;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, snapDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != anchorCollider) return;

            body.position = hit.point + anchorNormal * anchorSurfaceOffset;
        }
    }
}
