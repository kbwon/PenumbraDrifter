using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ShadowTeleport : MonoBehaviour
{
    [Header("Refs")]
    public ShadowInteractController shadowCtrl; // 같은 플레이어에 붙은 ShadowInteractController
    public Camera cam;                          // 비우면 Camera.main

    [Header("Raycast")]
    public LayerMask groundMask;                // Plane/지형만 포함 (큐브/벽은 제외해야 바닥을 찍음)
    public LayerMask surfaceMask;
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

        // 화면 클릭 지점 -> 바닥 레이캐스트
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, rayMaxDistance, surfaceMask, QueryTriggerInteraction.Ignore))
            return;

        // 목적지가 "안전 그림자"인지 검사(경계 걸침 방지 포함)
        float margin = (cc != null) ? cc.radius * 0.9f : 0.35f;
        if (!shadowCtrl.IsShadowSafeAtWorldPos(hit.point, margin))
            return;

        // (추후 확장) 거리 제한
        if (maxTeleportDistance > 0f)
        {
            Vector3 a = transform.position; a.y = 0;
            Vector3 b = hit.point; b.y = 0;
            if (Vector3.Distance(a, b) > maxTeleportDistance) return;
        }

        // 순간이동 실행
        TeleportToGroundPoint(hit.point);

        // 쿨다운 시작
        cooldownLeft = cooldownSeconds;
    }

    void TeleportToGroundPoint(Vector3 groundPoint)
    {
        Vector3 newPos = transform.position;
        newPos.x = groundPoint.x;
        newPos.z = groundPoint.z;

        // CharacterController를 바닥에 정확히 올리기
        // bottom = pos.y + center.y - height/2  => bottom == groundY
        float groundY = groundPoint.y;
        float desiredY = groundY + (cc.height * 0.5f - cc.center.y) + yLift;
        newPos.y = desiredY;

        // CC가 콜라이더와 겹치면 이동이 꼬일 수 있으니 잠깐 껐다가 위치 지정
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

        // CharacterController 기준 값
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
            // top = pos.y + centerY + halfH  => top == ceilingY
            newPos.y = p.y - (centerY + halfH) - yLift;
        }
        // 3) 벽(수평 노말): “벽에 달라붙는” 연출용
        else
        {
            // Y는 클릭 지점 높이 기준으로 feet(바닥)를 맞춰줌(벽을 밟고 있는 느낌을 맞추기 쉬움)
            newPos.y = p.y + (halfH - centerY);

            // 벽 평면 안으로 파고들지 않게, 법선 방향으로 살짝 밀어냄(캡슐 반지름만큼)
            newPos += n * (cc.radius + 0.02f);
        }

        cc.enabled = false;
        transform.position = newPos;
        cc.enabled = true;

        // ✅ 이 표면에 붙어있다는 정보(앵커)를 ShadowInteractController에 넘겨줌
        shadowCtrl.SetSurfaceAnchor(n, hit.collider);
    }
}
