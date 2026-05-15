using UnityEngine;

public class AssassinationBulletTimeStateBehaviour : StateMachineBehaviour
{
    public bool playOnEnter = true;

    AssassinationFeedback feedback;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        feedback = animator.GetComponentInParent<AssassinationFeedback>();

        if (playOnEnter && feedback != null)
            feedback.PlayBulletTime();
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        // AttackBite가 중간에 끊겨도 시간이 느려진 채 남지 않게 한다.
        if (feedback != null)
            feedback.StopBulletTime();
    }
}