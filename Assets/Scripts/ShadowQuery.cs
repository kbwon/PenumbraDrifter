using UnityEngine;

public static class ShadowQuery
{
    public static bool IsPointInShadow(
         Vector3 point,
         Vector3 normal,
         Light light,
         LayerMask occluderMask,
         float eps = 0.03f,
         float maxDirDistance = 80f)
    {
        if (!IsUsableLight(light)) return false;

        Vector3 safeNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
        Vector3 origin = point + safeNormal * eps;

        // 이 광원의 영향권 밖이면 그냥 "그림자 아님"
        if (!IsPointInsideLightInfluence(origin, light))
            return false;

        // 영향권 안인데 직접 비추면 밝음, 아니면 가려진 그림자
        return !IsLitByLight(origin, light, occluderMask, maxDirDistance);
    }

    public static bool IsPointInShadow(
        Vector3 point,
        Vector3 normal,
        Light[] lights,
        LayerMask occluderMask,
        float eps = 0.03f,
        float maxDirDistance = 80f)
    {
        if (lights == null || lights.Length == 0) return false;

        Vector3 safeNormal = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
        Vector3 origin = point + safeNormal * eps;

        bool hasRelevantLight = false;

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (!IsUsableLight(light)) continue;

            // 이 점이 이 광원의 영향권 안에 있는지 먼저 확인
            if (!IsPointInsideLightInfluence(origin, light))
                continue;

            hasRelevantLight = true;

            // 영향권 안의 광원 중 하나라도 직접 비추면 그림자 아님
            if (IsLitByLight(origin, light, occluderMask, maxDirDistance))
                return false;
        }

        // 영향권 안인 광원이 하나도 없으면 "그냥 어두움" -> 이동 불가
        if (!hasRelevantLight)
            return false;

        // 영향권 안인 광원은 있었지만, 전부 가려졌다면 그림자
        return true;
    }

    static bool IsUsableLight(Light light)
    {
        if (light == null || !light.isActiveAndEnabled) return false;
        if (light.shadows == LightShadows.None) return false;
        if (light.intensity <= 0f) return false;

        switch (light.type)
        {
            case LightType.Directional:
            case LightType.Point:
            case LightType.Spot:
                return true;
            default:
                return false;
        }
    }

    static bool IsPointInsideLightInfluence(Vector3 origin, Light light)
    {
        switch (light.type)
        {
            case LightType.Directional:
                // Directional은 전역 광원
                return true;

            case LightType.Point:
                {
                    float dist = Vector3.Distance(origin, light.transform.position);
                    return dist <= light.range;
                }

            case LightType.Spot:
                {
                    Vector3 toPoint = origin - light.transform.position;
                    float dist = toPoint.magnitude;
                    if (dist > light.range || dist <= 0.0001f) return false;

                    Vector3 dirToPoint = toPoint / dist;
                    float cosHalf = Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad);
                    float cosAng = Vector3.Dot(light.transform.forward.normalized, dirToPoint);

                    return cosAng >= cosHalf;
                }

            default:
                return false;
        }
    }

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
                    Vector3 toLight = -light.transform.forward.normalized;
                    return !Physics.Raycast(origin, toLight, maxDirDistance, occluderMask, QueryTriggerInteraction.Ignore);
                }

            case LightType.Point:
                {
                    Vector3 toLightVec = light.transform.position - origin;
                    float dist = toLightVec.magnitude;
                    if (dist <= 0.0001f || dist > light.range) return false;

                    Vector3 dir = toLightVec / dist;
                    return !Physics.Raycast(origin, dir, dist, occluderMask, QueryTriggerInteraction.Ignore);
                }

            case LightType.Spot:
                {
                    Vector3 toLightVec = light.transform.position - origin;
                    float dist = toLightVec.magnitude;
                    if (dist <= 0.0001f || dist > light.range) return false;

                    Vector3 lightToPoint = -toLightVec / dist;
                    float cosHalf = Mathf.Cos(light.spotAngle * 0.5f * Mathf.Deg2Rad);
                    float cosAng = Vector3.Dot(light.transform.forward.normalized, lightToPoint);
                    if (cosAng < cosHalf) return false;

                    Vector3 dir = toLightVec / dist;
                    return !Physics.Raycast(origin, dir, dist, occluderMask, QueryTriggerInteraction.Ignore);
                }

            default:
                return false;
        }
    }
}
