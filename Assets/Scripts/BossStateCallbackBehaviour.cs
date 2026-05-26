using System;
using UnityEngine;

public enum BossSMBCallbackType
{
    None = 0,

    WindupEnd = 1,

    PunchHit = 2,
    GroundSlamHit = 3,

    ShadowGrabPull = 4,
    ShadowGrabThrow = 5,

    VulnerableStart = 6,
    VulnerableEnd = 7,

    ActionEnd = 8,
    DeathEnd = 9,

    ChargeMoveStart = 10
}

[Serializable]
public class BossSMBTimedCallback
{
    public BossSMBCallbackType callback = BossSMBCallbackType.None;

    [Range(0f, 1f)]
    public float normalizedTime = 0.5f;
}

public class BossStateCallbackBehaviour : StateMachineBehaviour
{
    [Header("Enter")]
    public BossSMBCallbackType[] onEnterCallbacks;

    [Header("Timed")]
    public BossSMBTimedCallback[] timedCallbacks;

    [Header("Exit")]
    public BossSMBCallbackType[] onExitCallbacks;

    BossController boss;
    bool[] fired;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        boss = animator.GetComponentInParent<BossController>();

        if (timedCallbacks != null)
            fired = new bool[timedCallbacks.Length];
        else
            fired = null;

        ExecuteAll(onEnterCallbacks);
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (boss == null)
            boss = animator.GetComponentInParent<BossController>();

        if (boss == null)
            return;

        if (timedCallbacks == null || timedCallbacks.Length == 0)
            return;

        if (fired == null || fired.Length != timedCallbacks.Length)
            fired = new bool[timedCallbacks.Length];

        float t = stateInfo.normalizedTime;

        if (!stateInfo.loop)
            t = Mathf.Clamp01(t);

        for (int i = 0; i < timedCallbacks.Length; i++)
        {
            if (fired[i]) continue;

            BossSMBTimedCallback item = timedCallbacks[i];
            if (item == null) continue;

            if (t >= item.normalizedTime)
            {
                fired[i] = true;
                Execute(item.callback);
            }
        }
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (boss == null)
            boss = animator.GetComponentInParent<BossController>();

        ExecuteAll(onExitCallbacks);
    }

    void ExecuteAll(BossSMBCallbackType[] callbacks)
    {
        if (callbacks == null)
            return;

        for (int i = 0; i < callbacks.Length; i++)
            Execute(callbacks[i]);
    }

    void Execute(BossSMBCallbackType callback)
    {
        if (boss == null)
            return;

        switch (callback)
        {
            case BossSMBCallbackType.WindupEnd:
                boss.Anim_BossWindupEnd();
                break;

            case BossSMBCallbackType.PunchHit:
                boss.Anim_BossPunchHit();
                break;

            case BossSMBCallbackType.GroundSlamHit:
                boss.Anim_BossGroundSlamHit();
                break;

            case BossSMBCallbackType.ShadowGrabPull:
                boss.Anim_BossShadowGrabPull();
                break;

            case BossSMBCallbackType.ShadowGrabThrow:
                boss.Anim_BossShadowGrabThrow();
                break;

            case BossSMBCallbackType.VulnerableStart:
                boss.Anim_BossVulnerableStart();
                break;

            case BossSMBCallbackType.VulnerableEnd:
                boss.Anim_BossVulnerableEnd();
                break;

            case BossSMBCallbackType.ActionEnd:
                boss.Anim_BossActionEnd();
                break;

            case BossSMBCallbackType.DeathEnd:
                boss.Anim_BossDeathEnd();
                break;

            case BossSMBCallbackType.ChargeMoveStart:
                boss.Anim_BossChargeMoveStart();
                break;
        }
    }
}