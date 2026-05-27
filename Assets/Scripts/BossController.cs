using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class BossController : EnemyController
{
    enum BossAction
    {
        None,
        Punch,
        WindupToCharge,
        WindupToGroundSlam,
        ChargeStart,
        Charge,
        ChargeEnd,
        GroundSlam,
        ShadowGrab,
        Hurt,
        Dead
    }

    [Header("Boss")]
    public bool startActive = true;
    public BossConfig bossConfig;

    [Header("Optional VFX")]
    public GameObject shockwavePrefab;
    public Transform shockwaveSpawnPoint;

    [Header("Animator Triggers")]
    public string punchTrigger = "punch";
    public string chargeWindupTrigger = "chargeWindup";
    public string chargeLoopTrigger = "chargeLoop";
    public string chargeEndTrigger = "chargeEnd";
    public string groundSlamTrigger = "groundSlam";
    public string shadowGrabTrigger = "shadowGrab";
    public string hurtTrigger = "hurt";
    public string deathTrigger = "death";

    [Header("StateMachineBehaviour Fallback")]
    public float windupFallbackSeconds = 3f;
    public float punchFallbackSeconds = 2f;
    public float groundSlamFallbackSeconds = 3f;
    public float chargeEndFallbackSeconds = 3f;
    public float shadowGrabFallbackSeconds = 3f;
    public float hurtFallbackSeconds = 2f;

    [Header("Action Fallback Safety")]
    public float actionFallbackRecheckSeconds = 0.25f;
    public bool warnOnActionFallback = true;

    [Header("Shadow Grab Player Reaction")]
    public string playerGrabbedTrigger = "hurt";
    public string playerGrabbedStateName = "";
    public float playerGrabFreezeDelay = 0.08f;
    public bool freezePlayerAnimatorDuringThrow = true;
    public float shadowGrabThrowDuration = 0.35f;
    public float shadowGrabThrowHeight = 0.6f;

    [Header("Animator Recovery")]
    public string bossIdleStateName = "Base Layer.BossIdle";
    public string bossWalkStateName = "Base Layer.WalkLoop";
    public bool forceLocomotionStateOnActionEnd = true;
    public float actionEndCrossFadeSeconds = 0.05f;

    [Header("Action State Sync")]
    public float minActionStateSyncSeconds = 0.15f;
    public string bossIdleShortStateName = "BossIdle";
    public string bossWalkShortStateName = "WalkLoop";
    public string bossIdleToWalkShortStateName = "IdletoWalk";

    [Header("Windup Lock")]
    public bool lockRootDuringWindup = true;
    public bool lockRotationDuringWindup = true;
    public bool warpAgentWhileWindupLocked = true;

    [Header("ShadowGrab Priority")]
    public bool prioritizeShadowGrabOverPunch = true;
    public float shadowGrabRequestGraceSeconds = 0.35f;

    [Header("Telegraph")]
    public BossGroundSlamTelegraph groundSlamTelegraph;
    public float groundSlamTelegraphSeconds = 0.9f;

    ShadowInteractController pendingShadowGrabTarget;
    float lastShadowGrabRequestTime = -999f;

    Vector3 windupLockedPosition;
    Vector3 windupLockedForward;
    bool windupLocked;

    float actionStartTime;

    Rigidbody playerRb;
    Animator playerAnim;
    Coroutine playerThrowRoutine;
    Coroutine playerFreezeRoutine;
    float cachedPlayerAnimSpeed = 1f;
    bool playerAnimFrozen;

    int hp;
    bool combatActive;
    bool vulnerableToShadowAssassination;
    bool actionDamageDone;

    BossAction currentAction = BossAction.None;
    BossAction queuedAfterWindup = BossAction.None;

    float lastPunchTime = -999f;
    float lastGroundSlamTime = -999f;
    float lastChargeTime = -999f;
    float lastShadowGrabTime = -999f;

    float actionEndTime;
    Vector3 chargeDir;

    public BossChargeTelegraph chargeTelegraph;
    public float chargeTelegraphSeconds = 0.9f;
    public float chargeTelegraphWidth = 2.2f;

    PlayerController playerController;

    public System.Action<bool> OnVulnerableChanged;
    public bool IsVulnerable => vulnerableToShadowAssassination;

    protected override void Start()
    {
        base.Start();

        bossConfig = config as BossConfig;

        if (bossConfig == null)
            Debug.LogWarning($"{name}: BossController에는 BossConfig를 넣어야 합니다.", this);

        hp = bossConfig != null ? bossConfig.maxHp : 3;
        combatActive = startActive;

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerRb = player.GetComponent<Rigidbody>();
            playerAnim = player.GetComponentInChildren<Animator>();
        }

        if (groundSlamTelegraph != null && groundSlamTelegraph.groundMask.value == 0)
            groundSlamTelegraph.groundMask = groundMask;

        if (chargeTelegraph != null && chargeTelegraph.groundMask.value == 0)
            chargeTelegraph.groundMask = groundMask;
    }

    protected override bool HasRequiredRefs()
    {
        bool navReady = !useNavMeshMovement || agent != null;
        return bossConfig != null && player != null && rb != null && bodyCollider != null && navReady;
    }

    protected override void Update()
    {
        if (!HasRequiredRefs()) return;

        desiredVelocity = Vector3.zero;

        if (!combatActive || currentAction == BossAction.Dead)
        {
            StopMove();
            UpdateAnim();
            UpdateFlip(lastMoveDir);
            return;
        }

        if (isAttacking)
        {
            UpdateBossAction();
            UpdateAnim();
            UpdateFlip(lastMoveDir);
            return;
        }

        UpdateBossCombat();

        UpdateAnim();
        UpdateFlip(lastMoveDir);
    }

    void UpdateBossCombat()
    {
        if (prioritizeShadowGrabOverPunch &&
            HasValidPendingShadowGrab() &&
            CanStartShadowGrabNow())
        {
            StartShadowGrabAction(pendingShadowGrabTarget);
            return;
        }

        Vector3 dir = GetFlatDirectionTo(player.position, out float distance);

        if (dir.sqrMagnitude > 0.0001f)
            FaceDirection(dir);

        if (distance <= bossConfig.punchRange && Time.time - lastPunchTime >= bossConfig.punchCooldown)
        {
            StartPunch();
            return;
        }

        if (distance <= bossConfig.groundSlamRange && Time.time - lastGroundSlamTime >= bossConfig.groundSlamCooldown)
        {
            StartGroundSlamWindup();
            return;
        }

        if (distance >= bossConfig.chargeMinRange &&
            distance <= bossConfig.chargeMaxRange &&
            Time.time - lastChargeTime >= bossConfig.chargeCooldown)
        {
            StartChargeWindup();
            return;
        }

        MoveToPosition(player.position, bossConfig.moveSpeed, bossConfig.punchRange * 0.9f);
    }

    void BeginAction(BossAction action, string triggerName, float duration)
    {
        currentAction = action;
        isAttacking = true;
        actionDamageDone = false;
        vulnerableToShadowAssassination = false;

        actionStartTime = Time.time;
        actionEndTime = Time.time + Mathf.Max(0.05f, duration);

        StopMove();
        ZeroHorizontal();

        if (anim != null)
        {
            anim.SetBool(walkBoolName, false);

            if (!string.IsNullOrEmpty(triggerName))
                anim.SetTrigger(triggerName);
        }
    }

    void StartPunch()
    {
        lastPunchTime = Time.time;
        BeginAction(BossAction.Punch, punchTrigger, punchFallbackSeconds);
    }

    void StartGroundSlamWindup()
    {
        lastGroundSlamTime = Time.time;

        if (groundSlamTelegraph != null)
        {
            groundSlamTelegraph.Begin(
                transform.position,
                bossConfig.groundSlamRadius,
                groundSlamTelegraphSeconds
            );
        }

        Vector3 slamDir = GetFlatDirectionTo(player.position, out _);
        if (slamDir.sqrMagnitude <= 0.0001f)
            slamDir = transform.forward;

        queuedAfterWindup = BossAction.GroundSlam;
        BeginAction(BossAction.WindupToGroundSlam, chargeWindupTrigger, windupFallbackSeconds);

        BeginWindupLock(slamDir);
    }

    void StartChargeWindup()
    {
        lastChargeTime = Time.time;

        chargeDir = GetFlatDirectionTo(player.position, out _);
        if (chargeDir.sqrMagnitude <= 0.0001f)
            chargeDir = transform.forward;

        queuedAfterWindup = BossAction.Charge;
        BeginAction(BossAction.WindupToCharge, chargeWindupTrigger, windupFallbackSeconds);

        // 돌진 준비 중에는 위치와 방향을 고정합니다.
        BeginWindupLock(chargeDir);

        if (chargeTelegraph != null)
        {
            float length = bossConfig.chargeSpeed * bossConfig.chargeDuration;

            chargeTelegraph.Begin(
                transform.position,
                chargeDir,
                length,
                chargeTelegraphWidth,
                chargeTelegraphSeconds
            );
        }
    }

    void StartChargeLoop()
    {
        currentAction = BossAction.ChargeStart;
        isAttacking = true;
        actionDamageDone = false;
        vulnerableToShadowAssassination = false;

        actionStartTime = Time.time;
        actionEndTime = Time.time + windupFallbackSeconds;

        StopMove();
        ZeroHorizontal();

        if (chargeDir.sqrMagnitude > 0.0001f)
            FaceDirection(chargeDir);

        if (anim != null)
        {
            anim.SetBool(walkBoolName, false);

            if (!string.IsNullOrEmpty(chargeLoopTrigger))
                anim.SetTrigger(chargeLoopTrigger);
        }
    }

    void StartChargeEnd()
    {
        BeginAction(BossAction.ChargeEnd, chargeEndTrigger, chargeEndFallbackSeconds);
    }

    void StartGroundSlam()
    {
        EndWindupLock();
        BeginAction(BossAction.GroundSlam, groundSlamTrigger, groundSlamFallbackSeconds);
    }

    void StartHurt()
    {
        BeginAction(BossAction.Hurt, hurtTrigger, hurtFallbackSeconds);
    }

    void StartDeath()
    {
        currentAction = BossAction.Dead;
        isAttacking = true;
        combatActive = false;
        vulnerableToShadowAssassination = false;

        StopMove();
        ZeroHorizontal();

        if (agent != null)
            agent.isStopped = true;

        if (anim != null)
            anim.SetTrigger(deathTrigger);
    }

    void UpdateBossAction()
    {
        StopMove();
        ZeroHorizontal();

        if (currentAction == BossAction.WindupToCharge ||
            currentAction == BossAction.WindupToGroundSlam ||
            currentAction == BossAction.ChargeStart)
        {
            MaintainWindupLock();
        }

        if (currentAction == BossAction.Charge)
        {
            UpdateChargeMove();

            if (Time.time >= actionEndTime)
            {
                StartChargeEnd();
                return;
            }

            return;
        }

        if (Time.time >= actionEndTime)
        {
            if (currentAction == BossAction.WindupToCharge ||
                currentAction == BossAction.WindupToGroundSlam)
            {
                Debug.LogWarning($"{name}: Boss windup callback was not called. action={currentAction}", this);
                Anim_BossWindupEnd();
                return;
            }

            // 애니메이션이 아직 재생 중이면 강제 종료하지 않고 조금 더 기다립니다.
            if (WaitIfAnimatorStateStillPlaying())
                return;

            AnimatorStateInfo stateInfo = anim != null
                ? anim.GetCurrentAnimatorStateInfo(0)
                : default;

            if (warnOnActionFallback)
            {
                Debug.LogWarning(
                    $"{name}: Boss action end callback was not called. " +
                    $"action={currentAction}, normalizedTime={stateInfo.normalizedTime:0.00}, stateHash={stateInfo.shortNameHash}",
                    this
                );
            }

            ForceEndActionByFallback();
        }
    }

    void UpdateChargeMove()
    {
        if (chargeDir.sqrMagnitude <= 0.0001f)
            return;

        Vector3 delta = chargeDir.normalized * bossConfig.chargeSpeed * Time.deltaTime;

        if (IsUsingNavMesh())
        {
            agent.Move(delta);
        }
        else if (rb != null)
        {
            rb.MovePosition(rb.position + delta);
        }
        else
        {
            transform.position += delta;
        }

        lastMoveDir = chargeDir.normalized;

        TryChargeHitPlayer();
    }

    void TryChargeHitPlayer()
    {
        if (actionDamageDone) return;
        if (playerHealth == null || playerHealth.isDead) return;

        Vector3 a = transform.position;
        Vector3 b = player.position;
        a.y = 0f;
        b.y = 0f;

        float distance = Vector3.Distance(a, b);

        if (distance <= bossConfig.chargeHitRadius)
        {
            actionDamageDone = true;
            playerHealth.TakeDamage(bossConfig.chargeDamagePips);
        }
    }

    void EndBossAction()
    {
        HideAllTelegraphs();

        EndWindupLock();

        isAttacking = false;
        currentAction = BossAction.None;
        queuedAfterWindup = BossAction.None;
        vulnerableToShadowAssassination = false;
        actionDamageDone = false;
    }

    void ZeroHorizontal()
    {
        if (IsUsingNavMesh())
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.velocity = Vector3.zero;
            }

            return;
        }

        if (rb == null) return;

        if (rb.isKinematic) return;

        Vector3 v = rb.linearVelocity;
        v.x = 0f;
        v.z = 0f;
        rb.linearVelocity = v;
    }

    void BeginWindupLock(Vector3 faceDir)
    {
        windupLocked = true;
        windupLockedPosition = transform.position;

        faceDir.y = 0f;

        if (faceDir.sqrMagnitude <= 0.0001f)
            faceDir = transform.forward;

        faceDir.y = 0f;
        faceDir.Normalize();

        windupLockedForward = faceDir;

        if (lockRotationDuringWindup && windupLockedForward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(windupLockedForward, Vector3.up);

        if (IsUsingNavMesh())
        {
            agent.isStopped = true;

            if (agent.hasPath)
                agent.ResetPath();

            agent.velocity = Vector3.zero;
            agent.nextPosition = transform.position;
        }
    }

    void MaintainWindupLock()
    {
        if (!windupLocked)
            return;

        if (lockRootDuringWindup)
        {
            if (IsUsingNavMesh())
            {
                if (warpAgentWhileWindupLocked)
                {
                    Vector3 diff = transform.position - windupLockedPosition;
                    diff.y = 0f;

                    if (diff.sqrMagnitude > 0.000001f)
                        agent.Warp(windupLockedPosition);
                    else
                        agent.nextPosition = windupLockedPosition;
                }

                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
            else if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    Vector3 v = rb.linearVelocity;
                    v.x = 0f;
                    v.z = 0f;
                    rb.linearVelocity = v;
                }

                rb.position = windupLockedPosition;
            }
            else
            {
                transform.position = windupLockedPosition;
            }
        }

        if (lockRotationDuringWindup && windupLockedForward.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(windupLockedForward, Vector3.up);
    }

    void EndWindupLock()
    {
        windupLocked = false;

        if (IsUsingNavMesh())
        {
            agent.nextPosition = transform.position;
            agent.velocity = Vector3.zero;
        }
    }

    public void SetCombatActive(bool active)
    {
        combatActive = active;

        if (!active)
        {
            StopMove();
            ZeroHorizontal();
        }
    }
    public override bool CanStartShadowAssassination()
    {
        if (currentAction == BossAction.Dead)
            return false;

        return vulnerableToShadowAssassination;
    }
    public override void KillByAssassination()
    {
        if (currentAction == BossAction.Dead)
            return;

        if (!vulnerableToShadowAssassination)
        {
            TryStartShadowGrab();
            return;
        }

        hp--;

        if (hp <= 0)
        {
            StartDeath();
        }
        else
        {
            StartHurt();
        }
    }

    public void TryStartShadowGrab(ShadowInteractController targetShadow = null)
    {
        if (targetShadow != null && targetShadow.IsInShadowMode)
        {
            pendingShadowGrabTarget = targetShadow;
            lastShadowGrabRequestTime = Time.time;
        }

        if (!CanStartShadowGrabNow())
            return;

        StartShadowGrabAction(targetShadow != null ? targetShadow : pendingShadowGrabTarget);
    }

    public void Anim_BossWindupEnd()
    {
        if (!isAttacking) return;

        if (queuedAfterWindup == BossAction.Charge)
        {
            StartChargeLoop();
            return;
        }

        if (queuedAfterWindup == BossAction.GroundSlam)
        {
            StartGroundSlam();
            return;
        }
    }

    public void Anim_BossChargeMoveStart()
    {
        if (!isAttacking) return;
        if (currentAction != BossAction.ChargeStart &&
            currentAction != BossAction.WindupToCharge)
            return;

        EndWindupLock();

        currentAction = BossAction.Charge;
        actionDamageDone = false;
        vulnerableToShadowAssassination = false;

        actionStartTime = Time.time;

        float duration = bossConfig != null ? bossConfig.chargeDuration : 0.75f;
        actionEndTime = Time.time + Mathf.Max(0.05f, duration);

        StopMove();
        ZeroHorizontal();

        if (IsUsingNavMesh())
        {
            agent.isStopped = false;
            agent.ResetPath();
            agent.nextPosition = transform.position;
            agent.velocity = Vector3.zero;
        }

        if (chargeDir.sqrMagnitude > 0.0001f)
            FaceDirection(chargeDir);

        if (chargeTelegraph != null)
            chargeTelegraph.CompleteAndHide();
    }

    public void Anim_BossPunchHit()
    {
        if (!isAttacking) return;
        if (currentAction != BossAction.Punch) return;
        if (actionDamageDone) return;
        if (playerHealth == null || playerHealth.isDead) return;

        Vector3 a = transform.position;
        Vector3 b = player.position;
        a.y = 0f;
        b.y = 0f;

        if (Vector3.Distance(a, b) <= bossConfig.punchRange + 0.3f)
        {
            actionDamageDone = true;
            playerHealth.TakeDamage(bossConfig.punchDamagePips);
        }
    }

    public void Anim_BossGroundSlamHit()
    {
        if (!isAttacking) return;
        if (currentAction != BossAction.GroundSlam) return;
        if (actionDamageDone) return;

        if (groundSlamTelegraph != null)
            groundSlamTelegraph.CompleteAndHide();

        if (shockwavePrefab != null)
        {
            Vector3 pos = shockwaveSpawnPoint != null
                ? shockwaveSpawnPoint.position
                : transform.position;

            Instantiate(shockwavePrefab, pos, Quaternion.identity);
        }

        if (playerHealth == null || playerHealth.isDead)
            return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        if (distance > bossConfig.groundSlamRadius)
            return;

        actionDamageDone = true;
        playerHealth.TakeDamage(bossConfig.groundSlamDamagePips);
    }

    public void Anim_BossShadowGrabPull()
    {
        if (playerController != null)
        {
            playerController.ForceNormalModeAfterExternalShadowExit(false);
        }
        else if (playerShadow != null)
        {
            playerShadow.ForceExitShadowMode();
        }

        PlayPlayerGrabbedPose();

        if (playerFreezeRoutine != null)
            StopCoroutine(playerFreezeRoutine);

        playerFreezeRoutine = StartCoroutine(FreezePlayerAnimatorAfterDelay());
    }

    public void Anim_BossShadowGrabThrow()
    {
        if (playerThrowRoutine != null)
            StopCoroutine(playerThrowRoutine);

        playerThrowRoutine = StartCoroutine(ShadowGrabThrowRoutine());
    }

    public void Anim_BossVulnerableStart()
    {
        vulnerableToShadowAssassination = true;
        OnVulnerableChanged?.Invoke(true);
    }

    public void Anim_BossVulnerableEnd()
    {
        vulnerableToShadowAssassination = false;
        OnVulnerableChanged?.Invoke(false);
    }

    public void Anim_BossActionEnd()
    {
        if (!isAttacking && currentAction == BossAction.None)
            return;

        bool wasDead = currentAction == BossAction.Dead;
        bool wasShadowGrab = currentAction == BossAction.ShadowGrab;

        if (wasShadowGrab)
        {
            RestorePlayerAnimatorSpeed();

            if (playerThrowRoutine == null && playerController != null)
                playerController.ForceNormalModeAfterExternalShadowExit(true);
        }

        EndBossAction();

        if (!wasDead)
            ReturnAnimatorToLocomotion();
    }

    public void Anim_BossDeathEnd()
    {
        StopMove();
        ZeroHorizontal();
        // 여기서 BossStageDirector에 클리어 알림을 보내는 구조로 확장하면 됩니다.
    }

    bool HasAnimatorParameter(Animator targetAnim, string paramName, AnimatorControllerParameterType type)
    {
        if (targetAnim == null || string.IsNullOrEmpty(paramName))
            return false;

        AnimatorControllerParameter[] parameters = targetAnim.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == paramName && parameters[i].type == type)
                return true;
        }

        return false;
    }

    void PlayPlayerGrabbedPose()
    {
        if (playerAnim == null)
            return;

        playerAnim.speed = 1f;

        SafeSetPlayerBool("isWalk", false);
        SafeSetPlayerBool("Idle", false);
        SafeSetPlayerBool("isShadowWalk", false);
        SafeSetPlayerBool("ShadowIdle", false);
        SafeSetPlayerBool("isCrouching", false);
        SafeSetPlayerBool("isCrouchMoving", false);
        SafeSetPlayerBool("isPushing", false);
        SafeSetPlayerBool("isPushMoving", false);

        if (!string.IsNullOrEmpty(playerGrabbedStateName))
        {
            playerAnim.CrossFadeInFixedTime(playerGrabbedStateName, 0.02f, 0);
            playerAnim.Update(0f);
            return;
        }

        if (!string.IsNullOrEmpty(playerGrabbedTrigger) &&
            HasAnimatorParameter(playerAnim, playerGrabbedTrigger, AnimatorControllerParameterType.Trigger))
        {
            playerAnim.ResetTrigger(playerGrabbedTrigger);
            playerAnim.SetTrigger(playerGrabbedTrigger);
            playerAnim.Update(0f);
        }
    }

    void SafeSetPlayerBool(string paramName, bool value)
    {
        if (playerAnim == null) return;

        AnimatorControllerParameter[] parameters = playerAnim.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == paramName &&
                parameters[i].type == AnimatorControllerParameterType.Bool)
            {
                playerAnim.SetBool(paramName, value);
                return;
            }
        }
    }

    IEnumerator FreezePlayerAnimatorAfterDelay()
    {
        yield return new WaitForSeconds(playerGrabFreezeDelay);

        if (playerAnim == null) yield break;
        if (!freezePlayerAnimatorDuringThrow) yield break;

        if (!playerAnimFrozen)
        {
            cachedPlayerAnimSpeed = playerAnim.speed;
            playerAnim.speed = 0f;
            playerAnimFrozen = true;
        }
    }

    void RestorePlayerAnimatorSpeed()
    {
        if (playerAnim == null) return;

        if (playerAnimFrozen)
        {
            playerAnim.speed = cachedPlayerAnimSpeed;
            playerAnimFrozen = false;
        }
    }

    void SetPlayerPosition(Vector3 pos)
    {
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.position = pos;
        }
        else if (player != null)
        {
            player.position = pos;
        }
    }

    IEnumerator ShadowGrabThrowRoutine()
    {
        if (player == null)
            yield break;

        Vector3 start = player.position;

        Vector3 dir = start - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = transform.forward;

        dir.Normalize();

        Vector3 end = start + dir * bossConfig.shadowGrabThrowDistance;
        end.y = start.y;

        float duration = Mathf.Max(0.05f, shadowGrabThrowDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            Vector3 pos = Vector3.Lerp(start, end, u);
            pos.y += Mathf.Sin(u * Mathf.PI) * shadowGrabThrowHeight;

            SetPlayerPosition(pos);

            yield return null;
        }

        SetPlayerPosition(end);

        RestorePlayerAnimatorSpeed();

        if (playerController != null)
            playerController.ForceNormalModeAfterExternalShadowExit(true);

        playerThrowRoutine = null;
    }

    bool WaitIfAnimatorStateStillPlaying()
    {
        if (anim == null)
            return false;

        if (anim.IsInTransition(0))
        {
            actionEndTime = Time.time + actionFallbackRecheckSeconds;
            return true;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // Non-loop 상태가 아직 95%도 안 갔다면 fallback을 실행하지 않고 더 기다립니다.
        if (!stateInfo.loop && stateInfo.normalizedTime < 0.95f)
        {
            actionEndTime = Time.time + actionFallbackRecheckSeconds;
            return true;
        }

        return false;
    }

    void ForceEndActionByFallback()
    {
        if (currentAction == BossAction.ShadowGrab && playerController != null)
            playerController.ForceNormalModeAfterExternalShadowExit(true);

        vulnerableToShadowAssassination = false;
        Anim_BossActionEnd();
    }

    void ResetBossActionTriggers()
    {
        if (anim == null) return;

        if (!string.IsNullOrEmpty(punchTrigger)) anim.ResetTrigger(punchTrigger);
        if (!string.IsNullOrEmpty(chargeWindupTrigger)) anim.ResetTrigger(chargeWindupTrigger);
        if (!string.IsNullOrEmpty(chargeLoopTrigger)) anim.ResetTrigger(chargeLoopTrigger);
        if (!string.IsNullOrEmpty(chargeEndTrigger)) anim.ResetTrigger(chargeEndTrigger);
        if (!string.IsNullOrEmpty(groundSlamTrigger)) anim.ResetTrigger(groundSlamTrigger);
        if (!string.IsNullOrEmpty(shadowGrabTrigger)) anim.ResetTrigger(shadowGrabTrigger);
        if (!string.IsNullOrEmpty(hurtTrigger)) anim.ResetTrigger(hurtTrigger);
    }

    void ReturnAnimatorToLocomotion()
    {
        if (anim == null) return;

        ResetBossActionTriggers();

        bool shouldWalk = desiredVelocity.sqrMagnitude > 0.0001f;

        anim.SetBool(walkBoolName, shouldWalk);

        if (!forceLocomotionStateOnActionEnd)
            return;

        string stateName = shouldWalk ? bossWalkStateName : bossIdleStateName;

        if (!string.IsNullOrEmpty(stateName))
            anim.CrossFadeInFixedTime(stateName, actionEndCrossFadeSeconds, 0);
    }

    bool HasValidPendingShadowGrab()
    {
        if (pendingShadowGrabTarget == null)
            return false;

        if (!pendingShadowGrabTarget.IsInShadowMode)
            return false;

        if (Time.time - lastShadowGrabRequestTime > shadowGrabRequestGraceSeconds)
            return false;

        return true;
    }

    bool CanStartShadowGrabNow()
    {
        if (!combatActive) return false;
        if (currentAction == BossAction.Dead) return false;
        if (isAttacking || currentAction != BossAction.None) return false;
        if (vulnerableToShadowAssassination) return false;
        if (bossConfig == null) return false;
        if (Time.time - lastShadowGrabTime < bossConfig.shadowGrabCooldown) return false;

        return true;
    }

    void StartShadowGrabAction(ShadowInteractController targetShadow)
    {
        if (targetShadow != null)
            playerShadow = targetShadow;

        lastShadowGrabTime = Time.time;
        pendingShadowGrabTarget = null;

        if (playerController != null)
            playerController.SetInputLocked(true, false);

        if (playerShadow != null)
            playerShadow.SetShadowToggleLocked(true, 0.25f);

        BeginAction(BossAction.ShadowGrab, shadowGrabTrigger, shadowGrabFallbackSeconds);
    }

    void HideAllTelegraphs()
    {
        if (chargeTelegraph != null)
            chargeTelegraph.CompleteAndHide(0f);

        if (groundSlamTelegraph != null)
            groundSlamTelegraph.CompleteAndHide(0f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (bossConfig == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, bossConfig.punchRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bossConfig.groundSlamRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, bossConfig.chargeMaxRange);
    }
#endif
}