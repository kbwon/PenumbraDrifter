using System.Collections;
using UnityEngine;

public class ElevatorDoorController : MonoBehaviour
{
    [Header("Doors")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Closed Local Positions")]
    public Vector3 leftClosedLocalPos = new Vector3(-0.5f, 0f, 0f);
    public Vector3 rightClosedLocalPos = new Vector3(0.5f, 0f, 0f);

    [Header("Open Local Positions")]
    public Vector3 leftOpenLocalPos = new Vector3(-1.7f, 0f, 0f);
    public Vector3 rightOpenLocalPos = new Vector3(1.7f, 0f, 0f);

    [Header("Timing")]
    public float openSeconds = 0.5f;
    public float closeSeconds = 0.5f;

    [Header("Debug")]
    public bool debugLog = true;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        ValidateSetup();
    }

    void ValidateSetup()
    {
        if (leftDoor == null)
            SpecialStageDebugHUD.Error("Door", "LeftDoor is not assigned.", this);

        if (rightDoor == null)
            SpecialStageDebugHUD.Error("Door", "RightDoor is not assigned.", this);

        if (Vector3.Distance(leftClosedLocalPos, leftOpenLocalPos) < 0.01f)
            SpecialStageDebugHUD.Warn("Door", "Left closed/open positions are almost same.", this);

        if (Vector3.Distance(rightClosedLocalPos, rightOpenLocalPos) < 0.01f)
            SpecialStageDebugHUD.Warn("Door", "Right closed/open positions are almost same.", this);

        CheckCollider(leftDoor, "LeftDoor");
        CheckCollider(rightDoor, "RightDoor");
    }

    void CheckCollider(Transform door, string label)
    {
        if (door == null)
            return;

        Collider col = door.GetComponentInChildren<Collider>();

        if (col == null)
        {
            SpecialStageDebugHUD.Warn("Door", $"{label} has no Collider. Player can pass through it.", door);
            return;
        }

        if (col.isTrigger)
            SpecialStageDebugHUD.Warn("Door", $"{label} Collider is Trigger. It will not block player.", col);
    }

    [ContextMenu("TEST/Capture Current As Closed")]
    public void CaptureCurrentAsClosed()
    {
        if (leftDoor != null)
            leftClosedLocalPos = leftDoor.localPosition;

        if (rightDoor != null)
            rightClosedLocalPos = rightDoor.localPosition;

        Log($"Captured closed positions. Left={leftClosedLocalPos}, Right={rightClosedLocalPos}");
    }

    [ContextMenu("TEST/Capture Current As Open")]
    public void CaptureCurrentAsOpen()
    {
        if (leftDoor != null)
            leftOpenLocalPos = leftDoor.localPosition;

        if (rightDoor != null)
            rightOpenLocalPos = rightDoor.localPosition;

        Log($"Captured open positions. Left={leftOpenLocalPos}, Right={rightOpenLocalPos}");
    }

    [ContextMenu("TEST/Move Immediately To Closed")]
    public void MoveImmediatelyToClosed()
    {
        if (leftDoor != null)
            leftDoor.localPosition = leftClosedLocalPos;

        if (rightDoor != null)
            rightDoor.localPosition = rightClosedLocalPos;

        IsOpen = false;
        Log($"Moved immediately to closed. Left={leftClosedLocalPos}, Right={rightClosedLocalPos}");
    }

    [ContextMenu("TEST/Move Immediately To Open")]
    public void MoveImmediatelyToOpen()
    {
        if (leftDoor != null)
            leftDoor.localPosition = leftOpenLocalPos;

        if (rightDoor != null)
            rightDoor.localPosition = rightOpenLocalPos;

        IsOpen = true;
        Log($"Moved immediately to open. Left={leftOpenLocalPos}, Right={rightOpenLocalPos}");
    }

    [ContextMenu("TEST/Open Door")]
    public void TestOpen()
    {
        StopAllCoroutines();
        StartCoroutine(Open());
    }

    [ContextMenu("TEST/Close Door")]
    public void TestClose()
    {
        StopAllCoroutines();
        StartCoroutine(Close());
    }

    public IEnumerator Open()
    {
        Vector3 leftFrom = leftDoor != null ? leftDoor.localPosition : leftClosedLocalPos;
        Vector3 rightFrom = rightDoor != null ? rightDoor.localPosition : rightClosedLocalPos;

        Log($"Open start. Left {leftFrom} -> {leftOpenLocalPos}, Right {rightFrom} -> {rightOpenLocalPos}");

        yield return MoveDoors(
            leftFrom,
            leftOpenLocalPos,
            rightFrom,
            rightOpenLocalPos,
            openSeconds
        );

        IsOpen = true;
        Log($"Open complete. Left={GetLeftPos()}, Right={GetRightPos()}");
    }

    public IEnumerator Close()
    {
        Vector3 leftFrom = leftDoor != null ? leftDoor.localPosition : leftOpenLocalPos;
        Vector3 rightFrom = rightDoor != null ? rightDoor.localPosition : rightOpenLocalPos;

        Log($"Close start. Left {leftFrom} -> {leftClosedLocalPos}, Right {rightFrom} -> {rightClosedLocalPos}");

        yield return MoveDoors(
            leftFrom,
            leftClosedLocalPos,
            rightFrom,
            rightClosedLocalPos,
            closeSeconds
        );

        IsOpen = false;
        Log($"Close complete. Left={GetLeftPos()}, Right={GetRightPos()}");
    }

    IEnumerator MoveDoors(
        Vector3 leftFrom,
        Vector3 leftTo,
        Vector3 rightFrom,
        Vector3 rightTo,
        float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
            k = k * k * (3f - 2f * k);

            if (leftDoor != null)
                leftDoor.localPosition = Vector3.Lerp(leftFrom, leftTo, k);

            if (rightDoor != null)
                rightDoor.localPosition = Vector3.Lerp(rightFrom, rightTo, k);

            yield return null;
        }

        if (leftDoor != null)
            leftDoor.localPosition = leftTo;

        if (rightDoor != null)
            rightDoor.localPosition = rightTo;
    }

    Vector3 GetLeftPos()
    {
        return leftDoor != null ? leftDoor.localPosition : Vector3.zero;
    }

    Vector3 GetRightPos()
    {
        return rightDoor != null ? rightDoor.localPosition : Vector3.zero;
    }

    void Log(string message)
    {
        if (!debugLog)
            return;

        SpecialStageDebugHUD.Log("Door", message, this);
    }
}