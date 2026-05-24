using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialHUDGuideTrigger : MonoBehaviour
{
    public TutorialHUDGuideDirector guideDirector;
    public bool playOnce = true;

    bool used;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (playOnce && used) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (guideDirector == null)
            guideDirector = FindFirstObjectByType<TutorialHUDGuideDirector>();

        if (guideDirector == null)
        {
            Debug.LogWarning("[TutorialHUDGuideTrigger] TutorialHUDGuideDirector is missing.");
            return;
        }

        used = true;
        guideDirector.PlayGuide();
    }
}