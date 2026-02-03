using UnityEngine;

public static class ShadowQuery
{
    // point: 바닥 위 판정할 지점
    // normal: 바닥 법선(평평하면 Vector3.up)
    // lights: 씬에서 사용할 Light들(보통 Directional 1개 + 필요한 만큼)
    // occluderMask: 빛을 막는 오브젝트 레이어(벽/상자/건물 등). 플레이어/트리거는 제외 권장.
    public static bool IsPointInShadow(
        Vector3 point,
        Vector3 normal,
        Light[] lights,
        LayerMask occluderMask,
        float eps = 0.03f,
        float maxDirDistance = 80f)
    {
        // 원점이 바닥에 너무 붙으면 자기 자신을 치는 경우가 있어서 살짝 띄움
        Vector3 origin = point + normal * eps;

        bool litByAny = false;

        foreach (var l in lights)
        {
            if (l == null || !l.enabled) continue;

            // 라이트가 그림자를 꺼둔 경우는 "게임 규칙 라이트"에서 제외하는 게 보통 자연스러움
            if (l.shadows == LightShadows.None) continue;

            if (IsLitByLight(origin, point, l, occluderMask, maxDirDistance))
            {
                litByAny = true;
                break;
            }
        }

        return !litByAny;
    }

    static bool IsLitByLight(
        Vector3 origin,
        Vector3 point,
        Light l,
        LayerMask occluderMask,
        float maxDirDistance)
    {
        switch (l.type)
        {
            case LightType.Directional:
                {
                    // Unity에서 Light는 transform.forward 방향으로 “비추는” 편이므로,
                    // 지점->광원 방향은 -forward 로 보면 안전합니다.
                    Vector3 toLight = -l.transform.forward;

                    // 시각적 shadow distance와 맞추고 싶으면 maxDirDistance를 URP Shadow Distance로 맞추세요.
                    if (Physics.Raycast(origin, toLight, out RaycastHit hit, maxDirDistance, occluderMask, QueryTriggerInteraction.Ignore))
                    {
                        // 중간에 뭔가 맞으면 그 라이트는 가려짐(=그림자)
                        return false;
                    }
                    return true; // 막힘이 없으면 밝음
                }

            case LightType.Point:
                {
                    Vector3 toLightVec = (l.transform.position - origin);
                    float dist = toLightVec.magnitude;
                    if (dist > l.range) return false; // 범위 밖이면 이 라이트 영향 없음(=lit 아님)

                    Vector3 dir = toLightVec / dist;
                    if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, occluderMask, QueryTriggerInteraction.Ignore))
                        return false; // 막힘

                    return true;
                }

            case LightType.Spot:
                {
                    Vector3 toLightVec = (l.transform.position - origin);
                    float dist = toLightVec.magnitude;
                    if (dist > l.range) return false;

                    Vector3 dirToLight = toLightVec / dist;

                    // 스포트 각도 체크(라이트 forward 방향으로 비추는 원뿔)
                    float cosHalf = Mathf.Cos(l.spotAngle * 0.5f * Mathf.Deg2Rad);
                    float cosAng = Vector3.Dot(l.transform.forward, -dirToLight);
                    if (cosAng < cosHalf) return false; // 스포트 원뿔 밖

                    if (Physics.Raycast(origin, dirToLight, out RaycastHit hit, dist, occluderMask, QueryTriggerInteraction.Ignore))
                        return false;

                    return true;
                }

            default:
                return false;
        }
    }
}