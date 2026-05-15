using UnityEngine;

public class AssassinationImpactStateBehaviour : StateMachineBehaviour
{
    [Range(0f, 1f)]
    public float impactNormalizedTime = 0.6f;

    public bool callOnExitIfMissed = true;

    bool called;
    AssassinationFeedback feedback;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        called = false;
        feedback = animator.GetComponentInParent<AssassinationFeedback>();
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (called)
            return;

        float t = Mathf.Clamp01(stateInfo.normalizedTime);

        if (t >= impactNormalizedTime)
            CallImpact();
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (!called && callOnExitIfMissed)
            CallImpact();
    }

    void CallImpact()
    {
        called = true;

        if (feedback != null)
            feedback.PlayImpactFeedback();
    }
}