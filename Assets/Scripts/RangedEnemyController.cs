using UnityEngine;

public class RangedEnemyController : EnemyController
{
    [Header("Ranged")]
    public Transform muzzle;
    public EnemyProjectile projectilePrefab;

    protected RangedEnemyConfig rangedConfig;
    protected float lastFireTime = -999f;

    // 마지막으로 플레이어를 본 위치
    protected Vector3 lastKnownTargetPos;
    protected bool hasLastKnownTargetPos;

    protected override void Start()
    {
        base.Start();

        rangedConfig = config as RangedEnemyConfig;
        if (config != null && rangedConfig == null)
            Debug.LogWarning($"{name}: RangedEnemyController에는 RangedEnemyConfig를 넣어야 합니다.");

        if (player != null)
        {
            lastKnownTargetPos = player.position;
            hasLastKnownTargetPos = true;
        }
    }

    protected override bool HasRequiredRefs()
    {
        if (!base.HasRequiredRefs()) return false;
        if (rangedConfig == null) return false;
        if (muzzle == null) return false;
        if (projectilePrefab == null) return false;
        return true;
    }

    protected override void UpdateChaseState()
    {
        // 지금 플레이어가 보이면 마지막 위치 갱신
        if (vision.CanSeeNow)
        {
            notSeenTimer = 0f;
            lastKnownTargetPos = player.position;
            hasLastKnownTargetPos = true;

            MoveOrAttack(player.position, true);
            return;
        }

        // 안 보이면 시간 누적
        notSeenTimer += Time.deltaTime;

        // 너무 오래 못 보면 추적 종료
        if (notSeenTimer >= config.loseChaseAfterNotSeenSeconds)
        {
            state = State.LostWait;
            lostWaitTimer = config.waitAfterLost;
            notSeenTimer = 0f;
            StopMove();
            vision.ResetDetection();
            return;
        }

        // 아직은 플레이어를 놓친 것으로 확정하지 않았으므로
        // 마지막으로 본 위치를 기준으로 계속 행동
        if (hasLastKnownTargetPos)
            MoveOrAttack(lastKnownTargetPos, false);
        else
            StopMove();
    }

    protected void MoveOrAttack(Vector3 targetPos, bool canSeeTarget)
    {
        Vector3 moveDir = GetFlatDirectionTo(targetPos, out float distance);

        FaceDirection(moveDir);

        // 사정거리 밖이면 계속 접근
        if (distance > rangedConfig.attackRange)
        {
            MoveInDirection(moveDir, config.moveSpeed);
            return;
        }

        // 사정거리 안이면 멈추고 사격
        StopMove();

        bool canKeepFiring =
            canSeeTarget ||
            notSeenTimer <= rangedConfig.fireAfterLostSightSeconds;

        if (canKeepFiring)
            TryFire(moveDir);
    }

    protected virtual void TryFire(Vector3 shootDir)
    {
        if (Time.time - lastFireTime < rangedConfig.fireCooldown)
            return;

        if (shootDir.sqrMagnitude <= 0.0001f)
            shootDir = transform.forward;

        lastFireTime = Time.time;

        Vector3 spawnPos = muzzle.position + shootDir * 0.35f;

        EnemyProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPos,
            Quaternion.LookRotation(shootDir, Vector3.up)
        );

        projectile.Initialize(
            shootDir,
            rangedConfig.projectileSpeed,
            rangedConfig.projectileDamagePips,
            rangedConfig.projectileLifeTime,
            playerTag,
            cc
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (config is RangedEnemyConfig rc)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, rc.attackRange);
        }
    }
#endif
}