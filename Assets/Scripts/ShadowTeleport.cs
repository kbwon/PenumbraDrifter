using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShadowTeleport : MonoBehaviour
{
    [Header("Refs")]
    public ShadowInteractController shadowCtrl;
    public Camera cam;

    [Header("Raycast")]
    public LayerMask groundMask;
    public LayerMask surfaceMask;
    public float rayMaxDistance = 200f;

    [Header("Teleport")]
    public float cooldownSeconds = 10f;
    public float maxTeleportDistance = 0f;
    public float floorOffset = 0.02f;
    public float wallOffset = 0.02f;

    float cooldownLeft;
    Rigidbody rb;

    public bool IsReady => cooldownLeft <= 0f;
    public float Cooldown01 => Mathf.Clamp01(cooldownLeft / Mathf.Max(0.01f, cooldownSeconds));

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (!shadowCtrl)
            shadowCtrl = GetComponent<ShadowInteractController>();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterTeleport(this);

        if (!cam)
        {
            if (GameManager.Instance != null)
                cam = GameManager.Instance.mainCamera;

            if (!cam)
                cam = Camera.main;
        }

        if (surfaceMask.value == 0)
            surfaceMask = groundMask;
    }

    void Update()
    {
        if (cooldownLeft > 0f)
            cooldownLeft = Mathf.Max(0f, cooldownLeft - Time.deltaTime);

        if (!shadowCtrl || !cam) return;
        if (!shadowCtrl.IsInShadowMode) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (!IsReady) return;
        if (shadowCtrl.Gauge01 <= 0f) return;

        if (!TryGetTeleportTarget(out RaycastHit hit)) return;

        TeleportToSurface(hit);
        cooldownLeft = cooldownSeconds;
    }

    // 마우스가 가리키는 표면이 순간이동 가능한지 검사한다.
    public bool TryGetTeleportTarget(out RaycastHit hit)
    {
        hit = default;

        if (!shadowCtrl || !cam) return false;
        if (!shadowCtrl.IsInShadowMode) return false;
        if (!IsReady) return false;
        if (shadowCtrl.Gauge01 <= 0f) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit, rayMaxDistance, surfaceMask, QueryTriggerInteraction.Ignore))
            return false;

        float margin = shadowCtrl.GetActiveMargin(0.9f);
        if (!shadowCtrl.IsShadowSafeAtPoint(hit.point, hit.normal, margin))
            return false;

        if (maxTeleportDistance > 0f)
        {
            Vector3 a = transform.position;
            Vector3 b = hit.point;
            a.y = 0f;
            b.y = 0f;

            if (Vector3.Distance(a, b) > maxTeleportDistance)
                return false;
        }

        return true;
    }

    void TeleportToSurface(RaycastHit hit)
    {
        Vector3 point = hit.point;
        Vector3 normal = hit.normal.normalized;
        float offset = Mathf.Abs(normal.y) > 0.7f ? floorOffset : wallOffset;

        rb.position = shadowCtrl.GetRootPositionForSurfaceHit(point, normal, offset);
        rb.linearVelocity = Vector3.zero;
        shadowCtrl.SetSurfaceAnchor(normal, hit.collider);
    }
}
