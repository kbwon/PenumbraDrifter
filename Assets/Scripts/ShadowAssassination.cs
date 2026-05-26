using UnityEngine;

public class ShadowAssassination : MonoBehaviour
{
    public ShadowInteractController shadowCtrl;
    public PlayerController playerController;

    [Header("Input")]
    public KeyCode assassinateKey = KeyCode.Mouse0;

    [Header("Tuning")]
    public float maxAssassinateDistance = 2.5f;
    public LayerMask enemyMask;

    [Header("Animation")]
    public Animator anim;
    public string assassinateTriggerName = "shadowAssassinate";

    [Header("Debug")]
    public bool warnWhenNoTarget = false;

    readonly Collider[] overlapResults = new Collider[16];

    bool isAssassinating;
    bool hitDone;
    bool endDone;

    EnemyController pendingTarget;
    AssassinationFeedback feedback;

    void Awake()
    {
        if (!shadowCtrl)
            shadowCtrl = GetComponent<ShadowInteractController>();

        if (!playerController)
            playerController = GetComponent<PlayerController>();

        if (!anim)
            anim = GetComponentInChildren<Animator>();

        if (!feedback)
            feedback = GetComponent<AssassinationFeedback>();
    }

    void Update()
    {
        if (isAssassinating) return;

        if (!shadowCtrl) return;
        if (!shadowCtrl.IsInShadowMode) return;

        if (!Input.GetKeyDown(assassinateKey)) return;

        StartAssassination();
    }

    void StartAssassination()
    {
        pendingTarget = FindNearbyEnemy();

        if (pendingTarget == null)
        {
            if (warnWhenNoTarget)
                Debug.LogWarning("[Assassination] No valid target found. Check enemyMask, distance, collider layer, and canBeAssassinated.");

            return;
        }

        isAssassinating = true;
        hitDone = false;
        endDone = false;

        if (playerController != null)
        {
            // 조작은 잠그되, PlayerController가 Animator를 일반 Idle로 덮어쓰지 못하게 한다.
            playerController.SetInputLocked(true, false);
            playerController.SyncShadowStateWithoutTransition();
        }

        if (anim != null)
        {
            // 공격 시작 직전 Animator 조건을 그림자 모드 기준으로 고정한다.
            anim.SetBool("isWalk", false);
            anim.SetBool("Idle", false);
            anim.SetBool("isShadowWalk", false);
            anim.SetBool("ShadowIdle", true);

            anim.ResetTrigger(assassinateTriggerName);
            anim.SetTrigger(assassinateTriggerName);
        }
    }

    // StateMachineBehaviour에서 호출
    public void NotifyAssassinationHit()
    {
        DoAssassinationHit();
    }

    void DoAssassinationHit()
    {
        if (hitDone) return;
        hitDone = true;

        if (pendingTarget == null)
        {
            Debug.LogWarning("[Assassination] Hit timing reached, but pendingTarget is null.");
            return;
        }

        pendingTarget.KillByAssassination();
        pendingTarget = null;
    }

    // StateMachineBehaviour에서 호출
    public void NotifyAssassinationEnd()
    {
        FinishAssassination();
    }

    void FinishAssassination()
    {
        if (endDone) return;
        endDone = true;

        pendingTarget = null;

        ForceExitShadowForAssassination();

        if (playerController != null)
            playerController.SetInputLocked(false);

        isAssassinating = false;

        if (feedback != null)
        {
            feedback.ResetVisualScale();
            feedback.StopVisualFeedback();
        }
    }

    void ForceExitShadowForAssassination()
    {
        if (shadowCtrl == null) return;

        shadowCtrl.ForceExitShadowMode();

        if (playerController != null)
            playerController.SyncShadowStateWithoutTransition();
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

            EnemyController enemy = hitCol.GetComponentInParent<EnemyController>();
            if (enemy == null) continue;
            if (!enemy.gameObject.activeInHierarchy) continue;
            if (enemy.config == null) continue;
            if (!enemy.config.canBeAssassinated) continue;

            if (!enemy.CanStartShadowAssassination()) continue;

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
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, maxAssassinateDistance);
    }
}