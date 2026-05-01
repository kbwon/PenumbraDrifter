using System.Collections.Generic;
using UnityEngine;

public class CameraOcclusionDitherController : MonoBehaviour
{
    [Header("Refs")]
    public Transform cameraTransform;
    public Transform target;

    [Header("Detection")]
    public LayerMask occluderMask;
    public float sphereRadius = 0.45f;
    public float targetYOffset = 0.8f;

    [Header("Options")]
    public bool useGameManagerRefs = true;
    public bool ignoreTriggerColliders = true;

    readonly RaycastHit[] hits = new RaycastHit[64];

    readonly HashSet<OcclusionDitherTarget> currentTargets = new HashSet<OcclusionDitherTarget>();
    readonly HashSet<OcclusionDitherTarget> previousTargets = new HashSet<OcclusionDitherTarget>();

    void Awake()
    {
        ResolveRefs();
    }

    void LateUpdate()
    {
        ResolveRefs();

        if (!cameraTransform || !target)
            return;

        UpdateOccluders();
    }

    void OnDisable()
    {
        ClearAllCurrentTargets();
    }

    void ResolveRefs()
    {
        if (!useGameManagerRefs)
            return;

        if (!cameraTransform)
        {
            if (GameManager.Instance != null && GameManager.Instance.MainCameraTransform != null)
                cameraTransform = GameManager.Instance.MainCameraTransform;
            else if (Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        if (!target)
        {
            if (GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
                target = GameManager.Instance.PlayerTransform;
        }
    }

    void UpdateOccluders()
    {
        previousTargets.Clear();

        foreach (OcclusionDitherTarget item in currentTargets)
            previousTargets.Add(item);

        currentTargets.Clear();

        Vector3 origin = cameraTransform.position;
        Vector3 targetPoint = target.position + Vector3.up * targetYOffset;

        Vector3 toTarget = targetPoint - origin;
        float distance = toTarget.magnitude;

        if (distance <= 0.001f)
        {
            RestorePreviousTargets();
            return;
        }

        Vector3 dir = toTarget / distance;

        QueryTriggerInteraction triggerOption =
            ignoreTriggerColliders
                ? QueryTriggerInteraction.Ignore
                : QueryTriggerInteraction.Collide;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            sphereRadius,
            dir,
            hits,
            distance,
            occluderMask,
            triggerOption
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hits[i].collider;
            if (!col) continue;

            if (target != null)
            {
                if (col.transform == target || col.transform.IsChildOf(target))
                    continue;
            }

            OcclusionDitherTarget ditherTarget = col.GetComponentInParent<OcclusionDitherTarget>();
            if (!ditherTarget) continue;

            currentTargets.Add(ditherTarget);
        }

        foreach (OcclusionDitherTarget item in currentTargets)
        {
            if (!item) continue;

            item.SetOccluded(true);
            previousTargets.Remove(item);
        }

        RestorePreviousTargets();
    }

    void RestorePreviousTargets()
    {
        foreach (OcclusionDitherTarget item in previousTargets)
        {
            if (!item) continue;
            item.SetOccluded(false);
        }
    }

    void ClearAllCurrentTargets()
    {
        foreach (OcclusionDitherTarget item in currentTargets)
        {
            if (!item) continue;
            item.SetOccluded(false);
        }

        foreach (OcclusionDitherTarget item in previousTargets)
        {
            if (!item) continue;
            item.SetOccluded(false);
        }

        currentTargets.Clear();
        previousTargets.Clear();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Transform cam = cameraTransform;

        if (!cam && Camera.main)
            cam = Camera.main.transform;

        if (!cam || !target)
            return;

        Vector3 origin = cam.position;
        Vector3 targetPoint = target.position + Vector3.up * targetYOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, targetPoint);

        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(targetPoint, sphereRadius);
    }
#endif
}