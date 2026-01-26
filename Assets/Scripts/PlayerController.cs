using UnityEngine;

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

    void Awake()
    {
        cc = GetComponent<CharacterController>();

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
        anim.SetBool("isRun", true);

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        // 카메라 기준 이동 (XZ 평면)
        Vector3 camF = cam ? cam.forward : Vector3.forward;
        Vector3 camR = cam ? cam.right : Vector3.right;
        camF.y = 0f; camR.y = 0f;
        camF.Normalize(); camR.Normalize();

        Vector3 dir = (camR * x + camF * z);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        cc.Move(dir * (moveSpeed * Time.deltaTime));

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
            float lr = Vector3.Dot(dir, camR); // +면 화면 기준 오른쪽, -면 왼쪽
            if (Mathf.Abs(lr) > 0.001f)
            {
                bool faceRight = lr > 0f;
                ApplyFlip(faceRight);
            }
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
