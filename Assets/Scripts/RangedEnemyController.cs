using UnityEngine;

public class RangedEnemyController : EnemyController
{
    [Header("Ranged")]
    public Transform muzzle;
    public EnemyProjectile projectilePrefab;

    [Header("3D Projectile Aim")]
    public bool aimAtPlayerColliderCenter = true;
    public float projectileAimExtraYOffset = 0f;
    public float fallbackAimYOffset = 0.8f;

    [Header("Muzzle Offset")]
    public bool useProceduralMuzzle = true;
    public float muzzleHeight = 1.2f;
    public float muzzleForwardOffset = 0.75f;
    public float muzzleSideOffset = 0f;

    [Header("Muzzle Visual")]
    public bool useScreenSpaceMuzzleOffsets = true;

    // 총을 들고 있는 손 쪽으로 좌우 보정
    public float muzzleScreenSideOffset = 0.42f;

    // 플레이어가 화면상 위/아래에 있을 때 총구를 그 방향으로 조금 더 보정
    public float muzzleScreenDepthOffset = 0.18f;

    // 좌/우 끝으로 쏠 때 총구 끝으로 조금 더 붙이는 추가 보정
    public float muzzleDirectionalSideBias = 0.08f;

    [Header("Aim Gate")]
    [Range(-1f, 1f)] public float minFireFacingDot = 0.92f;



    protected RangedEnemyConfig rangedConfig;
    protected float lastFireTime = -999f;

    protected Vector3 lastKnownTargetPos;
    protected bool hasLastKnownTargetPos;

