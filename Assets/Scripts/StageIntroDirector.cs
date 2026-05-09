using System.Collections;
using UnityEngine;

public class StageIntroDirector : MonoBehaviour
{
    [Header("References")]
    public FollowCamera followCamera;
    public PlayerController playerController;
    public Transform goalTarget;

    [Header("Play")]
    public bool playOnStart = true;
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Escape;

    [Header("Camera Angles")]
    public float startYaw = 0f;
    public float endYaw = 45f;

    [Header("Camera Distance")]
    public float gameplayDistanceScale = 1f;
    public float overviewDistanceScale = 1.45f;

    [Header("Orthographic Size")]
    public bool useOrthoZoom = true;
    public float gameplayOrthoSize = 0f;
    public float overviewOrthoSize = 0f;

    [Header("Overview")]
    public Transform overviewTarget;

    [Header("Timing")]
    public float zoomOutTime = 0.7f;
    public float moveToGoalTime = 1.1f;
    public float holdGoalTime = 0.55f;
    public float returnTime = 0.45f;
    public float spinTime = 1.0f;
    public float finalPause = 0.1f;

    [Header("Step Spin")]
    public bool useStepSpin = true;
    public float stepRotateTime = 0.14f;
    public float stepHoldTime = 0.08f;

    [Header("Facing")]
    public FaceCameraY[] faceCameraTargets;
    public bool faceCameraDuringSpin = true;

    [Header("Enemy Facing")]
    public bool lockEnemiesDuringIntro = true;
    public bool autoFindEnemiesForFacingLock = true;
    public EnemyController[] enemyFacingTargets;

    bool isPlaying;
    bool skipRequested;

    public bool IsPlaying => isPlaying;

    IEnumerator Start()
    {
        if (followCamera == null)
            followCamera = FindFirstObjectByType<FollowCamera>();

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        // Start에서 한 프레임 기다린 뒤 실행해 초기 참조를 안정화한다.
        yield return null;

        if (playOnStart)
            PlayIntro();
    }

    void Update()
    {
        // 연출 중에는 Esc로 바로 스킵할 수 있다.
        if (isPlaying && allowSkip && Input.GetKeyDown(skipKey))
            skipRequested = true;
    }

    public void PlayIntro()
    {
        if (isPlaying) return;

        RefreshReferences();

        if (followCamera == null) return;
        if (goalTarget == null) return;

        StartCoroutine(IntroRoutine());
    }

    public IEnumerator PlayIntroAndWait()
    {
        if (isPlaying)
        {
            while (isPlaying)
                yield return null;

            yield break;
        }

        RefreshReferences();

        if (followCamera == null) yield break;
        if (goalTarget == null) yield break;

        yield return IntroRoutine();
    }

