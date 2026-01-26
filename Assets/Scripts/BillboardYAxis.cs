using UnityEngine;

public class BillboardYAxis : MonoBehaviour
{
    Transform cam;

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (!cam) return;

        Vector3 toCam = cam.position - transform.position;
        toCam.y = 0f; // Y축 회전만 (기울어짐 방지)
        if (toCam.sqrMagnitude < 0.0001f) return;

        transform.forward = toCam.normalized;
    }
}
