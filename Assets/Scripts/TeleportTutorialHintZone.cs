using UnityEngine;

public class TeleportTutorialHintZone : MonoBehaviour
{
    public TutorialUIController ui;
    public ShadowTeleport teleport;

    [Header("Sprites")]
    public Sprite hintIcon;
    public Sprite hintKey;

    [Header("Texts")]
    [TextArea(2, 4)] public string notInShadowText = "Enter Shadow Mode first";
    [TextArea(2, 4)] public string baseAimText = "Aim with the mouse / Press Space to Blink";
    [TextArea(2, 4)] public string validTargetText = "Blink available";
    [TextArea(2, 4)] public string cooldownText = "Blink is not ready yet";

    bool playerInside;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void Awake()
    {
        if (teleport == null && GameManager.Instance != null)
            teleport = GameManager.Instance.teleport;
    }

    void Update()
    {
        if (!playerInside) return;

        if (ui == null)
            ui = TutorialUIController.Instance != null
                ? TutorialUIController.Instance
                : FindFirstObjectByType<TutorialUIController>();

        if (ui == null || teleport == null || teleport.shadowCtrl == null)
            return;

        if (!teleport.shadowCtrl.IsInShadowMode)
        {
            ui.ShowHint(notInShadowText, hintIcon, hintKey, this);
            return;
        }

        if (!teleport.IsReady)
        {
            ui.ShowHint(cooldownText, hintIcon, hintKey, this);
            return;
        }

        bool valid = teleport.TryGetTeleportTarget(out _);
        ui.ShowHint(valid ? validTargetText : baseAimText, hintIcon, hintKey, this);
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        playerInside = false;

        if (ui != null)
            ui.HideHint(this, true);
    }
}