using UnityEngine;

public class TutorialHintTrigger : MonoBehaviour
{
    public TutorialUIController ui;
    public bool showOnlyOnce = false;

    [TextArea(2, 5)] public string hintText;
    public Sprite hintIcon;
    public Sprite hintKey;

    bool triggeredOnce;

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

        if (showOnlyOnce && triggeredOnce)
            return;

        triggeredOnce = true;

        if (ui == null)
            ui = TutorialUIController.Instance != null
                ? TutorialUIController.Instance
                : FindFirstObjectByType<TutorialUIController>();

        if (ui == null) return;

        ui.ShowHint(hintText, hintIcon, hintKey, this);
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (ui == null) return;
        ui.HideHint(this);
    }
}