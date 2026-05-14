using UnityEngine;

public class AssassinationEndStateBehaviour : StateMachineBehaviour
{
    [Range(0f, 1f)]
    public float endNormalizedTime = 0.9f;

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

        if (t >= endNormalizedTime)
            CallEnd();
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (!called && callOnExitIfMissed)
            CallEnd();
    }

    void CallEnd()
    {
        called = true;

        if (target != null)
            target.NotifyAssassinationEnd();
    }
}