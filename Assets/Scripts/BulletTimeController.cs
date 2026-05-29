using System.Collections;
using UnityEngine;

public class BulletTimeController : MonoBehaviour
{
    public static BulletTimeController Instance { get; private set; }

    [Header("Default")]
    [Range(0.05f, 1f)]
    public float defaultScale = 0.35f;

    public float defaultEnterSeconds = 0.05f;
    public float defaultHoldSeconds = 0.55f;
    public float defaultExitSeconds = 0.12f;

    [Header("Physics")]
    public bool adjustFixedDeltaTime = true;

    [Header("Player Compensation")]
    public bool compensatePlayerMoveSpeed = false;
    public PlayerController player;

    [Tooltip("true면 timeScale이 0.35일 때 플레이어 이동 배율을 1/0.35로 보정합니다.")]
    public bool fullPlayerCompensation = true;

    [Tooltip("fullPlayerCompensation이 false일 때 사용할 보정 배율입니다.")]
    public float manualPlayerMoveMultiplier = 1.5f;

    [Header("Debug")]
    public bool debugLog;

    float originalFixedDeltaTime;
    Coroutine routine;
    int requestId;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        originalFixedDeltaTime = Time.fixedDeltaTime;

        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.player;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            RestoreImmediate();
            Instance = null;
        }
    }

    public void PlayDefault()
    {
        Play(defaultScale, defaultEnterSeconds, defaultHoldSeconds, defaultExitSeconds);
    }

    public void Play(float targetScale, float enterSeconds, float holdSeconds, float exitSeconds)
    {
        requestId++;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(BulletTimeRoutine(
            requestId,
            Mathf.Clamp(targetScale, 0.05f, 1f),
            Mathf.Max(0f, enterSeconds),
            Mathf.Max(0f, holdSeconds),
            Mathf.Max(0f, exitSeconds)
        ));
    }

    public void Stop()
    {
        requestId++;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        RestoreImmediate();
    }

    IEnumerator BulletTimeRoutine(
        int myRequestId,
        float targetScale,
        float enterSeconds,
        float holdSeconds,
        float exitSeconds)
    {
        if (debugLog)
            Debug.Log($"[BulletTime] Start scale={targetScale}", this);

        float startScale = Time.timeScale;

        yield return LerpTimeScale(startScale, targetScale, enterSeconds, myRequestId);

        ApplyPlayerCompensation(targetScale);

        if (holdSeconds > 0f)
            yield return new WaitForSecondsRealtime(holdSeconds);

        yield return LerpTimeScale(Time.timeScale, 1f, exitSeconds, myRequestId);

        RestorePlayerCompensation();
        SetTimeScale(1f);

        routine = null;

        if (debugLog)
            Debug.Log("[BulletTime] End", this);
    }

    IEnumerator LerpTimeScale(float from, float to, float seconds, int myRequestId)
    {
        if (seconds <= 0f)
        {
            SetTimeScale(to);
            yield break;
        }

        float t = 0f;

        while (t < seconds)
        {
            if (myRequestId != requestId)
                yield break;

            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / seconds);

            // SmoothStep
            u = u * u * (3f - 2f * u);

            SetTimeScale(Mathf.Lerp(from, to, u));

            yield return null;
        }

        SetTimeScale(to);
    }

    void SetTimeScale(float scale)
    {
        Time.timeScale = scale;

        if (adjustFixedDeltaTime)
            Time.fixedDeltaTime = originalFixedDeltaTime * scale;
    }

    void ApplyPlayerCompensation(float targetScale)
    {
        if (!compensatePlayerMoveSpeed)
        {
            RestorePlayerCompensation();
            return;
        }

        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.player;

        if (player == null)
            return;

        float multiplier = fullPlayerCompensation
            ? 1f / Mathf.Max(0.05f, targetScale)
            : manualPlayerMoveMultiplier;

        player.SetExternalMoveSpeedMultiplier(multiplier);
    }

    void RestorePlayerCompensation()
    {
        if (player != null)
            player.SetExternalMoveSpeedMultiplier(1f);
    }

    void RestoreImmediate()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        RestorePlayerCompensation();
    }
}