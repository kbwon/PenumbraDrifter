using UnityEngine;

public class EnemyAnimationEventRelay : MonoBehaviour
{
    [SerializeField] EnemyController enemy;

    void Awake()
    {
        if (!enemy)
            enemy = GetComponentInParent<EnemyController>();
    }

    public void Anim_AttackHit()
    {
        if (!enemy)
            enemy = GetComponentInParent<EnemyController>();

        if (enemy)
            enemy.Anim_AttackHit();
    }

    public void Anim_AttackEnd()
    {
        if (!enemy)
            enemy = GetComponentInParent<EnemyController>();

        if (enemy)
            enemy.Anim_AttackEnd();
    }

    public void Anim_RangedFire()
    {
        if (!enemy)
            enemy = GetComponentInParent<EnemyController>();

        RangedEnemyController ranged = enemy as RangedEnemyController;
        if (ranged)
            ranged.Anim_RangedFire();
    }
}