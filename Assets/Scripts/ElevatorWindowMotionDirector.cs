using System.Collections;
using UnityEngine;

public class ElevatorWindowMotionDirector : MonoBehaviour
{
    [Header("Enable")]
    public bool useRealWindowMotion = true;

    [Header("Window Size")]
    public int stageWindowWidth = 900;
    public int stageWindowHeight = 600;

    [Header("Position")]
    public int xMargin = 40;
    public int yMargin = 40;

    [Header("Synced Motion")]
    [Tooltip("이 속도 이상이면 창 이동이 최대 속도로 움직입니다. 보통 fastScrollSpeed * 1.4 정도.")]
    public float referenceElevatorSpeed = 5.6f;

    [Tooltip("창이 최대 속도일 때 초당 몇 픽셀 올라갈지.")]
    public float maxPixelsPerSecond = 260f;

    [Tooltip("MoveMainWindowTo 호출 간격. 너무 낮으면 창 이동이 떨릴 수 있습니다.")]
    public float moveApplyInterval = 0.03f;

    [Header("Shake")]
    public int shakePixels = 14;
    public float shakeFrequency = 35f;

    [Header("Native Window Move")]
    public bool useNativeWindowMoveOnWindows = true;
    public bool useNativePrimaryScreenSize = true;

    bool windowShakeActive;

    [Header("Horizontal Motion")]
    [Range(0f, 1f)]
    public float startX01 = 0.08f;

    [Tooltip("수평 전환 1회 때 화면 가로폭 기준으로 얼마나 오른쪽으로 이동할지")]
    public float horizontalStep01 = 0.28f;

    public AnimationCurve horizontalEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Vertical Edge Boost")]
    public bool useVerticalEdgeBoost = true;

    [Tooltip("창이 화면 위쪽에 가까워지면 이 Y값부터 속도를 점점 올립니다. 0보다 크면 완전히 가려지기 전부터 가속됩니다.")]
    public float topBoostStartY = 120f;

    [Tooltip("위쪽으로 사라질 때 이 픽셀 수만큼만 남으면 바로 아래쪽으로 보냅니다. 0이면 완전히 사라진 뒤 이동합니다.")]
    public float warpWhenTopVisiblePixels = 35f;

    [Tooltip("아래쪽에서 다시 나타날 때, 화면 아래 바깥으로 얼마나 숨긴 상태에서 시작할지입니다.")]
    public float bottomReappearHiddenPixels = 20f;

    [Tooltip("위/아래 가장자리에서 최대 몇 배까지 빨라질지입니다.")]
    public float maxEdgeSpeedMultiplier = 4.0f;

    public AnimationCurve verticalEdgeBoostCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    float currentX;
    int displayWidthCached;
    int displayHeightCached;

    [Header("Debug")]
    public bool debugLog = true;

    bool initialized;
    bool moving;
    bool restoring;
    Coroutine routine;
    Coroutine shakeRoutine;

    float currentY;
    float motionSpeed01;
    float lastApplyTime;

    int originalWidth;
    int originalHeight;
    FullScreenMode originalMode;

#if !UNITY_EDITOR
    DisplayInfo displayInfo;
    int x;
    int initialBottomY;
    int upperOffscreenY;
    int lowerOffscreenY;
#endif

    public bool IsInitialized => initialized;

    public void BeginWindowMotion()
    {
        if (!useRealWindowMotion)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(WindowMotionRoutine());
    }

    public void SetMotionSpeed(float elevatorSpeed)
    {
        float reference = Mathf.Max(0.01f, referenceElevatorSpeed);
        motionSpeed01 = Mathf.Clamp01(Mathf.Abs(elevatorSpeed) / reference);

        if (debugLog)
            SpecialStageDebugHUD.Log("Window", $"SetMotionSpeed elevatorSpeed={elevatorSpeed:0.00}, speed01={motionSpeed01:0.00}", this);
    }

    public void StopMotion()
    {
        motionSpeed01 = 0f;

        if (debugLog)
            SpecialStageDebugHUD.Log("Window", "StopMotion", this);
    }

    void MoveWindowTo(int px, int py)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    if (useNativeWindowMoveOnWindows)
    {
        if (NativeWindowMover.TryMove(px, py))
            return;
    }
#endif

#if !UNITY_EDITOR
    Screen.MoveMainWindowTo(displayInfo, new Vector2Int(px, py));
#endif
    }

    public IEnumerator ShakeWindow(float seconds)
    {
        if (!useRealWindowMotion)
            yield break;

#if UNITY_EDITOR
        yield break;
#else
        if (!initialized)
            yield break;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(seconds));
        yield return shakeRoutine;
        shakeRoutine = null;
