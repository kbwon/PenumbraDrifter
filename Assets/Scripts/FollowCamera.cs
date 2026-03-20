using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 10f, -15f);
    public Vector3 lookAtOffset = new Vector3(0f, 6f, 0f);

    public float followSmooth = 12f;
    public float rotateSmooth = 18f;

    public float stepAngle = 45f;
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;

    float targetYaw;
    float currentYaw;

    void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterFollowCamera(this);

        Camera cam = GetComponent<Camera>();
        if (cam != null && GameManager.Instance != null)
            GameManager.Instance.RegisterMainCamera(cam);
    }

    void Start()
    {
        currentYaw = transform.eulerAngles.y;
        targetYaw = currentYaw;

        if (target == null && GameManager.Instance != null)
            target = GameManager.Instance.PlayerTransform;
    }

    void LateUpdate()
    {
        if (target == null && GameManager.Instance != null)
            target = GameManager.Instance.PlayerTransform;

        if (!target) return;

        // 카메라를 45도 단위로 회전한다.
        if (Input.GetKeyDown(rotateLeftKey)) targetYaw += stepAngle;
        if (Input.GetKeyDown(rotateRightKey)) targetYaw -= stepAngle;

        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotateSmooth * Time.deltaTime);
        Quaternion yawRot = Quaternion.Euler(0f, currentYaw, 0f);

        Vector3 desiredPos = target.position + yawRot * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSmooth * Time.deltaTime);

        Vector3 lookPoint = target.position + lookAtOffset;
        transform.rotation = Quaternion.LookRotation((lookPoint - transform.position).normalized, Vector3.up);
    }
}
