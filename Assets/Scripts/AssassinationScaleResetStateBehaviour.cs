using UnityEngine;

public class AssassinationScaleResetStateBehaviour : StateMachineBehaviour
{
    [Header("Scale")]
    public float startScale = 1.35f;
    public float targetScale = 1f;

    [Tooltip("상태 전체 길이 중 몇 퍼센트 안에 원래 크기로 돌아올지 설정합니다.")]
    [Range(0.05f, 1f)]
    public float resetDuration01 = 0.25f;

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
        float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, resetDuration01));

        float scale = Mathf.Lerp(startScale, targetScale, p);
        feedback.SetVisualScale(scale);
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (feedback != null)
            feedback.ResetVisualScale();
    }
}