using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ElevatorEnemySpawn
{
    public GameObject enemyPrefab;
    public Transform spawnPoint;

    [Header("Entry Move")]
    public Transform enterTarget;
    public float enterMoveSpeed = 2.5f;
}

[Serializable]
public class ElevatorWave
{
    public string waveName;
    public ElevatorEnemySpawn[] enemies;
}

public class ElevatorWaveSpawner : MonoBehaviour
{
    [Header("Debug")]
    public bool debugLog = true;

    [Header("Test")]
    public ElevatorWave testWave;

    readonly List<GameObject> aliveEnemies = new List<GameObject>();

    public IReadOnlyList<GameObject> AliveEnemies => aliveEnemies;

    [ContextMenu("TEST/Spawn Test Wave")]
    public void TestSpawnWave()
    {
        StopAllCoroutines();
        StartCoroutine(SpawnWaveAndWaitEntry(testWave));
    }

    [ContextMenu("TEST/Clear All Enemies")]
    public void TestClearEnemies()
    {
        ClearAllEnemies();
    }

    public IEnumerator SpawnWaveAndWaitEntry(ElevatorWave wave)
    {
        ClearNullEnemies();

        if (wave == null)
        {
            Log("SpawnWave called but wave is null.");
            yield break;
        }

        Log($"Spawn wave start: {wave.waveName}");

        List<Coroutine> entryRoutines = new List<Coroutine>();

        if (wave.enemies != null)
        {
            for (int i = 0; i < wave.enemies.Length; i++)
            {
                ElevatorEnemySpawn spawn = wave.enemies[i];

                if (spawn == null)
                {
                    Warn($"Wave {wave.waveName} enemy {i} is null.");
                    continue;
                }

                if (spawn.enemyPrefab == null)
                {
                    Warn($"Wave {wave.waveName} enemy {i} prefab is null.");
                    continue;
                }

                if (spawn.spawnPoint == null)
                {
                    Warn($"Wave {wave.waveName} enemy {i} spawnPoint is null.");
                    continue;
                }

                GameObject enemy = Instantiate(
                    spawn.enemyPrefab,
                    spawn.spawnPoint.position,
                    spawn.spawnPoint.rotation
                );

                aliveEnemies.Add(enemy);

                Log($"Spawned enemy: {enemy.name} at {spawn.spawnPoint.name}");

                if (spawn.enterTarget != null)
                {
                    Coroutine routine = StartCoroutine(MoveEnemyToEnterTarget(enemy, spawn));
                    entryRoutines.Add(routine);
                }
                else
                {
                    Log($"Enemy {enemy.name} has no enterTarget. AI starts immediately.");
                }
            }
        }

        for (int i = 0; i < entryRoutines.Count; i++)
            yield return entryRoutines[i];

        Log($"Spawn wave entry complete: {wave.waveName}");
    }

    IEnumerator MoveEnemyToEnterTarget(GameObject enemy, ElevatorEnemySpawn spawn)
    {
        if (enemy == null || spawn.enterTarget == null)
            yield break;

        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        EnemyVision enemyVision = enemy.GetComponent<EnemyVision>();
        Rigidbody rb = enemy.GetComponent<Rigidbody>();

        bool enemyControllerWasEnabled = enemyController != null && enemyController.enabled;
        bool enemyVisionWasEnabled = enemyVision != null && enemyVision.enabled;

        if (enemyController != null)
            enemyController.enabled = false;

        if (enemyVision != null)
            enemyVision.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Log($"Enemy entry move start: {enemy.name} -> {spawn.enterTarget.name}");

        float stopDistance = 0.05f;

        while (enemy != null)
        {
            Vector3 current = enemy.transform.position;
            Vector3 target = spawn.enterTarget.position;

            current.y = enemy.transform.position.y;
            target.y = enemy.transform.position.y;

            Vector3 toTarget = target - current;
            toTarget.y = 0f;

            if (toTarget.magnitude <= stopDistance)
                break;

            Vector3 dir = toTarget.normalized;

            enemy.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            Vector3 next = enemy.transform.position + dir * (spawn.enterMoveSpeed * Time.deltaTime);
            next.y = enemy.transform.position.y;

            if (rb != null)
                rb.MovePosition(next);
            else
                enemy.transform.position = next;

            yield return null;
        }

        if (enemy != null)
        {
            Vector3 finalPos = enemy.transform.position;
            finalPos.x = spawn.enterTarget.position.x;
            finalPos.z = spawn.enterTarget.position.z;

            if (rb != null)
            {
                rb.position = finalPos;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                enemy.transform.position = finalPos;
            }

            if (enemyController != null)
            {
                enemyController.ResetHomePositionToCurrent();
                enemyController.enabled = enemyControllerWasEnabled;
            }

            if (enemyVision != null)
                enemyVision.enabled = enemyVisionWasEnabled;

            Log($"Enemy entry move complete: {enemy.name}");
        }
    }

    public bool IsWaveCleared()
    {
        ClearNullEnemies();

        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = aliveEnemies[i];

            if (enemy != null && enemy.activeInHierarchy)
                return false;
        }

        return true;
    }

    public void ClearAllEnemies()
    {
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            if (aliveEnemies[i] != null)
                Destroy(aliveEnemies[i]);
        }

        aliveEnemies.Clear();

        Log("All spawned enemies cleared.");
    }

    void ClearNullEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null)
                aliveEnemies.RemoveAt(i);
        }
    }

    void Log(string message)
    {
        if (debugLog)
            SpecialStageDebugHUD.Log("Wave", message, this);
    }

    void Warn(string message)
    {
        SpecialStageDebugHUD.Warn("Wave", message, this);
    }
}