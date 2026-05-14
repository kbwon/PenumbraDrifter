using UnityEngine;

public class AssassinationHitStateBehaviour : StateMachineBehaviour
{
    [Range(0f, 1f)]
    public float hitNormalizedTime = 0.6f;

    public bool callOnExitIfMissed = true;

    bool called;
    ShadowAssassination target;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        called = false;
        target = animator.GetComponentInParent<ShadowAssassination>();
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (called) return;

        float t = stateInfo.normalizedTime;

        if (t >= hitNormalizedTime)
            CallHit();
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (!called && callOnExitIfMissed)
            CallHit();
    }

    void CallHit()
    {
        called = true;

        if (target != null)
            target.NotifyAssassinationHit();
    }
}