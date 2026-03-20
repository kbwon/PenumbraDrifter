using UnityEngine;

public class FaceCameraY : MonoBehaviour
{
    Transform cam;

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.MainCameraTransform != null)
            cam = GameManager.Instance.MainCameraTransform;
        else if (Camera.main != null)
            cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!cam)
        {
            if (GameManager.Instance != null && GameManager.Instance.MainCameraTransform != null)
                cam = GameManager.Instance.MainCameraTransform;
            else if (Camera.main != null)
                cam = Camera.main.transform;
        }

        if (!cam) return;

        // Y축만 돌려서 캐릭터가 눕지 않게 한다.
        Vector3 toCam = cam.position - transform.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude < 0.0001f) return;

        transform.forward = toCam.normalized;
    }
}
