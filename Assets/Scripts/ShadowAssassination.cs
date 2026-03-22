using UnityEngine;

public class ShadowAssassination : MonoBehaviour
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

        bool hasValidLight = false;

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (!IsUsableLight(light)) continue;

            hasValidLight = true;

            // 하나라도 직접 비추면 그림자가 아님
            if (IsLitByLight(origin, light, occluderMask, maxDirDistance))
                return false;
        }

        // 유효 광원이 있고, 그 어떤 광원도 직접 비추지 못하면 그림자
        return hasValidLight;
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
