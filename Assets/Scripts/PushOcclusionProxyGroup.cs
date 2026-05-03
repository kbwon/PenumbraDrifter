using UnityEngine;

public class PushOcclusionProxyGroup : MonoBehaviour
{
    [Header("Proxy Objects")]
    public GameObject posX;
    public GameObject negX;
    public GameObject posZ;
    public GameObject negZ;

    [Header("Camera Rule")]
    public Camera cam;

    [Tooltip("켜면 플레이어가 카메라에 가까운 쪽에 있을 때만 가림면을 켭니다.")]
    public bool onlyWhenPlayerOnCameraNearSide = true;

    [Tooltip("값이 클수록 카메라 앞쪽이라고 더 확실히 판단될 때만 켭니다.")]
    [Range(-1f, 1f)] public float nearSideDot = 0.1f;

    [Header("Debug")]
    public bool debugLog;

    void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        HideAll();
    }

    public void HideAll()
    {
        SetActive(posX, false);
        SetActive(negX, false);
        SetActive(posZ, false);
        SetActive(negZ, false);
    }

    public void ShowFor(Transform player, Collider targetCollider)
    {
        HideAll();

        if (player == null || targetCollider == null)
            return;

        if (onlyWhenPlayerOnCameraNearSide && !IsPlayerOnCameraNearSide(player.position, targetCollider))
            return;

        Vector3 side = player.position - targetCollider.bounds.center;
        side.y = 0f;

        if (side.sqrMagnitude <= 0.0001f)
            return;

        if (Mathf.Abs(side.x) >= Mathf.Abs(side.z))
        {
            if (side.x >= 0f)
            {
                SetActive(posX, true);
                if (debugLog) Debug.Log("Show Occ_PosX");
            }
            else
            {
                SetActive(negX, true);
                if (debugLog) Debug.Log("Show Occ_NegX");
            }
        }
        else
        {
            if (side.z >= 0f)
            {
                SetActive(posZ, true);
                if (debugLog) Debug.Log("Show Occ_PosZ");
            }
            else
            {
                SetActive(negZ, true);
                if (debugLog) Debug.Log("Show Occ_NegZ");
            }
        }
    }

    bool IsPlayerOnCameraNearSide(Vector3 playerPos, Collider targetCollider)
    {
        Camera targetCam = cam != null ? cam : Camera.main;

        if (targetCam == null)
            return true;

        Vector3 side = playerPos - targetCollider.bounds.center;
        side.y = 0f;

        Vector3 camForward = targetCam.transform.forward;
        camForward.y = 0f;

        if (side.sqrMagnitude <= 0.0001f || camForward.sqrMagnitude <= 0.0001f)
            return false;

        side.Normalize();
        camForward.Normalize();

        // 카메라에 가까운 쪽은 camForward의 반대 방향입니다.
        return Vector3.Dot(side, -camForward) > nearSideDot;
    }

    void SetActive(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active)
            obj.SetActive(active);
    }
}