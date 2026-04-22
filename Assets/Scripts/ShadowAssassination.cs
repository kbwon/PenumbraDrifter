using UnityEngine;

public class ShadowAssassination : MonoBehaviour
{
    public ShadowInteractController shadowCtrl;
    public KeyCode assassinateKey = KeyCode.Mouse0;

    [Header("Tuning")]
    public float maxAssassinateDistance = 2.5f;
    public LayerMask enemyMask;

    public Animator anim;

    // 주변 적을 담아둘 배열이다.
    readonly Collider[] overlapResults = new Collider[16];

    void Awake()
    {
        // 필요한 참조를 자동으로 가져온다.
        if (!shadowCtrl) shadowCtrl = GetComponent<ShadowInteractController>();
        if (!anim) anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 그림자 모드일 때만 그림자 암살을 사용할 수 있다.
        if (!shadowCtrl) return;
        if (!shadowCtrl.IsInShadowMode) return;

        // 공격 키를 누른 순간만 처리한다.
        if (!Input.GetKeyDown(assassinateKey)) return;

        // 적이 없어도 공격 동작은 실행한다.
        if (anim != null)
            anim.SetTrigger("attack");

        // 주변에서 암살 가능한 가장 가까운 적을 찾는다.
        EnemyController target = FindNearbyEnemy();
        if (target == null) return;

        // 적이 가까이에 있으면 즉시 처치한다.
        target.KillByAssassination();
    }

    EnemyController FindNearbyEnemy()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            maxAssassinateDistance,
            overlapResults,
            enemyMask,
            QueryTriggerInteraction.Collide
        );

        EnemyController closest = null;
        float bestDistSqr = maxAssassinateDistance * maxAssassinateDistance;

        Vector3 myPos = transform.position;
        myPos.y = 0f;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCol = overlapResults[i];
            if (!hitCol) continue;

            // 자식 콜라이더에 닿아도 부모 EnemyController를 찾는다.
            EnemyController enemy = hitCol.GetComponentInParent<EnemyController>();
            if (enemy == null) continue;

            // 비활성화된 적은 제외한다.
            if (!enemy.gameObject.activeInHierarchy) continue;

            // 설정이 없거나 암살 불가 적이면 제외한다.
            if (enemy.config == null) continue;
            if (!enemy.config.canBeAssassinated) continue;

            Vector3 enemyPos = enemy.transform.position;
            enemyPos.y = 0f;

            float distSqr = (enemyPos - myPos).sqrMagnitude;
            if (distSqr > bestDistSqr) continue;

            closest = enemy;
            bestDistSqr = distSqr;
        }

        return closest;
    }

    void OnDrawGizmosSelected()
    {
        // 암살 가능 범위를 Scene 뷰에서 확인한다.
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, maxAssassinateDistance);
    }
}