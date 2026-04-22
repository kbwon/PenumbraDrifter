using UnityEngine;

public class TutorialAreaTrigger : MonoBehaviour
{
    public TutorialUIController ui;
    public bool triggerOnce = true;

    [Header("Popup")]
    public bool showPopup = true;
    public string popupTitle;
    [TextArea(3, 8)] public string popupBody;
    public Sprite popupIcon;
    public Sprite popupKeyA;
    public Sprite popupKeyB;
    public bool lockPlayerInput = true;

    [Header("Mission")]
    public bool showMission = true;
    public string missionText;
    public float missionAutoHideSeconds = 0f;

    bool triggered;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (triggerOnce && triggered)
            return;

        triggered = true;

        if (ui == null)
            ui = TutorialUIController.Instance != null
                ? TutorialUIController.Instance
                : FindFirstObjectByType<TutorialUIController>();

        if (ui == null) return;

        if (showPopup)
        {
            ui.ShowPopup(
                popupTitle,
                popupBody,
                popupIcon,
                popupKeyA,
                popupKeyB,
                lockPlayerInput
            );
        }

        if (showMission && !string.IsNullOrWhiteSpace(missionText))
            ui.SetMission(missionText, missionAutoHideSeconds);
    }
}