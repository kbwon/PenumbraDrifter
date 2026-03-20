using UnityEngine;

public static class ShadowQuery
{
    // 단일 광원 기준으로 그림자인지 검사한다.
    public static bool IsPointInShadow(
        Vector3 point,
        Vector3 normal,
        Light light,
        LayerMask occluderMask,
        float eps = 0.03f,
        float maxDirDistance = 80f)
    {
        if (light == null || !light.enabled) return false;
        if (light.shadows == LightShadows.None) return false;

        Vector3 origin = point + normal.normalized * eps;
        return !IsLitByLight(origin, light, occluderMask, maxDirDistance);
    }

    // 여러 광원 중 하나라도 비추면 그림자가 아니다.
    public static bool IsPointInShadow(
        Vector3 point,
        Vector3 normal,
        Light[] lights,
        LayerMask occluderMask,
        float eps = 0.03f,
        float maxDirDistance = 80f)
    {
        if (lights == null || lights.Length == 0) return false;

        Vector3 origin = point + normal.normalized * eps;

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null || !light.enabled) continue;
            if (light.shadows == LightShadows.None) continue;

            if (IsLitByLight(origin, light, occluderMask, maxDirDistance))
                return false;
        }

        return true;
    }

    // 광원 타입별로 실제 조명 도달 여부를 계산한다.
    static bool IsLitByLight(
        Vector3 origin,
        Light light,
        LayerMask occluderMask,
        float maxDirDistance)
    {
        switch (light.type)
        {
            case LightType.Directional:
            {
                Vector3 toLight = -light.transform.forward;
                return !Physics.Raycast(origin, toLight, maxDirDistance, occluderMask, QueryTriggerInteraction.Ignore);
            }

            case LightType.Point:
            {
                Vector3 toLightVec = light.transform.position - origin;
                float dist = toLightVec.magnitude;
                if (dist > light.range) return false;

                Vector3 dir = toLightVec / dist;
                return !Physics.Raycast(origin, dir, dist, occluderMask, QueryTriggerInteraction.Ignore);
            }

            case LightType.Spot:
            {
                Vector3 toLightVec = light.transform.position - origin;
                float dist = toLightVec.magnitude;
                if (dist > light.range) return false;

                Vector3 dirToLight = toLightVec / dist;
                float cosHalf = Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad);
                float cosAng = Vector3.Dot(light.transform.forward, -dirToLight);
                if (cosAng < cosHalf) return false;

                return !Physics.Raycast(origin, dirToLight, dist, occluderMask, QueryTriggerInteraction.Ignore);
            }

            default:
                return false;
        }
    }
}
