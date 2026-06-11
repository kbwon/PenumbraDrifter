using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ElevatorScrollTarget
{
    public string name;
    public Transform target;

    [Header("Move")]
    public Vector3 moveDirection = Vector3.down;
    public float speedMultiplier = 1f;

    [Header("Loop")]
    public bool loop = true;
    public float loopDistance = 20f;

    [HideInInspector] public Vector3 startPosition;
}

public class ElevatorExteriorScroller : MonoBehaviour
{
    [Header("Targets")]
    public ElevatorScrollTarget[] targets;

    [Header("Base Move")]
    public float speed = 2f;

    [Header("Pause")]
    public bool respectGamePause = true;

    [Header("Debug")]
    public bool debugLog = true;
    public float debugPositionLogInterval = 1f;

    bool playing;
    float lastDebugLogTime;

    void Awake()
    {
        CacheStartPositions();
    }

    void CacheStartPositions()
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null && targets[i].target != null)
                targets[i].startPosition = targets[i].target.position;
        }

        Log("Start positions cached.");
    }

    public void Play(float newSpeed)
    {
        speed = newSpeed;
        playing = true;
        Log($"Play scroll. speed={speed}");
    }

    public void Stop()
    {
        playing = false;
        Log("Stop scroll.");
    }

    void Update()
    {
        if (IsGamePaused())
            return;

        if (!playing)
            return;

        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            ElevatorScrollTarget item = targets[i];

            if (item == null || item.target == null)
                continue;

            Vector3 dir = item.moveDirection.sqrMagnitude > 0.0001f
                ? item.moveDirection.normalized
                : Vector3.down;

            float finalSpeed = speed * item.speedMultiplier;

            item.target.position += dir * (finalSpeed * Time.deltaTime);

            if (item.loop)
            {
                float moved = Vector3.Distance(item.target.position, item.startPosition);

                if (moved >= item.loopDistance)
                {
                    item.target.position = item.startPosition;
                    Log($"Loop reset: {GetTargetName(item)}");
                }
            }
        }

        if (debugLog && Time.time - lastDebugLogTime >= debugPositionLogInterval)
        {
            lastDebugLogTime = Time.time;
            LogPositions();
        }
    }

    bool IsGamePaused()
    {
        return respectGamePause &&
               GameManager.Instance != null &&
               GameManager.Instance.IsPaused;
    }

    void LogPositions()
    {
        if (targets == null)
            return;

        for (int i = 0; i < targets.Length; i++)
        {
            ElevatorScrollTarget item = targets[i];

            if (item == null || item.target == null)
                continue;

            SpecialStageDebugHUD.Log(
                "Scroller",
                $"{GetTargetName(item)} pos={item.target.position}, dir={item.moveDirection}, mul={item.speedMultiplier}",
                item.target
            );
        }
    }

    string GetTargetName(ElevatorScrollTarget item)
    {
        if (!string.IsNullOrEmpty(item.name))
            return item.name;

        return item.target != null ? item.target.name : "NULL";
    }

    void Log(string message)
    {
        if (debugLog)
            SpecialStageDebugHUD.Log("Scroller", message, this);
    }

    [ContextMenu("TEST/Play Scroll")]
    void TestPlay()
    {
        Play(speed);
    }

    [ContextMenu("TEST/Stop Scroll")]
    void TestStop()
    {
        Stop();
    }

    [ContextMenu("TEST/Cache Start Positions")]
    void TestCache()
    {
        CacheStartPositions();
    }
}