using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class ElevatorBuildingSet
{
    public string setName;
    public Transform root;

    [HideInInspector] public Vector3 initialLocalPosition;
}

public class ElevatorBuildingSetRailController : MonoBehaviour
{
    [Header("Refs")]
    public Transform railRoot;

    [Header("Sets")]
    public ElevatorBuildingSet[] sets;

    [Header("Layout")]
    public bool autoLayoutXOnAwake = true;
    public float setSpacing = 24f;

    [Header("Vertical Scroll")]
    public Vector3 verticalDirection = new Vector3(0f, -1f, 0f);
    public float currentVerticalSpeed;
    public bool verticalPlaying;

    [Header("Horizontal Transition")]
    public AnimationCurve horizontalEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("true면 다음 세트가 들어올 때 현재 세트의 Y 위치와 맞춥니다. 지금 기획에서는 false 추천.")]
    public bool alignNextSetYToCurrentOnTransition = false;

    [Header("Debug")]
    public bool debugLog = true;

    int currentIndex = 0;
    float baseRailLocalX;

    public int CurrentIndex => currentIndex;
    public float CurrentVerticalSpeed => currentVerticalSpeed;

    void Awake()
    {
        if (railRoot == null)
            railRoot = transform;

        baseRailLocalX = railRoot.localPosition.x;

        CacheInitialPositions();

        if (autoLayoutXOnAwake)
            LayoutSetsByX();

        SetImmediate(0, true);
    }

    void Update()
    {
        if (!verticalPlaying)
            return;

        if (!IsValidIndex(currentIndex))
            return;

        Transform currentRoot = sets[currentIndex].root;
        if (currentRoot == null)
            return;

        Vector3 dir = verticalDirection.sqrMagnitude > 0.0001f
            ? verticalDirection.normalized
            : Vector3.down;

        currentRoot.localPosition += dir * currentVerticalSpeed * Time.unscaledDeltaTime;
    }

    void CacheInitialPositions()
    {
        if (sets == null)
            return;

        for (int i = 0; i < sets.Length; i++)
        {
            if (sets[i] == null || sets[i].root == null)
                continue;

            sets[i].initialLocalPosition = sets[i].root.localPosition;
        }
    }

    void LayoutSetsByX()
    {
        if (sets == null)
            return;

        for (int i = 0; i < sets.Length; i++)
        {
            if (sets[i] == null || sets[i].root == null)
                continue;

            Vector3 p = sets[i].initialLocalPosition;
            p.x = i * setSpacing;

            sets[i].initialLocalPosition = p;
            sets[i].root.localPosition = p;
        }
    }

    public void SetImmediate(int index, bool resetSetY)
    {
        if (!IsValidIndex(index))
            return;

        currentIndex = index;

        Vector3 railPos = railRoot.localPosition;
        railPos.x = GetRailXForIndex(index);
        railRoot.localPosition = railPos;

        for (int i = 0; i < sets.Length; i++)
        {
            if (sets[i] == null || sets[i].root == null)
                continue;

            if (resetSetY)
                ResetSetLocalPosition(i);

            sets[i].root.gameObject.SetActive(i == currentIndex);
        }

        Log($"SetImmediate index={index}, resetSetY={resetSetY}");
    }

    public void PlayVertical(float speed)
    {
        currentVerticalSpeed = Mathf.Max(0f, speed);
        verticalPlaying = currentVerticalSpeed > 0.001f;

        Log($"PlayVertical speed={currentVerticalSpeed:0.00}, current={GetSetName(currentIndex)}");
    }

    public void SetVerticalSpeed(float speed)
    {
        currentVerticalSpeed = Mathf.Max(0f, speed);
        verticalPlaying = currentVerticalSpeed > 0.001f;

        Log($"SetVerticalSpeed speed={currentVerticalSpeed:0.00}, playing={verticalPlaying}, current={GetSetName(currentIndex)}");
    }

    public void StopVertical()
    {
        currentVerticalSpeed = 0f;
        verticalPlaying = false;

        Log("StopVertical");
    }

    public void ResetCurrentSetToInitialY()
    {
        if (!IsValidIndex(currentIndex))
            return;

        ResetSetLocalPosition(currentIndex);
    }

    public IEnumerator TransitionTo(int nextIndex, float seconds)
    {
        if (!IsValidIndex(nextIndex))
            yield break;

        if (nextIndex == currentIndex)
            yield break;

        StopVertical();

        int previousIndex = currentIndex;

        Transform previousRoot = sets[previousIndex].root;
        Transform nextRoot = sets[nextIndex].root;

        // 핵심 1:
        // 다음 세트는 전환 직전에 초기 위치로 리셋합니다.
        ResetSetLocalPosition(nextIndex);

        // 선택 옵션:
        // false면 다음 세트는 항상 처음 높이에서 등장합니다.
        // true면 전환 중 높이 차이를 줄이기 위해 현재 세트 Y와 맞춥니다.
        if (alignNextSetYToCurrentOnTransition && previousRoot != null && nextRoot != null)
        {
            Vector3 nextPos = nextRoot.localPosition;
            nextPos.y = previousRoot.localPosition.y;
            nextRoot.localPosition = nextPos;
        }

        previousRoot.gameObject.SetActive(true);
        nextRoot.gameObject.SetActive(true);

        Vector3 railStart = railRoot.localPosition;
        Vector3 railEnd = railStart;

        // 핵심 2:
        // 레일은 X만 움직이고 Y/Z는 그대로 유지합니다.
        railEnd.x = GetRailXForIndex(nextIndex);

        float duration = Mathf.Max(0.01f, seconds);
        float t = 0f;

        Log($"Transition start {GetSetName(previousIndex)} -> {GetSetName(nextIndex)}");
        Log($"Previous Y={previousRoot.localPosition.y:0.00}, Next Y={nextRoot.localPosition.y:0.00}");

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);

            float k = horizontalEaseCurve != null
                ? horizontalEaseCurve.Evaluate(u)
                : u * u * (3f - 2f * u);

            Vector3 railPos = railRoot.localPosition;
            railPos.x = Mathf.Lerp(railStart.x, railEnd.x, k);
            railPos.y = railStart.y;
            railPos.z = railStart.z;
            railRoot.localPosition = railPos;

            yield return null;
        }

        railRoot.localPosition = railEnd;

        previousRoot.gameObject.SetActive(false);
        nextRoot.gameObject.SetActive(true);

        currentIndex = nextIndex;

        Log($"Transition end. Current={GetSetName(currentIndex)}");
    }

    void ResetSetLocalPosition(int index)
    {
        if (!IsValidIndex(index))
            return;

        sets[index].root.localPosition = sets[index].initialLocalPosition;
    }

    float GetRailXForIndex(int index)
    {
        return baseRailLocalX - setSpacing * index;
    }

    bool IsValidIndex(int index)
    {
        return sets != null
            && index >= 0
            && index < sets.Length
            && sets[index] != null
            && sets[index].root != null;
    }

    string GetSetName(int index)
    {
        if (!IsValidIndex(index))
            return "NULL";

        if (!string.IsNullOrEmpty(sets[index].setName))
            return sets[index].setName;

        return sets[index].root.name;
    }

    void Log(string message)
    {
        if (!debugLog)
            return;

        if (SpecialStageDebugHUD.Instance != null)
            SpecialStageDebugHUD.Log("BuildingRail", message, this);
        else
            Debug.Log($"[BuildingRail] {message}", this);
    }
}