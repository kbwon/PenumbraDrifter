using UnityEngine;

public class TestMovingObject : MonoBehaviour
{
    [Header("Path")]
    public Vector3 pointA;
    public Vector3 pointB;
    public bool useLocalOffsets = true;

    [Header("Motion")]
    public float speed = 2.0f;
    public float waitAtEnds = 0.0f;

    [Header("Options")]
    public bool faceMoveDirection = true;

    Vector3 aWorld;
    Vector3 bWorld;
    bool goingToB = true;
    float waitTimer;

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
        Vector3 before = transform.position;
        transform.position = Vector3.MoveTowards(before, target, speed * Time.deltaTime);

        if (faceMoveDirection)
        {
            Vector3 delta = transform.position - before;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.000001f)
                transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        }

        if ((transform.position - target).sqrMagnitude < 0.0001f)
        {
            goingToB = !goingToB;
            if (waitAtEnds > 0f)
                waitTimer = waitAtEnds;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 a = useLocalOffsets ? transform.position + pointA : pointA;
        Vector3 b = useLocalOffsets ? transform.position + pointB : pointB;

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(a, 0.15f);
        Gizmos.DrawSphere(b, 0.15f);
        Gizmos.DrawLine(a, b);
    }
#endif
}
