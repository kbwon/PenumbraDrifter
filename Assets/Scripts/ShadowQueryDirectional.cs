using UnityEngine;

public static class ShadowQueryDirectional
{
    // sun: Directional Light (태양)
    // point: 바닥 지점
    // normal: 바닥 법선
    // occluderMask: 빛을 막는 레이어(벽/상자 등)
    public static bool IsInShadow(Vector3 point, Vector3 normal, Light sun, LayerMask occluderMask,
                                  float eps = 0.03f, float maxDistance = 80f)
    {
        if (sun == null || !sun.enabled) return false;
        if (sun.type != LightType.Directional) return false;
        if (sun.shadows == LightShadows.None) return false;

        Vector3 origin = point + normal * eps;

        // 태양이 비추는 방향의 반대(-forward) 쪽으로 레이 쏴서 막히면 그림자
        Vector3 toLight = -sun.transform.forward;

        bool blocked = Physics.Raycast(origin, toLight, maxDistance, occluderMask, QueryTriggerInteraction.Ignore);
        return blocked;
    }
}
