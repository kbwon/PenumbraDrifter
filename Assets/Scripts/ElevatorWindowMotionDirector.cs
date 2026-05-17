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

    [Header("Motion")]
    public float moveSeconds = 7f;
    public float resetPauseSeconds = 0.08f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    bool moving;
    Coroutine routine;

    int originalWidth;
    int originalHeight;
    FullScreenMode originalMode;

#if !UNITY_EDITOR
    DisplayInfo displayInfo;
#endif

    public void BeginWindowMotion()
    {
        if (!useRealWindowMotion)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(WindowMotionRoutine());
    }

    public void EndWindowMotion()
    {
        moving = false;

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
        moving = true;

        originalWidth = Screen.width;
        originalHeight = Screen.height;
        originalMode = Screen.fullScreenMode;

        displayInfo = Screen.mainWindowDisplayInfo;

        Screen.SetResolution(stageWindowWidth, stageWindowHeight, FullScreenMode.Windowed);

        // SetResolution은 현재 프레임 끝에 적용되므로 한 프레임 기다립니다.
        yield return null;

        int displayWidth = displayInfo.width;
        int displayHeight = displayInfo.height;

        int x = Mathf.Max(xMargin, (displayWidth - stageWindowWidth) / 2);
        int bottomY = Mathf.Max(yMargin, displayHeight - stageWindowHeight - yMargin);
        int topY = yMargin;

        while (moving)
        {
            float t = 0f;

            while (t < moveSeconds && moving)
            {
                t += Time.deltaTime;

                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, moveSeconds));
                float eased = ease != null ? ease.Evaluate(k) : k;

                int y = Mathf.RoundToInt(Mathf.Lerp(bottomY, topY, eased));

                Screen.MoveMainWindowTo(ref displayInfo, new Vector2Int(x, y));

                // 매 프레임 호출보다 약간 간격을 두는 편이 덜 떨릴 수 있습니다.
                yield return new WaitForSeconds(0.03f);
            }

            Screen.MoveMainWindowTo(ref displayInfo, new Vector2Int(x, bottomY));

            yield return new WaitForSeconds(resetPauseSeconds);
        }
#endif
    }

    void RestoreWindow()
    {
#if !UNITY_EDITOR
        if (!useRealWindowMotion)
            return;

        if (originalWidth <= 0 || originalHeight <= 0)
            return;

        Screen.SetResolution(originalWidth, originalHeight, originalMode);
#endif
    }
}