    void RefreshReferences()
    {
        if (followCamera == null)
            followCamera = FindFirstObjectByType<FollowCamera>();

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    // 튜토리얼 구역이 바뀔 때 목표 오브젝트를 교체해 재사용한다.
    public void SetGoalTarget(Transform newGoalTarget)
    {
        goalTarget = newGoalTarget;
    }

    IEnumerator IntroRoutine()
    {
        isPlaying = true;
        skipRequested = false;

        if (playerController != null)
            playerController.SetInputLocked(true);

        // 인트로 초반에는 플레이어가 카메라를 따라 돌지 않게 한다.
        SetFacingLocked(true);

        CacheEnemyFacingTargets();

        if (lockEnemiesDuringIntro)
            SetEnemyFacingLocked(true);

        followCamera.SetCinematicMode(true);

        SetupOrthoValues();

        // 연출 시작 시 yaw 0도 기준으로 먼저 맞춘다.
        followCamera.SetGameplayYaw(startYaw);
        followCamera.SetDistanceScaleImmediate(gameplayDistanceScale);

        if (ShouldUseOrthoZoom())
            followCamera.SetOrthoSizeImmediate(gameplayOrthoSize);

        followCamera.ClearFocusOverride();
        followCamera.SnapNow();

        Vector3 playerFocus = GetPlayerFocusPoint();
        Vector3 overviewFocus = GetOverviewFocusPoint();
        Vector3 goalFocus = GetGoalFocusPoint();

        yield return AnimateSegment(
            playerFocus, overviewFocus,
            startYaw, startYaw,
            gameplayDistanceScale, overviewDistanceScale,
            gameplayOrthoSize, overviewOrthoSize,
            zoomOutTime);

        if (skipRequested) { FinishIntro(); yield break; }

        playerFocus = GetPlayerFocusPoint();
        goalFocus = GetGoalFocusPoint();

        yield return AnimateSegment(
            overviewFocus, goalFocus,
            startYaw, startYaw,
            overviewDistanceScale, overviewDistanceScale,
            overviewOrthoSize, overviewOrthoSize,
            moveToGoalTime);

        if (skipRequested) { FinishIntro(); yield break; }

        yield return WaitSegment(holdGoalTime);

        if (skipRequested) { FinishIntro(); yield break; }

        playerFocus = GetPlayerFocusPoint();
        goalFocus = GetGoalFocusPoint();

        yield return AnimateSegment(
            goalFocus, playerFocus,
            startYaw, startYaw,
            overviewDistanceScale, gameplayDistanceScale,
            overviewOrthoSize, gameplayOrthoSize,
            returnTime);

        if (skipRequested) { FinishIntro(); yield break; }

        // 회전 프리뷰 구간에서는 플레이어가 다시 카메라를 바라보게 한다.
        if (faceCameraDuringSpin)
            SetFacingLocked(false);

        // 한 바퀴 돌고 45도에서 멈춰 화면 회전의 중요성을 보여준다.
        playerFocus = GetPlayerFocusPoint();

        if (useStepSpin)
        {
            yield return RotateStepPreview(playerFocus);
        }
        else
        {
            float spinEndYaw = startYaw + 360f + Mathf.DeltaAngle(startYaw, endYaw);

            yield return AnimateSegment(
                playerFocus, playerFocus,
                startYaw, spinEndYaw,
                gameplayDistanceScale, gameplayDistanceScale,
                gameplayOrthoSize, gameplayOrthoSize,
                spinTime);
        }

        if (skipRequested) { FinishIntro(); yield break; }

        yield return WaitSegment(finalPause);

        FinishIntro();
    }

    IEnumerator AnimateSegment(
        Vector3 fromFocus,
        Vector3 toFocus,
        float fromYaw,
        float toYaw,
        float fromDistanceScale,
        float toDistanceScale,
        float fromOrthoSize,
        float toOrthoSize,
        float duration)
    {
        if (duration <= 0f)
        {
            ApplyPose(toFocus, toYaw, toDistanceScale, toOrthoSize);
            yield break;
        }

        float t = 0f;

        while (t < duration)
        {
            if (skipRequested) yield break;

            t += Time.deltaTime;
            float k = EaseInOut(Mathf.Clamp01(t / duration));

            Vector3 focus = Vector3.Lerp(fromFocus, toFocus, k);
            float yaw = Mathf.Lerp(fromYaw, toYaw, k);
            float dist = Mathf.Lerp(fromDistanceScale, toDistanceScale, k);
            float ortho = Mathf.Lerp(fromOrthoSize, toOrthoSize, k);

            ApplyPose(focus, yaw, dist, ortho);
            yield return null;
        }

        ApplyPose(toFocus, toYaw, toDistanceScale, toOrthoSize);
    }

    IEnumerator WaitSegment(float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            if (skipRequested) yield break;

            t += Time.deltaTime;
            yield return null;
        }
    }
    IEnumerator RotateStepPreview(Vector3 focusPoint)
    {
        float step = 45f;

        if (followCamera != null)
            step = Mathf.Abs(followCamera.stepAngle);

        float yaw = startYaw;

        // 0도 화면은 이미 복귀 직후 보여졌으니 315도까지만 프리뷰한다.
        int previewCount = Mathf.Max(1, Mathf.RoundToInt(360f / step) - 1);

        for (int i = 0; i < previewCount; i++)
        {
            float nextYaw = yaw + step;

            yield return AnimateSegment(
                focusPoint, focusPoint,
                yaw, nextYaw,
                gameplayDistanceScale, gameplayDistanceScale,
                gameplayOrthoSize, gameplayOrthoSize,
                stepRotateTime);

            if (skipRequested) yield break;

            yield return WaitSegment(stepHoldTime);

            if (skipRequested) yield break;

            yaw = nextYaw;
            focusPoint = GetPlayerFocusPoint();
        }

        // 마지막은 플레이 시작 각도인 45도로 맞춘다.
        float finalYaw = startYaw + 360f + Mathf.DeltaAngle(startYaw, endYaw);

        yield return AnimateSegment(
            focusPoint, focusPoint,
            yaw, finalYaw,
            gameplayDistanceScale, gameplayDistanceScale,
            gameplayOrthoSize, gameplayOrthoSize,
            stepRotateTime);
    }
    void ApplyPose(Vector3 focusPoint, float yaw, float distanceScale, float orthoSize)
    {
        followCamera.SetFocusPoint(focusPoint);
        followCamera.SetYawImmediate(yaw);
        followCamera.SetDistanceScaleImmediate(distanceScale);

        if (ShouldUseOrthoZoom())
            followCamera.SetOrthoSizeImmediate(orthoSize);

        followCamera.SnapNow();
    }

