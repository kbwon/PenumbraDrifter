using UnityEngine;

public class TestMovingObject : MonoBehaviour
{
    [Header("Path (local or world)")]
    public Vector3 pointA;               // 시작점(월드 좌표로 쓰거나 아래 useLocal로 로컬로 쓸 수 있음)
    public Vector3 pointB;               // 끝점
    public bool useLocalOffsets = true;  // ✅ true면 현재 위치를 기준으로 pointA/pointB를 "오프셋"으로 사용

    [Header("Motion")]
    public float speed = 2.0f;           // 이동 속도 (units/sec)
    public float waitAtEnds = 0.0f;      // 끝에서 잠깐 멈춤(초)

    [Header("Options")]
    public bool faceMoveDirection = true; // 이동 방향으로 회전(캡슐이 앞으로 보게)

    Vector3 aWorld, bWorld;
    bool goingToB = true;
    float waitTimer = 0f;

    void Start()
    {
        if (useLocalOffsets)
        {
            aWorld = transform.position + pointA;
            bWorld = transform.position + pointB;
        }
        else
        {
            aWorld = pointA;
            bWorld = pointB;
        }
    }

    void Update()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        Vector3 target = goingToB ? bWorld : aWorld;

        // 이동
        Vector3 before = transform.position;
        transform.position = Vector3.MoveTowards(before, target, speed * Time.deltaTime);

        // 방향 회전(선택)
        if (faceMoveDirection)
        {
            Vector3 v = transform.position - before;
            v.y = 0f;
            if (v.sqrMagnitude > 0.000001f)
                transform.rotation = Quaternion.LookRotation(v.normalized, Vector3.up);
        }

        // 도착 체크
        if ((transform.position - target).sqrMagnitude < 0.0001f)
        {
            goingToB = !goingToB;
            if (waitAtEnds > 0f) waitTimer = waitAtEnds;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 에디터에서 경로 확인용
        Vector3 a = useLocalOffsets ? transform.position + pointA : pointA;
        Vector3 b = useLocalOffsets ? transform.position + pointB : pointB;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(a, 0.15f);
        Gizmos.DrawSphere(b, 0.15f);
        Gizmos.DrawLine(a, b);
    }
#endif
}
