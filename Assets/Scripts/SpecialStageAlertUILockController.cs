using UnityEngine;

public class SpecialStageAlertUILockController : MonoBehaviour
{
    public bool applyOnStart = true;
    public bool debugLog = true;

    void Start()
    {
        if (applyOnStart)
            ApplyToSceneAlertUIs();
    }

    public void ApplyToSceneAlertUIs()
    {
        EnemyAlertUI[] alertUIs = FindObjectsByType<EnemyAlertUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < alertUIs.Length; i++)
            ApplyToAlertUI(alertUIs[i]);

        Log($"Applied to scene alert UIs. Count={alertUIs.Length}");
    }

    public void ApplyToSpawnedEnemy(GameObject enemyRoot)
    {
        if (enemyRoot == null)
            return;

        EnemyAlertUI[] alertUIs = enemyRoot.GetComponentsInChildren<EnemyAlertUI>(true);

        for (int i = 0; i < alertUIs.Length; i++)
            ApplyToAlertUI(alertUIs[i]);

        Log($"Applied to spawned enemy alert UIs. Enemy={enemyRoot.name}, Count={alertUIs.Length}");
    }

    void ApplyToAlertUI(EnemyAlertUI alertUI)
    {
        if (alertUI == null)
            return;

        FaceCameraY faceCamera = alertUI.GetComponent<FaceCameraY>();
        if (faceCamera != null)
            faceCamera.enabled = false;

        SpecialStageAlertUIFaceLock faceLock =
            alertUI.GetComponent<SpecialStageAlertUIFaceLock>();

        if (faceLock == null)
            faceLock = alertUI.gameObject.AddComponent<SpecialStageAlertUIFaceLock>();

        faceLock.locked = true;
        faceLock.useCameraForward = true;
    }

    void Log(string message)
    {
        if (!debugLog)
            return;

        if (SpecialStageDebugHUD.Instance != null)
            SpecialStageDebugHUD.Log("AlertUILock", message, this);
        else
            Debug.Log($"[AlertUILock] {message}", this);
    }
}