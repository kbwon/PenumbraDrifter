using UnityEngine;

public class ShadowInteractController : MonoBehaviour
{
    [Header("Masks")]
    public LayerMask groundMask;     // (유지) 기존 사용 가능
    public LayerMask occluderMask;   // 그림자 만드는 물체 레이어(큐브/벽)
    public LayerMask surfaceMask;    // ✅ "서 있을 수 있는 표면" (Ground + Occluder + Platform 등)

    [Header("Directional Light")]
    public Light sun;

    [Header("Indicator")]
    public GameObject shadowIndicator; // 발밑 표시

    [Header("Shadow Mode")]
    public Transform visualRoot;        // 스켈레탈(비주얼) 루트 연결!
    public float sinkVisualY = -0.35f;  // “땅에 박히는” 연출(비주얼만)
    public float shadowSpeedMul = 0.6f;
    public float maxDirDistance = 80f;

    [Header("Shadow Gauge (Dev)")]
    public float drainFullSeconds = 5f;
    public float regenFullSeconds = 10f;
    [Range(0f, 1f)] public float gauge01 = 1f;

    [Header("Input")]
    public int mouseButton = 1; // 우클릭 = 1

    bool inShadowMode;
    Vector3 visualOriginalLocalPos;

    public bool hasSurfaceAnchor { get; private set; }
    Vector3 anchorNormal;
    Collider anchorCollider;

    // ✅ PlayerController에서 읽을 수 있게 공개
    public Vector3 AnchorNormal => anchorNormal;

    // ✅ 벽 겹침이 심하면 이 값을 조금 키우세요 (0.05~0.15 추천)
    [Header("Anchor Visual Offset")]
    public float anchorSurfaceOffset = 0.08f;

    void Awake()
    {
        if (shadowIndicator) shadowIndicator.SetActive(false);

        if (visualRoot != null)
            visualOriginalLocalPos = visualRoot.localPosition;

        // surfaceMask가 비어있으면(세팅 안 했으면) 기존 groundMask로 폴백
        if (surfaceMask.value == 0)
            surfaceMask = groundMask;
    }

    void Update()
    {
        // ✅ [핵심] 현재 표면 포인트를 surfaceMask/앵커 기반으로 얻는다
        if (!TryGetCurrentSurfacePoint(out var gp, out var gn))
            return;

        bool onShadow = ShadowQueryDirectional.IsInShadow(gp, gn, sun, occluderMask, maxDistance: maxDirDistance);

        // 그림자 위에 서면 표시(그림자 모드가 아닐 때만)
        if (shadowIndicator && !inShadowMode)
            shadowIndicator.SetActive(onShadow);

        // ✅ 게이지 업데이트
        if (inShadowMode)
        {
            gauge01 -= Time.deltaTime / Mathf.Max(0.01f, drainFullSeconds);
            gauge01 = Mathf.Clamp01(gauge01);

            if (gauge01 <= 0f)
            {
                gauge01 = 0f;
                ExitShadowMode(); // 게이지 0이면 자동으로 나옴
            }

            // (기존 규칙 유지) 그림자 밖으로 나오면 자동으로 나옴
            if (!onShadow)
                ExitShadowMode();
        }
        else
        {
            gauge01 += Time.deltaTime / Mathf.Max(0.01f, regenFullSeconds);
            gauge01 = Mathf.Clamp01(gauge01);
        }

        // ✅ 우클릭 토글: 그림자 위에서만
        if (Input.GetMouseButtonDown(mouseButton) && onShadow)
        {
            if (!inShadowMode)
            {
                if (gauge01 > 0f) EnterShadowMode();
            }
            else
            {
                ExitShadowMode();
            }
        }
    }

    public bool IsInShadowMode => inShadowMode;
    public float SpeedMultiplier => inShadowMode ? shadowSpeedMul : 1f;
    public float Gauge01 => gauge01;

    void EnterShadowMode()
    {
        inShadowMode = true;

        if (visualRoot != null)
        {
            var p = visualOriginalLocalPos;
            p.y += sinkVisualY; // 비주얼만 내림
            visualRoot.localPosition = p;
        }

        if (shadowIndicator) shadowIndicator.SetActive(false);
    }

    void ExitShadowMode()
    {
        inShadowMode = false;

        if (visualRoot != null)
            visualRoot.localPosition = visualOriginalLocalPos;

        ClearSurfaceAnchor();
    }

