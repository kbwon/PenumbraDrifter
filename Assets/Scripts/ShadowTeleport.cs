using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ShadowTeleport : MonoBehaviour
{
    [Header("Refs")]
    public ShadowInteractController shadowCtrl; // 같은 플레이어에 붙은 ShadowInteractController
    public Camera cam;                          // 비우면 Camera.main

    [Header("Raycast")]
    public LayerMask groundMask;                // (기존) 사용 안 해도 됨. 필요하면 유지
    public LayerMask surfaceMask;               // ✅ 클릭 가능한 표면(바닥/기둥/벽/천장/플랫폼 등 포함)
    public float rayMaxDistance = 200f;

    [Header("Teleport")]
    public float cooldownSeconds = 10f;         // 초기 10초
    public float maxTeleportDistance = 0f;      // 0이면 거리 제한 없음(추후 확장용)
    public float yLift = 0.02f;                 // 바닥에서 살짝 띄우기(파고듦 방지)

    float cooldownLeft;
    CharacterController cc;

    public bool IsReady => cooldownLeft <= 0f;
    public float Cooldown01 => Mathf.Clamp01(cooldownLeft / Mathf.Max(0.01f, cooldownSeconds)); // UI용

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!shadowCtrl) shadowCtrl = GetComponent<ShadowInteractController>();
        if (!cam && Camera.main) cam = Camera.main;

        // [추가] surfaceMask 안 넣었으면 기존 groundMask로 폴백
        if (surfaceMask.value == 0)
            surfaceMask = groundMask;
    }

    void Update()
    {
        // 쿨다운 감소
        if (cooldownLeft > 0f)
            cooldownLeft = Mathf.Max(0f, cooldownLeft - Time.deltaTime);

        if (!shadowCtrl || !cam) return;

        // 그림자 속(ShadowMode)일 때만 사용
        if (!shadowCtrl.IsInShadowMode) return;

        // 좌클릭(0)
        if (!Input.GetMouseButtonDown(0)) return;

        // 쿨타임 체크
        if (!IsReady) return;

        // 게이지 0이면 못 쓰게(원하면 조건 제거 가능)
        if (shadowCtrl.Gauge01 <= 0f) return;

        // 화면 클릭 지점 -> 표면 레이캐스트 (바닥/벽/천장/플랫폼 등)
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, surfaceMask, QueryTriggerInteraction.Ignore))
            return;

        // 목적지가 "안전 그림자"인지 검사(경계 걸침 방지 포함)
        // [변경] hit.point(표면점) 자체를 넣어도 되고, 아래처럼 월드포지션으로 넣어도 됨
        float margin = (cc != null) ? cc.radius * 0.9f : 0.35f;

        if (!shadowCtrl.IsShadowSafeAtPoint(hit.point, hit.normal, margin))
            return;

        // (추후 확장) 거리 제한
        if (maxTeleportDistance > 0f)
        {
            Vector3 a = transform.position; a.y = 0;
            Vector3 b = hit.point; b.y = 0;
            if (Vector3.Distance(a, b) > maxTeleportDistance) return;
        }

        // [변경] 순간이동 실행: 바닥뿐 아니라 벽/천장도 대응
        TeleportToSurface(hit);

        // 쿨다운 시작
        cooldownLeft = cooldownSeconds;
    }

    // ✅ [추가] 현재 마우스 위치가 "순간이동 가능한 그림자"인지 판정 + hit 반환
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

        float margin = (cc != null) ? cc.radius * 0.9f : 0.35f;

        // ✅ 벽/천장도 정확히 판정하려면 WorldPos가 아니라 Point/Normal 기반이 더 좋음
        if (!shadowCtrl.IsShadowSafeAtPoint(hit.point, hit.normal, margin))
            return false;

        if (maxTeleportDistance > 0f)
        {
            Vector3 a = transform.position; a.y = 0;
            Vector3 b = hit.point; b.y = 0;
            if (Vector3.Distance(a, b) > maxTeleportDistance) return false;
        }

        return true;
    }

    // (유지) 바닥 전용 텔레포트가 필요하면 남겨두되, 현재 흐름에서는 TeleportToSurface를 사용
    void TeleportToGroundPoint(Vector3 groundPoint)
    {
        Vector3 newPos = transform.position;
        newPos.x = groundPoint.x;
        newPos.z = groundPoint.z;

        float groundY = groundPoint.y;
        float desiredY = groundY + (cc.height * 0.5f - cc.center.y) + yLift;
        newPos.y = desiredY;

        cc.enabled = false;
        transform.position = newPos;
        cc.enabled = true;
    }

    void TeleportToSurface(RaycastHit hit)
    {
        Vector3 p = hit.point;
        Vector3 n = hit.normal.normalized;

        Vector3 newPos = transform.position;

        // 공통: XZ는 찍은 지점 기준으로
        newPos.x = p.x;
        newPos.z = p.z;

        float halfH = cc.height * 0.5f;
        float centerY = cc.center.y;

        // 1) 바닥(위쪽 노말): bottom을 바닥에 맞춤
        if (n.y > 0.7f)
        {
            newPos.y = p.y + (halfH - centerY) + yLift;
        }
        // 2) 천장(아래쪽 노말): top을 천장에 맞춤
        else if (n.y < -0.7f)
        {
            newPos.y = p.y - (centerY + halfH) - yLift;
        }
        // 3) 벽(수평 노말): “벽에 달라붙는” 연출용
        else
        {
            newPos.y = p.y + (halfH - centerY);
            newPos += n * (cc.radius + 0.02f);
        }

        cc.enabled = false;
        transform.position = newPos;
        cc.enabled = true;

        // ✅ 이 표면에 붙어있다는 정보(앵커)를 ShadowInteractController에 넘겨줌
        shadowCtrl.SetSurfaceAnchor(n, hit.collider);
    }
}
