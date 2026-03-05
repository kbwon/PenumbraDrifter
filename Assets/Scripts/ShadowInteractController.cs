using UnityEngine;

public class ShadowInteractController : MonoBehaviour
{
    [Header("Masks")]
    public LayerMask groundMask;     // Plane/지형 레이어
    public LayerMask occluderMask;   // 그림자 만드는 물체 레이어(큐브/벽)

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
    public float drainFullSeconds = 5f;   // ✅ 게이지가 1→0 되는 시간(초기 5초)
    public float regenFullSeconds = 10f;  // ✅ 게이지가 0→1 되는 시간(초기 10초)
    [Range(0f, 1f)] public float gauge01 = 1f; // 현재 게이지(0~1)

    [Header("Input")]
    public int mouseButton = 1; // 우클릭 = 1

    bool inShadowMode;
    Vector3 visualOriginalLocalPos;

    public bool hasSurfaceAnchor { get; private set; }
    Vector3 anchorNormal;
    Collider anchorCollider;

    void Awake()
    {
        if (shadowIndicator) shadowIndicator.SetActive(false);

        if (visualRoot != null)
            visualOriginalLocalPos = visualRoot.localPosition;
    }

    void Update()
    {
        if (!GroundUtil.GetGroundPoint(transform, groundMask, out var gp, out var gn))
            return;

        bool onShadow = ShadowQueryDirectional.IsInShadow(gp, gn, sun, occluderMask, maxDistance: maxDirDistance);

        // 그림자 위에 서면 표시(그림자 모드가 아닐 때만)
        if (shadowIndicator && !inShadowMode)
            shadowIndicator.SetActive(onShadow);

        // ✅ 게이지 업데이트
        if (inShadowMode)
        {
            gauge01 -= Time.deltaTime / Mathf.Max(0.01f, drainFullSeconds);
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
            if (gauge01 > 1f) gauge01 = 1f;
        }

        // ✅ 우클릭 토글: 그림자 위에서만
        // - 들어갈 때: 게이지가 0이면 진입 불가
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

    // UI에서 바로 쓰기 좋게 (0~1)
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

    // (옵션) 특정 월드 위치가 그림자인지 다른 스크립트에서 쓰고 싶을 때
    public bool IsShadowAtWorldPos(Vector3 worldPos)
    {
        // worldPos에서 바닥점을 다시 구해서 판정
        Vector3 origin = worldPos + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return ShadowQueryDirectional.IsInShadow(hit.point, hit.normal, sun, occluderMask, maxDistance: maxDirDistance);
        }
        return false;
    }

    public bool IsShadowSafeAtWorldPos(Vector3 worldPos, float margin)
    {
        // 바닥점 얻기
        Vector3 origin = worldPos + Vector3.up * 2f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
            return false;

        Vector3 p = hit.point;
        Vector3 n = hit.normal;

        // 중심 1점 + 주변 4점(십자)도 모두 그림자면 "안전"
        Vector3[] offsets =
        {
            Vector3.zero,
            new Vector3( margin, 0, 0),
            new Vector3(-margin, 0, 0),
            new Vector3(0, 0,  margin),
            new Vector3(0, 0, -margin),
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

    public void SnapToAnchoredSurface(Transform actor, float snapDistance = 2f)
    {
        if (!hasSurfaceAnchor || anchorCollider == null) return;

        // 표면 바깥쪽에서 표면 안쪽으로 레이
        Vector3 origin = actor.position + anchorNormal * 1.0f;
        Vector3 dir = -anchorNormal;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, snapDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            // 같은 콜라이더에 붙도록(정확도↑)
            if (hit.collider != anchorCollider) return;

            // 벽/천장일 경우: 살짝 바깥으로 띄워서 “붙어있게”
            actor.position = hit.point + anchorNormal * 0.05f;
        }
    }
}
