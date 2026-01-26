using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 10f, -15f);  // 위/뒤로
    public Vector3 lookAtOffset = new Vector3(0f, 6f, 0f);

    public float followSmooth = 12f;
    public float rotateSmooth = 18f;

    public float stepAngle = 45f;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    float targetYaw;
    float currentYaw;

    void Start()
    {
        currentYaw = transform.eulerAngles.y;
        targetYaw = currentYaw;
    }

    void LateUpdate()
    {
        if (!target) return;

        // 입력: 45도씩 회전
        if (Input.GetKeyDown(rotateLeftKey)) targetYaw += stepAngle;
        if (Input.GetKeyDown(rotateRightKey)) targetYaw -= stepAngle;

        // 부드러운 Yaw 보간
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotateSmooth * Time.deltaTime);
        Quaternion yawRot = Quaternion.Euler(0f, currentYaw, 0f);

        // 위치: 타겟 + (회전된 오프셋)
        Vector3 desiredPos = target.position + yawRot * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSmooth * Time.deltaTime);

        // 바라보기
        Vector3 lookPoint = target.position + lookAtOffset;
        transform.rotation = Quaternion.LookRotation((lookPoint - transform.position).normalized, Vector3.up);
    }
}
