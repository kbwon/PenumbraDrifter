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
    public float maxShadowTime = 5f;
    public float maxDirDistance = 80f;

    [Header("Input")]
    public int mouseButton = 1; // 우클릭 = 1

    bool inShadowMode;
    float timeLeft;

    Vector3 visualOriginalLocalPos;

    void Awake()
    {
        timeLeft = maxShadowTime;
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

        // 우클릭 토글: 그림자 위에서만
        if (Input.GetMouseButtonDown(mouseButton) && onShadow)
        {
            if (!inShadowMode) EnterShadowMode();
            else ExitShadowMode();
        }

        // 그림자 모드 유지/자동 종료
        if (inShadowMode)
        {
            timeLeft -= Time.deltaTime;

            // 빛으로 나가거나 시간이 끝나면 자동으로 나옴
            if (!onShadow || timeLeft <= 0f)
                ExitShadowMode();
        }
    }

    public bool IsInShadowMode => inShadowMode;
    public float SpeedMultiplier => inShadowMode ? shadowSpeedMul : 1f;
    public float TimeLeft01 => Mathf.Clamp01(timeLeft / maxShadowTime);

    void EnterShadowMode()
    {
        inShadowMode = true;
        timeLeft = maxShadowTime;

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
}
