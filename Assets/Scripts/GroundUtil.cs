using UnityEngine;

public static class GroundUtil
{
    // 오브젝트 아래 표면의 점과 법선을 반환한다.
    public static bool GetGroundPoint(Transform t, LayerMask groundMask, out Vector3 point, out Vector3 normal)
    {
        Vector3 origin = t.position + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }

        point = default;
        normal = Vector3.up;
        return false;
    }
}
