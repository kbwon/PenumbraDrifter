using System.Collections.Generic;
using UnityEngine;

public class SpecialStageFacingLockController : MonoBehaviour
{
    public static SpecialStageFacingLockController Instance { get; private set; }

    [Header("FaceCameraY Lock")]
    public bool lockFaceCameraY = true;
    public bool autoFindSceneFaceCameraTargets = true;
    public FaceCameraY[] manualFaceCameraTargets;

    [Header("Enemy Visual Billboard Lock")]
    public bool lockEnemyVisualBillboards = false;
    public bool autoFindSceneEnemies = false;
    public EnemyController[] manualEnemyTargets;

    [Header("Debug")]
    public bool debugLog = true;

    readonly HashSet<FaceCameraY> lockedFaceCameraTargets = new HashSet<FaceCameraY>();
    readonly HashSet<EnemyController> lockedEnemies = new HashSet<EnemyController>();

    bool active;

    void Awake()
    {
        Instance = this;
    }

    public void BeginLock()
    {
        active = true;

        if (lockFaceCameraY)
            CacheAndLockFaceCameraTargets();

        if (lockEnemyVisualBillboards)
            CacheAndLockEnemies();

        Log($"BeginLock complete. FaceCameraY={lockedFaceCameraTargets.Count}, Enemies={lockedEnemies.Count}");
    }

    public void EndLock()
    {
        foreach (FaceCameraY target in lockedFaceCameraTargets)
        {
            if (target != null)
                target.SetFacingLocked(false);
        }

        foreach (EnemyController enemy in lockedEnemies)
        {
            if (enemy != null)
                enemy.SetBillboardLocked(false);
        }

        Log($"EndLock complete. FaceCameraY={lockedFaceCameraTargets.Count}, Enemies={lockedEnemies.Count}");

        lockedFaceCameraTargets.Clear();
        lockedEnemies.Clear();
        active = false;
    }

    public void LockSpawnedObject(GameObject root)
    {
        if (!active) return;
        if (root == null) return;

        if (lockFaceCameraY)
        {
            FaceCameraY[] faces = root.GetComponentsInChildren<FaceCameraY>(true);

            for (int i = 0; i < faces.Length; i++)
                LockFaceCameraTarget(faces[i], true);
        }

        if (lockEnemyVisualBillboards)
        {
            EnemyController[] enemies = root.GetComponentsInChildren<EnemyController>(true);

            for (int i = 0; i < enemies.Length; i++)
                LockEnemy(enemies[i], true);
        }
    }

    void CacheAndLockFaceCameraTargets()
    {
        if (manualFaceCameraTargets != null)
        {
            for (int i = 0; i < manualFaceCameraTargets.Length; i++)
                LockFaceCameraTarget(manualFaceCameraTargets[i], true);
        }

        if (autoFindSceneFaceCameraTargets)
        {
            FaceCameraY[] found = FindObjectsByType<FaceCameraY>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < found.Length; i++)
                LockFaceCameraTarget(found[i], true);
        }
    }

    void CacheAndLockEnemies()
    {
        if (manualEnemyTargets != null)
        {
            for (int i = 0; i < manualEnemyTargets.Length; i++)
                LockEnemy(manualEnemyTargets[i], true);
        }

        if (autoFindSceneEnemies)
        {
            EnemyController[] found = FindObjectsByType<EnemyController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < found.Length; i++)
                LockEnemy(found[i], true);
        }
    }

    void LockFaceCameraTarget(FaceCameraY target, bool locked)
    {
        if (target == null)
            return;

        target.SetFacingLocked(locked);

        if (locked)
            lockedFaceCameraTargets.Add(target);
        else
            lockedFaceCameraTargets.Remove(target);
    }

    void LockEnemy(EnemyController enemy, bool locked)
    {
        if (enemy == null)
            return;

        enemy.SetBillboardLocked(locked);

        if (locked)
            lockedEnemies.Add(enemy);
        else
            lockedEnemies.Remove(enemy);
    }

    void OnDisable()
    {
        EndLock();

        if (Instance == this)
            Instance = null;
    }

    void Log(string message)
    {
        if (!debugLog) return;

        if (SpecialStageDebugHUD.Instance != null)
            SpecialStageDebugHUD.Log("FacingLock", message, this);
        else
            Debug.Log($"[SpecialStageFacingLock] {message}", this);
    }
}