using UnityEngine;

public class AssassinationScaleUpStateBehaviour : StateMachineBehaviour
{
    [Header("Scale")]
    public float startScale = 1f;
    public float targetScale = 1.35f;

    [Header("Lift")]
    [Tooltip("스케일이 커질 때 위로 올릴 local Y 값입니다.")]
    public float startLiftLocalY = 0f;

    public float targetLiftLocalY = 0.18f;

    [Header("Curve")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public AnimationCurve liftCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    AssassinationFeedback feedback;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        feedback = animator.GetComponentInParent<AssassinationFeedback>();

        if (feedback != null)
        {
            feedback.CacheBaseTransform();
            feedback.SetVisualPose(startScale, startLiftLocalY);
        }
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (feedback == null)
            return;

        float t = Mathf.Clamp01(stateInfo.normalizedTime);

        float scaleK = scaleCurve != null ? scaleCurve.Evaluate(t) : t;
        float liftK = liftCurve != null ? liftCurve.Evaluate(t) : t;

        float scale = Mathf.Lerp(startScale, targetScale, scaleK);
        float lift = Mathf.Lerp(startLiftLocalY, targetLiftLocalY, liftK);

        feedback.SetVisualPose(scale, lift);
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (feedback != null)
            feedback.SetVisualPose(targetScale, targetLiftLocalY);
    }
}