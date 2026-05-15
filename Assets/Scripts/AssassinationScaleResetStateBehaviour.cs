using UnityEngine;

public class AssassinationScaleResetStateBehaviour : StateMachineBehaviour
{
    [Header("Scale")]
    public float startScale = 1.35f;
    public float targetScale = 1f;

    [Header("Lift")]
    public float startLiftLocalY = 0.18f;
    public float targetLiftLocalY = 0f;

    [Tooltip("상태 전체 길이 중 몇 퍼센트 안에 원래 크기와 위치로 돌아올지 설정합니다.")]
    [Range(0.05f, 1f)]
    public float resetDuration01 = 0.25f;

    [Header("Curve")]
    public AnimationCurve resetCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    AssassinationFeedback feedback;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        feedback = animator.GetComponentInParent<AssassinationFeedback>();

        if (feedback != null)
            feedback.SetVisualPose(startScale, startLiftLocalY);
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (feedback == null)
            return;

        float t = Mathf.Clamp01(stateInfo.normalizedTime);
        float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, resetDuration01));

        float k = resetCurve != null ? resetCurve.Evaluate(p) : p;

        float scale = Mathf.Lerp(startScale, targetScale, k);
        float lift = Mathf.Lerp(startLiftLocalY, targetLiftLocalY, k);

        feedback.SetVisualPose(scale, lift);
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (feedback != null)
            feedback.ResetVisualPose();
    }
}