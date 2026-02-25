using UnityEngine;

public class ShadowAssassination : MonoBehaviour
{
    public ShadowInteractController shadowCtrl;  // PlayerRoot의 ShadowInteractController
    public KeyCode assassinateKey = KeyCode.Space;

    [Header("Tuning")]
    public float maxAssassinateDistance = 2.5f;  // 너무 멀리 있는 적 그림자로 오인 방지
    public float rayEps = 0.03f;                 // 레이 시작점 살짝 띄우기(바닥/자기충돌 방지)

    CharacterController cc;
    public Animator anim;

    void Awake()
    {
        if (!shadowCtrl) shadowCtrl = GetComponent<ShadowInteractController>();
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!shadowCtrl) return;

        // ✅ 반드시 "그림자 모드" 상태일 때만 암살 가능
        if (!shadowCtrl.IsInShadowMode) return;

        if (!Input.GetKeyDown(assassinateKey)) return;

        // 플레이어 발밑 지점
        if (!GroundUtil.GetGroundPoint(transform, shadowCtrl.groundMask, out var gp, out var gn))
            return;

        // 태양/마스크 확인
        if (!shadowCtrl.sun || shadowCtrl.sun.type != LightType.Directional) return;

        // 발밑에서 태양 방향으로 레이캐스트 → 빛을 막는 "첫 번째" 오브젝트가 적이면 적 그림자 안
        Vector3 origin = gp + gn * rayEps;
        Vector3 toLight = -shadowCtrl.sun.transform.forward;

        if (Physics.Raycast(origin, toLight, out RaycastHit hit, shadowCtrl.maxDirDistance,
                            shadowCtrl.occluderMask, QueryTriggerInteraction.Ignore))
        {
            // 적 찾기(자식 콜라이더일 수 있으니 parent까지)
            var killable = hit.collider.GetComponentInParent<EnemyKillable>();
            if (killable == null || !killable.canBeAssassinated) return;

            // 거리 제한(선택)
            Vector3 a = transform.position; a.y = 0;
            Vector3 b = killable.transform.position; b.y = 0;
            if (Vector3.Distance(a, b) > maxAssassinateDistance) return;

            // TODO: Player attack animation trigger here (스페이스 눌렀을 때 공격 애니메이션)
            // 예: anim.SetTrigger("assassinate");
            anim.SetBool("attack", true);

            killable.KillByAssassination();

            // 암살 후에도 그림자 모드 유지(요구사항)
            // 필요하면 여기서 추가 처리(예: 짧은 딜레이, 카메라 연출 등)
        }
    }
}
