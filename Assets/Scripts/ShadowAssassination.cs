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
        if (!shadowCtrl)
            shadowCtrl = GetComponent<ShadowInteractController>();

        if (!shadowCtrl && GameManager.Instance != null)
            shadowCtrl = GameManager.Instance.shadow;

        if (!anim)
            anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!shadowCtrl) return;
        if (!shadowCtrl.IsInShadowMode) return;
        if (!Input.GetKeyDown(assassinateKey)) return;
        if (!shadowCtrl.TryGetCurrentSurfacePoint(out Vector3 point, out Vector3 normal)) return;
        if (!shadowCtrl.sun || shadowCtrl.sun.type != LightType.Directional) return;

        Vector3 origin = point + normal * rayEps;
        Vector3 toLight = -shadowCtrl.sun.transform.forward;

        // 현재 위치 그림자의 주인을 찾아 암살 대상을 찾는다.
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
