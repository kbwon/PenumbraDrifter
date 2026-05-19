using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class ElevatorBackgroundPhase
{
    public string phaseName;
    public Transform root;

    [Tooltip("비워두면 root 아래 SpriteRenderer를 자동으로 찾습니다.")]
    public SpriteRenderer[] renderers;

    [Header("Option")]
    public bool resetPositionOnEnter = false;

    [HideInInspector] public Vector3 startPosition;
}

public class ElevatorBackgroundPhaseController : MonoBehaviour
{
    [Header("Phases")]
    public ElevatorBackgroundPhase[] phases;

    [Header("Fade")]
    public float fadeSeconds = 0.5f;
    public bool useFade = true;

    [Header("Debug")]
    public bool debugLog = true;

    Coroutine fadeRoutine;
    int currentPhaseIndex = -1;

    void Awake()
    {
        CachePhases();

        // 시작 시 전부 투명 처리
        for (int i = 0; i < phases.Length; i++)
            SetPhaseAlpha(i, 0f);
    }

    void CachePhases()
    {
        if (phases == null)
            return;

        for (int i = 0; i < phases.Length; i++)
        {
            ElevatorBackgroundPhase phase = phases[i];
            if (phase == null || phase.root == null)
                continue;

            phase.startPosition = phase.root.position;

            if (phase.renderers == null || phase.renderers.Length == 0)
                phase.renderers = phase.root.GetComponentsInChildren<SpriteRenderer>(true);
        }
    }

    public void SetPhaseImmediate(int index)
    {
        if (!IsValidIndex(index))
            return;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        currentPhaseIndex = index;

        for (int i = 0; i < phases.Length; i++)
            SetPhaseAlpha(i, i == index ? 1f : 0f);

        if (phases[index].resetPositionOnEnter && phases[index].root != null)
            phases[index].root.position = phases[index].startPosition;

        Log($"Set phase immediate: {GetPhaseName(index)}");
    }

    public void SetPhase(int index)
    {
        if (!IsValidIndex(index))
            return;

        if (!useFade || fadeSeconds <= 0f)
        {
            SetPhaseImmediate(index);
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeToPhase(index));
    }

    IEnumerator FadeToPhase(int index)
    {
        Log($"Fade phase start: {GetPhaseName(index)}");

        if (phases[index].resetPositionOnEnter && phases[index].root != null)
            phases[index].root.position = phases[index].startPosition;

        float[] startAlphas = new float[phases.Length];

        for (int i = 0; i < phases.Length; i++)
            startAlphas[i] = GetPhaseAlpha(i);

        float t = 0f;

        while (t < fadeSeconds)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds));
            k = k * k * (3f - 2f * k);

            for (int i = 0; i < phases.Length; i++)
            {
                float target = i == index ? 1f : 0f;
                float alpha = Mathf.Lerp(startAlphas[i], target, k);
                SetPhaseAlpha(i, alpha);
            }

            yield return null;
        }

        for (int i = 0; i < phases.Length; i++)
            SetPhaseAlpha(i, i == index ? 1f : 0f);

        currentPhaseIndex = index;
        fadeRoutine = null;

        Log($"Fade phase complete: {GetPhaseName(index)}");
    }

    void SetPhaseAlpha(int index, float alpha)
    {
        if (!IsValidIndex(index, false))
            return;

        SpriteRenderer[] renderers = phases[index].renderers;
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null) continue;

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;

            // alpha가 0이어도 오브젝트는 활성 상태로 둡니다.
            // 그래야 Scroller가 루트 Transform을 계속 움직일 수 있습니다.
            sr.enabled = alpha > 0.001f;
        }
    }

    float GetPhaseAlpha(int index)
    {
        if (!IsValidIndex(index, false))
            return 0f;

        SpriteRenderer[] renderers = phases[index].renderers;
        if (renderers == null || renderers.Length == 0 || renderers[0] == null)
            return 0f;

        return renderers[0].color.a;
    }

    bool IsValidIndex(int index, bool warn = true)
    {
        bool valid = phases != null && index >= 0 && index < phases.Length && phases[index] != null;

        if (!valid && warn)
            Log($"Invalid phase index: {index}");

        return valid;
    }

    string GetPhaseName(int index)
    {
        if (!IsValidIndex(index, false))
            return "Invalid";

        return string.IsNullOrEmpty(phases[index].phaseName)
            ? $"Phase {index}"
            : phases[index].phaseName;
    }

    void Log(string message)
    {
        if (!debugLog)
            return;

        if (SpecialStageDebugHUD.Instance != null)
            SpecialStageDebugHUD.Log("BGPhase", message, this);
        else
            Debug.Log($"[BGPhase] {message}", this);
    }
}