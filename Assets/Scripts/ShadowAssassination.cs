using UnityEngine;

public class ShadowAssassination : MonoBehaviour
{
    public ShadowInteractController shadowCtrl;
    public KeyCode assassinateKey = KeyCode.Space;

    [Header("Tuning")]
    public float maxAssassinateDistance = 2.5f;
    public float rayEps = 0.03f;

    public Animator anim;

    void Awake()
    {
        if (!shadowCtrl) shadowCtrl = GetComponent<ShadowInteractController>();
        if (!anim) anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!shadowCtrl) return;
        if (!shadowCtrl.IsInShadowMode) return;
        if (!Input.GetKeyDown(assassinateKey)) return;
        if (!shadowCtrl.TryGetCurrentSurfacePoint(out var surfacePoint, out var surfaceNormal)) return;
        if (!shadowCtrl.sun || shadowCtrl.sun.type != LightType.Directional) return;

        Vector3 origin = surfacePoint + surfaceNormal * rayEps;
        Vector3 toLight = -shadowCtrl.sun.transform.forward;

        if (Physics.Raycast(origin, toLight, out RaycastHit hit, shadowCtrl.maxDirDistance,
            shadowCtrl.occluderMask, QueryTriggerInteraction.Ignore))
        {
            EnemyKillable killable = hit.collider.GetComponentInParent<EnemyKillable>();
            if (killable == null || !killable.canBeAssassinated) return;

            Vector3 a = transform.position;
            Vector3 b = killable.transform.position;
            a.y = 0f;
            b.y = 0f;

            if (Vector3.Distance(a, b) > maxAssassinateDistance) return;

            if (anim != null)
                anim.SetBool("attack", true);

            killable.KillByAssassination();
        }
    }
}
