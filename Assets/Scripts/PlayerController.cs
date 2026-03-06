using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 4.5f;
    public Transform cam;                 // 비우면 메인 카메라
    public Transform visualBillboard;     // (선택) 카메라 바라보게 할 부모
    public Transform flipRoot;            // (필수) 좌/우 뒤집을 트랜스폼
    public Animator anim;                // 비우면 자식에서 자동 탐색

    [Header("Facing / Flip")]
    public bool artFacesRight = true;     // 아트의 기본 정면이 오른쪽이면 true, 왼쪽이면 false

    private CharacterController cc;

    public float gravity = -25f;
    float verticalVel;

    // [추가] 매 프레임 GetComponent 호출 줄이기(선택이지만 안정적)
    ShadowInteractController shadowCtrl;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        shadowCtrl = GetComponent<ShadowInteractController>(); // [추가]

        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;

        if (anim == null)
            anim = GetComponentInChildren<Animator>(); // Skeletal은 자식에 있는 경우가 많음

        // flipRoot가 비어있으면 안전하게 VisualBillboard 또는 자기 자신을 사용(권장: Inspector에서 연결)
        if (flipRoot == null)
            flipRoot = visualBillboard != null ? visualBillboard : transform;

        // 시작 방향이 반대로 보이면 artFacesRight를 반대로 바꾸면 해결되는 구조
        ApplyFlip(true); // 기본은 "오른쪽을 정면"으로 시작(아트 기준은 artFacesRight로 보정)
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        bool inShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;
        bool anchored = inShadow && shadowCtrl.hasSurfaceAnchor;

        Vector3 dir;
        Vector3 camRForFlip = cam ? cam.right : Vector3.right;
        camRForFlip.y = 0f;
        camRForFlip.Normalize();

        if (anchored)
        {
            Vector3 n = shadowCtrl.AnchorNormal.normalized;

            // 표면 평면 위의 "좌/우" = 카메라 right를 표면에 투영
            Vector3 rightOnSurface = (cam ? cam.right : Vector3.right);
            rightOnSurface = rightOnSurface - Vector3.Dot(rightOnSurface, n) * n;
            if (rightOnSurface.sqrMagnitude < 0.0001f)
                rightOnSurface = Vector3.Cross(Vector3.up, n); // 폴백
            rightOnSurface.Normalize();

            // 표면 평면 위의 "상/하" = 월드 up을 표면에 투영 (벽에서 위로 '등반'이 안정적)
            Vector3 upOnSurface = Vector3.up - Vector3.Dot(Vector3.up, n) * n;

            // 천장처럼 n이 up/down에 가까우면 upOnSurface가 0에 수렴할 수 있음 → 그땐 cam.forward 투영으로 폴백
            if (upOnSurface.sqrMagnitude < 0.0001f)
            {
                upOnSurface = (cam ? cam.forward : Vector3.forward);
                upOnSurface = upOnSurface - Vector3.Dot(upOnSurface, n) * n;
            }
            upOnSurface.Normalize();

            dir = rightOnSurface * x + upOnSurface * z;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
        }
        else
        {
            // 일반 바닥 이동(XZ)
            Vector3 camF = cam ? cam.forward : Vector3.forward;
            Vector3 camR = cam ? cam.right : Vector3.right;
            camF.y = 0f;
            camR.y = 0f;
            camF.Normalize();
            camR.Normalize();

            dir = camR * x + camF * z;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
        }

        float speed = moveSpeed * (shadowCtrl ? shadowCtrl.SpeedMultiplier : 1f);
        Vector3 horizontalMove = dir * (speed * Time.deltaTime);

        // [유지] 중력: 그림자 모드에서는 OFF, 밖에서는 ON
        if (inShadow)
        {
            verticalVel = 0f;
        }
        else
        {
            if (cc.isGrounded && verticalVel < 0f) verticalVel = -2f;
            verticalVel += gravity * Time.deltaTime;
        }

        Vector3 move = horizontalMove + Vector3.up * (verticalVel * Time.deltaTime);
        cc.Move(move);

        // [유지] 앵커가 있으면 표면에 붙도록 스냅(벽/천장/플랫폼에서 “옆에 서있는” 느낌 완화)
        if (inShadow && shadowCtrl.hasSurfaceAnchor)
        {
            shadowCtrl.SnapToAnchoredSurface(transform);
        }

        // [유지] 그림자 모드 상태에서 영역을 벗어나면 튕겨나오기
        if (inShadow)
        {
            float margin = cc != null ? cc.radius * 0.9f : 0.35f;
            if (!shadowCtrl.IsShadowSafeAtWorldPos(transform.position, margin))
                shadowCtrl.ForceExitShadowMode();
        }

        // isRun: 입력(또는 dir)로 한 번만 결정
        bool isRun = dir.sqrMagnitude > 0.0001f;
        if (anim != null)
            anim.SetBool("isRun", isRun);

        // (선택) 빌보드: 비주얼만 카메라 바라보게
        if (visualBillboard != null && cam != null)
        {
            Vector3 toCam = cam.position - visualBillboard.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.0001f)
                visualBillboard.forward = toCam.normalized;
        }

        // 좌/우 바라보기: "이동 방향이 카메라 오른쪽(+)/왼쪽(-) 중 어디냐"로 결정
        if (isRun)
        {
            float lr = Vector3.Dot(dir, camRForFlip); // +면 화면 기준 오른쪽, -면 왼쪽
            if (Mathf.Abs(lr) > 0.001f)
            {
                bool faceRight = lr > 0f;
                ApplyFlip(faceRight);
            }
        }

        // 디버그: K 키로 1칸 데미지
        if (Input.GetKeyDown(KeyCode.K))
        {
            GetComponent<PlayerHealth>().TakeDamage(1);
        }
    }

    void ApplyFlip(bool faceRight)
    {
        if (flipRoot == null) return;

        // 아트 기본 정면이 왼쪽이면 여기서 반대로 보정
        if (!artFacesRight) faceRight = !faceRight;

        Vector3 s = flipRoot.localScale;
        s.x = Mathf.Abs(s.x) * (faceRight ? 1f : -1f);
        flipRoot.localScale = s;
    }
}