    // =========================
    // ✅ 표면 포인트/노말 얻기
    // =========================
    bool TryGetCurrentSurfacePoint(out Vector3 point, out Vector3 normal)
    {
        // 1) 앵커가 있으면: 앵커 방향으로 표면 샘플 (벽/천장/플랫폼 대응)
        if (hasSurfaceAnchor && anchorCollider != null)
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

            // 앵커가 끊겼으면 해제
            ClearSurfaceAnchor();
        }

        // 2) 일반: 위에서 아래로 표면 찾기 (✅ surfaceMask 사용)
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

    // =========================
    // (옵션) 그림자 판정 API
    // =========================
    public bool IsShadowAtWorldPos(Vector3 worldPos)
    {
        Vector3 origin = worldPos + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, surfaceMask, QueryTriggerInteraction.Ignore))
        {
            return ShadowQueryDirectional.IsInShadow(hit.point, hit.normal, sun, occluderMask, maxDistance: maxDirDistance);
        }
        return false;
    }

    public bool IsShadowAtPoint(Vector3 p, Vector3 n)
    {
        return ShadowQueryDirectional.IsInShadow(p, n, sun, occluderMask, maxDistance: maxDirDistance);
    }

    // =========================
    // ✅ "안전 그림자" 판정(벽/천장 포함)
    // - 앵커가 있으면 앵커 표면에서 검사
    // - margin 오프셋은 표면 tangent(좌/우 + 상/하) 기준
    // =========================
    public bool IsShadowSafeAtWorldPos(Vector3 worldPos, float margin)
    {
        // 1) 앵커가 있으면 앵커 표면에서 검사
        if (hasSurfaceAnchor && anchorCollider != null)
        {
            Vector3 origin = worldPos + anchorNormal * 1.0f;
            Vector3 dir = -anchorNormal;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider == anchorCollider)
            {
                return IsShadowSafeAtPoint(hit.point, hit.normal, margin);
            }
        }

        // 2) 일반: 아래로 표면 찾기
        Vector3 originDown = worldPos + Vector3.up * 2f;
        if (!Physics.Raycast(originDown, Vector3.down, out RaycastHit hitDown, 10f, surfaceMask, QueryTriggerInteraction.Ignore))
            return false;

        return IsShadowSafeAtPoint(hitDown.point, hitDown.normal, margin);
    }

    static void BuildTangents(Vector3 n, out Vector3 t1, out Vector3 t2)
    {
        // n과 평행하지 않은 축 선택
        Vector3 a = (Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.95f) ? Vector3.right : Vector3.up;
        t1 = Vector3.Cross(n, a).normalized;
        t2 = Vector3.Cross(n, t1).normalized;
    }

    public bool IsShadowSafeAtPoint(Vector3 p, Vector3 n, float margin)
    {
        n = n.normalized;
        BuildTangents(n, out var t1, out var t2);

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
            Vector3 test = p + offsets[i];
            bool inShadow = ShadowQueryDirectional.IsInShadow(test, n, sun, occluderMask, maxDistance: maxDirDistance);
            if (!inShadow) return false;
        }

        return true;
    }

    public void ForceExitShadowMode()
    {
        if (inShadowMode)
            ExitShadowMode();
    }

    // =========================
    // 앵커(벽/천장/플랫폼 붙기)
    // =========================
    public void SetSurfaceAnchor(Vector3 normal, Collider col)
    {
        hasSurfaceAnchor = true;
        anchorNormal = normal.normalized;
        anchorCollider = col;
    }

    public void ClearSurfaceAnchor()
    {
        hasSurfaceAnchor = false;
        anchorCollider = null;
    }

    // ✅ 벽 겹침/떨어짐 보정: 붙어있는 표면으로 계속 스냅
    public void SnapToAnchoredSurface(Transform actor, float snapDistance = 2f)
    {
        if (!hasSurfaceAnchor || anchorCollider == null) return;

        Vector3 origin = actor.position + anchorNormal * 1.0f;
        Vector3 dir = -anchorNormal;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, snapDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != anchorCollider) return;

            // 살짝 바깥으로 띄워서 겹침 완화
            actor.position = hit.point + anchorNormal * anchorSurfaceOffset;
        }
    }

}