    Vector3 pendingShootDir = Vector3.forward;

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
        if (projectilePrefab == null) return false;
        return true;
    }

    protected override void UpdateChaseState()
    {
        if (vision.CanSeeNow)
        {
            notSeenTimer = 0f;
            lastKnownTargetPos = player.position;
            hasLastKnownTargetPos = true;

            MoveOrAttack(player.position, true);
            return;
        }

        notSeenTimer += Time.deltaTime;

        if (notSeenTimer >= config.loseChaseAfterNotSeenSeconds)
        {
            EnterVisualAlertState(1f);
            vision.ResetDetection();
            return;
        }

        if (hasLastKnownTargetPos)
            MoveOrAttack(lastKnownTargetPos, false);
        else
            StopMove();
    }

    protected void MoveOrAttack(Vector3 targetPos, bool canSeeTarget)
    {
        Vector3 moveDir = GetFlatDirectionTo(targetPos, out float distance);

        FaceDirection(moveDir);

        // 멈춰서 조준할 때도 비주얼 플립이 이 방향을 따라가게 한다.
        if (moveDir.sqrMagnitude > 0.0001f)
            lastMoveDir = moveDir;

        if (distance > rangedConfig.attackRange)
        {
            MoveToPosition(targetPos, config.moveSpeed, rangedConfig.attackRange);
            return;
        }

        StopMove();

        bool canKeepFiring =
            canSeeTarget ||
            notSeenTimer <= rangedConfig.fireAfterLostSightSeconds;

        // 아직 충분히 몸을 안 돌렸으면 이번 프레임은 회전만 하고 발사는 안 한다.
        if (!IsFacingDirectionEnough(moveDir))
            return;

        if (canKeepFiring)
            TryStartShoot(moveDir);
    }

    protected virtual void TryStartShoot(Vector3 shootDir)
    {
        if (isAttacking) return;
        if (Time.time - lastFireTime < rangedConfig.fireCooldown) return;

        if (shootDir.sqrMagnitude <= 0.0001f)
            shootDir = transform.forward;

        pendingShootDir = shootDir.normalized;
        
        lastMoveDir = pendingShootDir;

        lastFireTime = Time.time;

        isAttacking = true;
        attackDamageDone = false;

        float lockSeconds = Mathf.Max(0.01f, config.attackLockSeconds);
        attackEndTime = Time.time + lockSeconds;

        StopMove();
        ZeroHorizontalVelocity();

        if (anim != null)
            anim.SetBool(walkBoolName, false);

        TriggerAttackAnim();
    }

    protected override void UpdateAttackLock()
    {
        StopMove();
        ZeroHorizontalVelocity();

        if (pendingShootDir.sqrMagnitude > 0.0001f)
            FaceDirection(pendingShootDir);

        if (Time.time >= attackEndTime)
            EndAttack();
    }

    public void Anim_RangedFire()
    {
        if (!isAttacking) return;
        FireProjectile(pendingShootDir);
    }

    protected virtual void FireProjectile(Vector3 shootDir)
    {
        if (projectilePrefab == null || rangedConfig == null)
            return;

        if (shootDir.sqrMagnitude <= 0.0001f)
            shootDir = transform.forward;

        // 총구 위치 계산과 적 회전은 수평 방향 기준으로 유지합니다.
        // 그래야 스프라이트가 위/아래로 기울지 않습니다.
        Vector3 flatShootDir = shootDir;
        flatShootDir.y = 0f;

        if (flatShootDir.sqrMagnitude <= 0.0001f)
            flatShootDir = transform.forward;

        flatShootDir.y = 0f;
        flatShootDir.Normalize();

        Vector3 spawnPos = GetProjectileSpawnPosition(flatShootDir);

        // 실제 총알 방향은 플레이어 Collider 중심을 향하게 합니다.
        Vector3 aimPoint = GetProjectileAimPoint();
        Vector3 finalShootDir = aimPoint - spawnPos;

        if (finalShootDir.sqrMagnitude <= 0.0001f)
            finalShootDir = flatShootDir;
        else
            finalShootDir.Normalize();

        EnemyProjectile projectile = Instantiate(
            projectilePrefab,
            spawnPos,
            Quaternion.LookRotation(finalShootDir, Vector3.up)
        );

        projectile.Initialize(
            finalShootDir,
            rangedConfig.projectileSpeed,
            rangedConfig.projectileDamagePips,
            rangedConfig.projectileLifeTime,
            playerTag,
            bodyCollider
        );
    }

    bool IsFacingDirectionEnough(Vector3 dir)
    {
        if (dir.sqrMagnitude <= 0.0001f)
            return true;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        dir.y = 0f;
        dir.Normalize();

        return Vector3.Dot(forward, dir) >= minFireFacingDot;
    }

    Vector3 GetProjectileSpawnPosition(Vector3 shootDir)
    {
        if (!useProceduralMuzzle && muzzle != null)
            return muzzle.position + shootDir * 0.15f;

        Vector3 spawn = transform.position + Vector3.up * muzzleHeight;

        // 총알이 몸 안에서 시작하지 않게 하는 실제 전방 오프셋
        spawn += shootDir * muzzleForwardOffset;

        if (!useScreenSpaceMuzzleOffsets || cam == null)
            return spawn;

        Vector3 camRight = cam.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 camForwardOnGround = cam.forward;
        camForwardOnGround.y = 0f;
        camForwardOnGround.Normalize();

        float handSign = GetWeaponHandScreenSign();

        // 1) 기본적으로 총을 든 손 쪽으로 이동
        spawn += camRight * (muzzleScreenSideOffset * handSign);

        // 2) 플레이어가 화면 위/아래에 있을수록 총구를 그 방향으로 더 보내기
        float depthDot = Vector3.Dot(shootDir, camForwardOnGround);
        spawn += camForwardOnGround * (muzzleScreenDepthOffset * depthDot);

        // 3) 좌/우로 강하게 쏠 때 총구 끝쪽으로 살짝 더 밀기
        float sideDot = Vector3.Dot(shootDir, camRight);
        spawn += camRight * (muzzleDirectionalSideBias * sideDot);

        return spawn;
    }

    float GetWeaponHandScreenSign()
    {
        float sign = 1f;

        if (flipRoot != null)
            sign = Mathf.Sign(flipRoot.lossyScale.x);

        if (!artFacesRight)
            sign *= -1f;

        return sign;
    }

    Vector3 GetProjectileAimPoint()
    {
        if (aimAtPlayerColliderCenter && player != null)
        {
            Collider bestCollider = FindBestPlayerCollider();

            if (bestCollider != null)
                return bestCollider.bounds.center + Vector3.up * projectileAimExtraYOffset;
        }

        if (player != null)
            return player.position + Vector3.up * fallbackAimYOffset;

        return transform.position + transform.forward;
    }

    Collider FindBestPlayerCollider()
    {
        if (player == null)
            return null;

        Collider[] colliders = player.GetComponentsInChildren<Collider>(true);

        Collider best = null;
        float bestScore = -1f;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col == null) continue;
            if (!col.enabled) continue;
            if (!col.gameObject.activeInHierarchy) continue;

            // Trigger 전용 판정 콜라이더는 조준 기준에서 제외하는 편이 안전합니다.
            if (col.isTrigger) continue;

            Vector3 size = col.bounds.size;
            float score = size.x * size.y * size.z;

            if (score > bestScore)
            {
                bestScore = score;
                best = col;
            }
        }

        return best;
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