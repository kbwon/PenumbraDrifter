using UnityEngine;

public class DemoProgressSaveTrigger : MonoBehaviour
{
    [Header("Save Point")]
    public DemoContinuePoint pointToSave = DemoContinuePoint.Stage1;

    [Header("When To Save")]
    public bool saveOnStart = false;
    public bool saveOnTriggerEnter = true;

    [Header("Trigger Filter")]
    public bool playerOnly = true;
    public string playerTag = "Player";

    [Header("Options")]
    public bool saveOnce = true;
    public bool debugLog = true;

    bool saved;

    void Start()
    {
        if (saveOnStart)
            Save();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!saveOnTriggerEnter)
            return;

        if (playerOnly)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();

            if (player == null)
                return;

            if (!player.CompareTag(playerTag))
                return;
        }

        Save();
    }

    public void Save()
    {
        if (saveOnce && saved)
            return;

        DemoProgress.SaveContinuePoint(pointToSave);
        saved = true;

        if (debugLog)
            Debug.Log($"[DemoProgressSaveTrigger] Saved continue point: {pointToSave}", this);
    }

    public void SaveStage1()
    {
        pointToSave = DemoContinuePoint.Stage1;
        Save();
    }

    public void SaveSpecialStage()
    {
        pointToSave = DemoContinuePoint.SpecialStage;
        Save();
    }

    public void SaveBossStage()
    {
        pointToSave = DemoContinuePoint.BossStage;
        Save();
    }
}