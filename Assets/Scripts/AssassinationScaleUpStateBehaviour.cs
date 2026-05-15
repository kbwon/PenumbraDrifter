using UnityEngine;

public class AssassinationScaleUpStateBehaviour : StateMachineBehaviour
{
    [Header("Scale")]
    public float startScale = 1f;
    public float targetScale = 1.35f;

    [Header("Curve")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    AssassinationFeedback feedback;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        feedback = animator.GetComponentInParent<AssassinationFeedback>();

        if (feedback != null)
            feedback.SetVisualScale(startScale);
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (feedback == null)
            return;

        float t = Mathf.Clamp01(stateInfo.normalizedTime);
        float k = scaleCurve != null ? scaleCurve.Evaluate(t) : t;

        float scale = Mathf.Lerp(startScale, targetScale, k);
        feedback.SetVisualScale(scale);
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (feedback != null)
            feedback.SetVisualScale(targetScale);
    }
}