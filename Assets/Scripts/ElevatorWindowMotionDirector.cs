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

        x = Mathf.Max(xMargin, (displayWidth - stageWindowWidth) / 2);

        // 처음 시작 위치: 모니터 아래쪽 안쪽에 딱 맞춤
        initialBottomY = Mathf.Max(yMargin, displayHeight - stageWindowHeight - yMargin);

        // 위쪽 종료 위치: 창이 완전히 모니터 위로 나간 뒤
        upperOffscreenY = -stageWindowHeight - yMargin;

        // 반복 시작 위치: 창이 완전히 모니터 아래 바깥에 있는 위치
        lowerOffscreenY = displayHeight + yMargin;

        currentY = initialBottomY;

        Screen.MoveMainWindowTo(displayInfo, new Vector2Int(x, initialBottomY));

        initialized = true;

        while (moving)
        {
            float dt = Time.unscaledDeltaTime;

            if (motionSpeed01 > 0.001f)
            {
                currentY -= maxPixelsPerSecond * motionSpeed01 * dt;

                if (currentY <= upperOffscreenY)
                {
                    currentY = lowerOffscreenY;

                    if (debugLog)
                        SpecialStageDebugHUD.Log("Window", "Window loop reset to lower offscreen.", this);
                }
            }

            if (Time.unscaledTime - lastApplyTime >= moveApplyInterval)
            {
                lastApplyTime = Time.unscaledTime;

                int y = Mathf.RoundToInt(currentY);
                Screen.MoveMainWindowTo(displayInfo, new Vector2Int(x, y)); 
            }

            yield return null;
        }
#endif
    }

#if !UNITY_EDITOR
    IEnumerator ShakeRoutine(float seconds)
    {
        float t = 0f;

        while (t < seconds && initialized && moving)
        {
            t += Time.unscaledDeltaTime;

            float sx = Mathf.Sin(Time.unscaledTime * shakeFrequency) * shakePixels;
            float sy = Mathf.Cos(Time.unscaledTime * shakeFrequency * 0.73f) * shakePixels;

            int px = x + Mathf.RoundToInt(sx);
            int py = Mathf.RoundToInt(currentY + sy);

            Screen.MoveMainWindowTo(displayInfo, new Vector2Int(px, py));

            yield return new WaitForSecondsRealtime(moveApplyInterval);
        }

        Screen.MoveMainWindowTo(displayInfo, new Vector2Int(x, Mathf.RoundToInt(currentY)));
    }
#endif

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

    void OnDisable()
    {
        EndWindowMotion();
    }

    void OnApplicationQuit()
    {
        EndWindowMotion();
    }
}