#endif
    }

    public void EndWindowMotion()
    {
        moving = false;
        motionSpeed01 = 0f;

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        RestoreWindow();
    }

    IEnumerator WindowMotionRoutine()
    {
#if UNITY_EDITOR
        yield break;
#else
        initialized = false;
        moving = true;
        restoring = false;

        originalWidth = Screen.width;
        originalHeight = Screen.height;
        originalMode = Screen.fullScreenMode;

        displayInfo = Screen.mainWindowDisplayInfo;

        Screen.SetResolution(stageWindowWidth, stageWindowHeight, FullScreenMode.Windowed);

        // SetResolution은 현재 프레임 끝에 적용되므로 기다립니다.
        yield return null;
        yield return null;

        int displayWidth = displayInfo.width;
        int displayHeight = displayInfo.height;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (useNativeWindowMoveOnWindows && useNativePrimaryScreenSize)
        {
            if (NativeWindowMover.TryGetPrimaryScreenSize(out int nativeWidth, out int nativeHeight))
            {
                displayWidth = nativeWidth;
                displayHeight = nativeHeight;
            }
        }
#endif

        displayHeightCached = displayHeight;
        displayWidthCached = displayWidth;


        int minX = xMargin;
        int maxX = Mathf.Max(xMargin, displayWidth - stageWindowWidth - xMargin);

        currentX = Mathf.Lerp(minX, maxX, startX01);
        x = Mathf.RoundToInt(currentX);

        // 처음 시작 위치: 모니터 아래쪽 안쪽에 딱 맞춤
        initialBottomY = Mathf.Max(yMargin, displayHeight - stageWindowHeight - yMargin);

        // 위쪽 종료 위치: 창이 완전히 모니터 위로 나간 뒤
        upperOffscreenY = -stageWindowHeight - yMargin;

        // 반복 시작 위치: 창이 완전히 모니터 아래 바깥에 있는 위치
        lowerOffscreenY = displayHeight + yMargin;

        currentY = initialBottomY;

        MoveWindowTo(x, initialBottomY);

        initialized = true;

        while (moving)
        {
            float dt = Time.unscaledDeltaTime;

            if (motionSpeed01 > 0.001f)
{
    float speedMultiplier = GetVerticalEdgeSpeedMultiplier();
    currentY -= maxPixelsPerSecond * motionSpeed01 * speedMultiplier * dt;

    if (useVerticalEdgeBoost)
    {
        // 창이 위쪽으로 거의 다 사라졌을 때 바로 아래쪽으로 보냅니다.
        // 기존처럼 완전히 화면 밖으로 다 나갈 때까지 기다리지 않습니다.
        float topWarpY = -stageWindowHeight + warpWhenTopVisiblePixels;

        if (currentY <= topWarpY)
        {
            currentY = displayHeightCached + bottomReappearHiddenPixels;

            if (debugLog)
            {
                SpecialStageDebugHUD.Log(
                    "Window",
                    $"Fast edge wrap. currentY={currentY:0.0}, speedMul={speedMultiplier:0.00}",
                    this
                );
            }
        }
    }
    else
    {
        // 기존 방식 유지
        if (currentY <= upperOffscreenY)
        {
            currentY = lowerOffscreenY;

            if (debugLog)
                SpecialStageDebugHUD.Log("Window", "Window loop reset to lower offscreen.", this);
        }
    }
}

            if (!windowShakeActive && Time.unscaledTime - lastApplyTime >= moveApplyInterval)
            {
                lastApplyTime = Time.unscaledTime;

                int y = Mathf.RoundToInt(currentY);
                MoveWindowTo(Mathf.RoundToInt(currentX), y);
            }

            yield return null;
        }
#endif
    }