    void FinishIntro()
    {
        followCamera.SetGameplayYaw(endYaw);
        followCamera.SetDistanceScaleImmediate(gameplayDistanceScale);

        if (ShouldUseOrthoZoom())
            followCamera.SetOrthoSizeImmediate(gameplayOrthoSize);

        followCamera.ClearFocusOverride();
        followCamera.SnapNow();
        followCamera.SetCinematicMode(false);

        // 인트로 종료 후에는 기본적으로 카메라를 다시 바라보게 한다.
        SetFacingLocked(false);
        SetEnemyFacingLocked(false);

        if (playerController != null)
            playerController.SetInputLocked(false);

        isPlaying = false;
        skipRequested = false;
    }

    void SetupOrthoValues()
    {
        if (!ShouldUseOrthoZoom()) return;

        if (gameplayOrthoSize <= 0f)
            gameplayOrthoSize = followCamera.CachedCamera.orthographicSize;

        if (overviewOrthoSize <= 0f)
            overviewOrthoSize = gameplayOrthoSize * 1.35f;
    }

    bool ShouldUseOrthoZoom()
    {
        return useOrthoZoom
            && followCamera != null
            && followCamera.CachedCamera != null
            && followCamera.CachedCamera.orthographic;
    }

    Vector3 GetPlayerFocusPoint()
    {
        return followCamera != null
            ? followCamera.GetGameplayFocusPoint()
            : Vector3.zero;
    }

    // 목표 오브젝트는 카메라가 바로 바라볼 위치에 둔다.
    Vector3 GetGoalFocusPoint()
    {
        return goalTarget != null
            ? goalTarget.position
            : GetPlayerFocusPoint();
    }

    Vector3 GetOverviewFocusPoint()
    {
        if (overviewTarget != null)
            return overviewTarget.position;

        Vector3 playerFocus = GetPlayerFocusPoint();
        Vector3 goalFocus = GetGoalFocusPoint();

        // overviewTarget이 없으면 플레이어와 목표의 중간을 기본값으로 사용한다.
        return Vector3.Lerp(playerFocus, goalFocus, 0.5f);
    }

    void SetFacingLocked(bool locked)
    {
        if (playerController != null)
            playerController.SetBillboardLocked(locked);

        if (faceCameraTargets == null) return;

        foreach (var faceCam in faceCameraTargets)
        {
            if (faceCam != null)
                faceCam.SetFacingLocked(locked);
        }
    }
    float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    void OnDisable()
    {
        if (!isPlaying) return;

        if (followCamera != null)
        {
            followCamera.ClearFocusOverride();
            followCamera.SetCinematicMode(false);
        }

        SetFacingLocked(false);
        SetEnemyFacingLocked(false);

        if (playerController != null)
            playerController.SetInputLocked(false);

        isPlaying = false;
        skipRequested = false;
    }

    void CacheEnemyFacingTargets()
    {
        if (!autoFindEnemiesForFacingLock)
            return;

        enemyFacingTargets = FindObjectsByType<EnemyController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
    }

    void SetEnemyFacingLocked(bool locked)
    {
        if (enemyFacingTargets == null)
            return;

        for (int i = 0; i < enemyFacingTargets.Length; i++)
        {
            EnemyController enemy = enemyFacingTargets[i];

            if (enemy != null)
                enemy.SetBillboardLocked(locked);
        }
    }
}