#if !UNITY_EDITOR
IEnumerator ShakeRoutine(float seconds)
{
    windowShakeActive = true;

    float t = 0f;

    while (t < seconds && initialized && moving)
    {
        t += Time.unscaledDeltaTime;

        float sx = Mathf.Sin(Time.unscaledTime * shakeFrequency) * shakePixels;
        float sy = Mathf.Cos(Time.unscaledTime * shakeFrequency * 0.73f) * shakePixels;

        int px = Mathf.RoundToInt(currentX + sx);
        int py = Mathf.RoundToInt(currentY + sy);

        MoveWindowTo(px, py);

        yield return new WaitForSecondsRealtime(moveApplyInterval);
    }

    windowShakeActive = false;
    MoveWindowTo(Mathf.RoundToInt(currentX), Mathf.RoundToInt(currentY));
}
#endif

    public IEnumerator MoveWindowHorizontalBy01(float delta01, float seconds)
    {
        if (!useRealWindowMotion)
            yield break;

#if UNITY_EDITOR
        yield break;
#else
    if (!initialized)
        yield break;

    StopMotion();

    int minX = xMargin;
    int maxX = Mathf.Max(xMargin, displayWidthCached - stageWindowWidth - xMargin);
    float widthRange = Mathf.Max(1f, maxX - minX);

    float start = currentX;
    float target = Mathf.Clamp(start + widthRange * delta01, minX, maxX);

    float duration = Mathf.Max(0.01f, seconds);
    float t = 0f;

    SpecialStageDebugHUD.Log(
        "Window",
        $"Horizontal move start. x={start:0.0} -> {target:0.0}, seconds={seconds:0.00}",
        this
    );

    while (t < duration)
    {
        t += Time.unscaledDeltaTime;
        float u = Mathf.Clamp01(t / duration);

        float k = horizontalEaseCurve != null
            ? horizontalEaseCurve.Evaluate(u)
            : u * u * (3f - 2f * u);

        currentX = Mathf.Lerp(start, target, k);

        if (!windowShakeActive && Time.unscaledTime - lastApplyTime >= moveApplyInterval)
        {
            lastApplyTime = Time.unscaledTime;
            MoveWindowTo(Mathf.RoundToInt(currentX), Mathf.RoundToInt(currentY));
        }

        yield return null;
    }

    currentX = target;
    x = Mathf.RoundToInt(currentX);

    MoveWindowTo(x, Mathf.RoundToInt(currentY));

    SpecialStageDebugHUD.Log("Window", $"Horizontal move end. x={x}", this);
#endif
    }

    public void StopWindowShake()
    {
#if !UNITY_EDITOR
    if (shakeRoutine != null)
    {
        StopCoroutine(shakeRoutine);
        shakeRoutine = null;
    }

    windowShakeActive = false;

    MoveWindowTo(Mathf.RoundToInt(currentX), Mathf.RoundToInt(currentY));
#endif
    }

    void RestoreWindow()
    {
#if !UNITY_EDITOR
        if (!useRealWindowMotion)
            return;

        if (restoring)
            return;

        restoring = true;

        if (originalWidth <= 0 || originalHeight <= 0)
            return;

        Screen.SetResolution(originalWidth, originalHeight, originalMode);

        if (debugLog)
            SpecialStageDebugHUD.Log("Window", "RestoreWindow", this);
#endif
    }

    float GetVerticalEdgeSpeedMultiplier()
    {
        if (!useVerticalEdgeBoost)
            return 1f;

        if (displayHeightCached <= 0 || stageWindowHeight <= 0)
            return 1f;

        float multiplier = 1f;

        // 1. 위쪽으로 사라지는 구간
        // currentY가 0보다 작아지면 창이 위로 잘리기 시작합니다.
        // topBoostStartY부터 미리 점점 가속합니다.
        float topWarpY = -stageWindowHeight + warpWhenTopVisiblePixels;

        if (currentY <= topBoostStartY)
        {
            float u = Mathf.InverseLerp(topBoostStartY, topWarpY, currentY);
            u = Mathf.Clamp01(u);

            float k = verticalEdgeBoostCurve != null
                ? verticalEdgeBoostCurve.Evaluate(u)
                : u * u * (3f - 2f * u);

            multiplier = Mathf.Max(multiplier, Mathf.Lerp(1f, maxEdgeSpeedMultiplier, k));
        }

        // 2. 아래쪽에서 다시 나타나는 구간
        // top-left Y가 displayHeight - windowHeight보다 크면 창 아래쪽이 화면 밖에 있습니다.
        float fullyVisibleBottomY = displayHeightCached - stageWindowHeight;

        if (currentY > fullyVisibleBottomY)
        {
            float hiddenStartY = displayHeightCached + bottomReappearHiddenPixels;

            float u = Mathf.InverseLerp(fullyVisibleBottomY, hiddenStartY, currentY);
            u = Mathf.Clamp01(u);

            float k = verticalEdgeBoostCurve != null
                ? verticalEdgeBoostCurve.Evaluate(u)
                : u * u * (3f - 2f * u);

            multiplier = Mathf.Max(multiplier, Mathf.Lerp(1f, maxEdgeSpeedMultiplier, k));
        }

        return multiplier;
    }

    void OnDisable()
    {
        EndWindowMotion();
    }

    void OnApplicationQuit()
    {
        EndWindowMotion();
    }
}