using System.Collections;
using UnityEngine;

public class ElevatorStageDirector : MonoBehaviour
{
    [Header("Refs")]
    public PlayerController player;
    public FollowCamera followCamera;
    public ElevatorDoorController door;
    public ElevatorWaveSpawner waveSpawner;
    public ElevatorExteriorScroller exteriorScroller;
    public ElevatorWindowMotionDirector windowMotion;

    [Header("Waves")]
    public ElevatorWave wave01;
    public ElevatorWave wave02;
    public ElevatorWave finalWave;

    [Header("Camera")]
    public Transform elevatorCameraFocus;
    public float fixedCameraYaw = -90f;
    public float gameplayOrthoSize = 7f;
    public bool useElevatorFocusOverride = true;

    [Header("Elevator Timing")]
    public float startDelay = 0.5f;
    public float ascend01Seconds = 8f;
    public float ascend02Seconds = 10f;
    public float malfunctionSeconds = 4f;
    public float ascendFinalSeconds = 8f;

    [Header("Scroll Speed")]
    public float normalScrollSpeed = 2.5f;
    public float fastScrollSpeed = 4f;

    [Header("Elevator Motion Feel")]
    public float accelSeconds = 1.2f;
    public float decelSeconds = 1.0f;
    public AnimationCurve elevatorEaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Building Set Rail")]
    public ElevatorBuildingSetRailController buildingSetRail;

    [Header("Horizontal Transition Tuning")]
    public float horizontalTransitionSeconds = 3.0f;
    public float horizontalWindowStep01 = 0.12f;
    public float horizontalWindowStep02 = 0.10f;
    public float horizontalPrePauseSeconds = 0.15f;
    public float horizontalPostPauseSeconds = 0.2f;

    [Header("Horizontal Transition Shake")]
    public float horizontalCameraShakeSeconds = 2.5f;
    public float horizontalWindowShakeSeconds = 0.35f;
    public float horizontalEffectStartDelaySeconds = 0.1f;
    public bool stopWindowShakeAtHorizontalEnd = true;

    [Header("Clear Exit")]
    public float arrivePauseSeconds = 0.5f;
    public float exitWalkSeconds = 0.8f;
    public float exitWalkSpeed = 2.5f;

    [Header("Background Phases")]
    public ElevatorBackgroundPhaseController backgroundPhases;
    public int lowPhaseIndex = 0;
    public int midPhaseIndex = 1;
    public int highPhaseIndex = 2;

    [Header("Special Camera")]
    public SpecialStageCameraFramer cameraFramer;
    public bool useSpecialCameraFramer = true;
    public bool lockPlayerBillboardDuringSpecialCamera = false;

    [Header("Player Visual Facing Lock")]
    public FaceCameraY playerVisualFaceCameraY;
    public bool lockPlayerVisualFaceCameraYDuringSpecialCamera = true;

    [Header("Window Restore")]
    public float windowRestoreWaitSeconds = 0.8f;

    [Header("Next Scene")]
    public string nextSceneName = "Stage_Rooftop";
    public string nextEntryId = "Rooftop_Start";
    public Vector3 exitDirection = Vector3.forward;

    [Header("Opening Dialogue")]
    public DialogueManager dialogue;
    public bool playOpeningDialogue = true;
    public DialogueLine[] openingLines;

    public ElevatorMotionFX motionFX;
    bool playing;

    [Header("Opening Skip")]
    public bool allowOpeningSkip = true;
    public KeyCode openingSkipKey = KeyCode.Escape;

    bool openingDialoguePlaying;

    IEnumerator Start()
    {
        yield return null;
        ResolveRefs();

        if (!playing)
            StartCoroutine(StageRoutine());
    }

    void Update()
    {
        if (!openingDialoguePlaying)
            return;

        if (!allowOpeningSkip)
            return;

        if (Input.GetKeyDown(openingSkipKey))
        {
            if (dialogue != null)
                dialogue.RequestSkipAll();

            SpecialStageDebugHUD.Log("Stage", "Opening dialogue skipped.", this);
        }
    }

    void ResolveRefs()
    {
        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.player;

        if (followCamera == null && GameManager.Instance != null)
            followCamera = GameManager.Instance.followCamera;

        if (door == null)
            door = FindFirstObjectByType<ElevatorDoorController>();

        if (waveSpawner == null)
            waveSpawner = FindFirstObjectByType<ElevatorWaveSpawner>();

        if (exteriorScroller == null)
            exteriorScroller = FindFirstObjectByType<ElevatorExteriorScroller>();

        if (windowMotion == null)
            windowMotion = FindFirstObjectByType<ElevatorWindowMotionDirector>();

        if (motionFX == null)
            motionFX = FindFirstObjectByType<ElevatorMotionFX>();

        if (backgroundPhases == null)
            backgroundPhases = FindFirstObjectByType<ElevatorBackgroundPhaseController>();

        if (cameraFramer == null)
            cameraFramer = FindFirstObjectByType<SpecialStageCameraFramer>();

        if (playerVisualFaceCameraY == null && player != null)
            playerVisualFaceCameraY = player.GetComponentInChildren<FaceCameraY>(true);

        if (buildingSetRail == null)
            buildingSetRail = FindFirstObjectByType<ElevatorBuildingSetRailController>();

        if (dialogue == null)
            dialogue = DialogueManager.Instance != null
                ? DialogueManager.Instance
                : FindFirstObjectByType<DialogueManager>();
    }

    IEnumerator StageRoutine()
    {
        playing = true;

        SpecialStageDebugHUD.Step("StageRoutine started", this);

        ResolveRefs();

        SpecialStageDebugHUD.Step("Setup camera", this);
        SetupCameraForElevator();

        // 카메라가 특수 스테이지 각도/위치로 잡힌 뒤,
        // FaceCameraY가 한 번 올바른 방향으로 회전할 시간을 줍니다.
        yield return null;

        LockPlayerVisualFacingForSpecialStage();

        if (backgroundPhases != null)
        {
            backgroundPhases.SetPhaseImmediate(lowPhaseIndex);
            SpecialStageDebugHUD.Log("Stage", "Background phase set to Low.", this);
        }

        if (GameManager.Instance != null && GameManager.Instance.shadow != null)
        {
            GameManager.Instance.shadow.ForceExitShadowMode();
            GameManager.Instance.shadow.ClearSurfaceAnchor();
            GameManager.Instance.shadow.ClearMovingShadowHost();
            SpecialStageDebugHUD.Log("Stage", "Player shadow mode cleared.", this);
        }

        if (player != null)
        {
            player.SetInputLocked(true);
            SpecialStageDebugHUD.Log("Stage", "Player input locked for start.", this);
        }

        yield return new WaitForSeconds(startDelay);

        if (door != null)
        {
            SpecialStageDebugHUD.Step("Initial door close", this);
            yield return door.Close();
        }


        yield return PlayOpeningDialogue();

        if (player != null)
        {
            player.SetInputLocked(false);
            SpecialStageDebugHUD.Log("Stage", "Player input unlocked.", this);
        }

        if (windowMotion != null)
        {
            SpecialStageDebugHUD.Step("Window motion begin", this);
            windowMotion.BeginWindowMotion();
        }
        else
        {
            SpecialStageDebugHUD.Log("Stage", "Window motion is None. Skipped.", this);
        }

        SpecialStageDebugHUD.Step("Ascend 01", this);
        yield return AscendSegmentSmooth(ascend01Seconds, normalScrollSpeed, true, true);

        yield return StopOpenSpawnClose(wave01);

        SpecialStageDebugHUD.Step("Combat during ascend: Wave01", this);
        yield return AscendUntilWaveCleared(wave01, normalScrollSpeed);

        yield return HorizontalTransitionSegment("Horizontal Transition 01", midPhaseIndex, 1, horizontalWindowStep01);

        SpecialStageDebugHUD.Step("Ascend 02", this);
        yield return AscendSegmentSmooth(ascend02Seconds, fastScrollSpeed, true, true);

        yield return StopOpenSpawnClose(wave02);

        SpecialStageDebugHUD.Step("Combat during ascend: Wave02", this);
        yield return AscendUntilWaveCleared(wave02, fastScrollSpeed);

        yield return HorizontalTransitionSegment("Horizontal Transition 02", highPhaseIndex, 2, horizontalWindowStep02);

        SpecialStageDebugHUD.Step("Final ascend", this);
        yield return AscendSegmentSmooth(ascendFinalSeconds, normalScrollSpeed, true, true);

        yield return StopOpenSpawnClose(finalWave);

        SpecialStageDebugHUD.Step("Combat during ascend: FinalWave", this);
        yield return AscendUntilWaveCleared(finalWave, normalScrollSpeed);

        SpecialStageDebugHUD.Step("Final wave cleared. Arriving at destination.", this);
        yield return ClearStage();
    }

    void SetupCameraForElevator()
    {
        if (followCamera == null)
            return;

        followCamera.SetCinematicMode(true);
        followCamera.SetGameplayYaw(fixedCameraYaw);
        followCamera.SetYawImmediate(fixedCameraYaw);

        if (useElevatorFocusOverride && elevatorCameraFocus != null)
            followCamera.SetFocusPoint(elevatorCameraFocus.position);

        if (gameplayOrthoSize > 0f)
            followCamera.SetOrthoSizeImmediate(gameplayOrthoSize);

        followCamera.SnapNow();
        if (useSpecialCameraFramer && cameraFramer != null)
        {
            cameraFramer.Begin();
        }
    }

    IEnumerator AscendSegmentSmooth(float seconds, float targetSpeed, bool accelerateAtStart, bool decelerateAtEnd)
    {
        SpecialStageDebugHUD.Log("Stage", $"Smooth ascend start. seconds={seconds}, targetSpeed={targetSpeed}", this);

        if (exteriorScroller == null)
        {
            SpecialStageDebugHUD.Warn("Stage", "ExteriorScroller is not assigned.", this);
            yield return new WaitForSeconds(seconds);
            yield break;
        }

        if (exteriorScroller != null)
        {
            exteriorScroller.Play(accelerateAtStart ? 0f : targetSpeed);
            SyncWindowSpeedToScroller();
            SyncBuildingSetSpeedToScroller();
        }

        if (buildingSetRail != null)
        {
            float startSpeed = accelerateAtStart ? 0f : targetSpeed;
            buildingSetRail.PlayVertical(startSpeed);
        }

        if (accelerateAtStart)
            yield return ChangeScrollSpeedSmooth(targetSpeed, accelSeconds);

        float cruiseSeconds = seconds;

        if (accelerateAtStart)
            cruiseSeconds -= accelSeconds;

        if (decelerateAtEnd)
            cruiseSeconds -= decelSeconds;

        cruiseSeconds = Mathf.Max(0f, cruiseSeconds);

        yield return new WaitForSeconds(cruiseSeconds);

        if (decelerateAtEnd)
            yield return ChangeScrollSpeedSmooth(0f, decelSeconds);

        SpecialStageDebugHUD.Log("Stage", "Smooth ascend end.", this);
    }

    IEnumerator StopOpenSpawnClose(ElevatorWave wave)
    {
        SpecialStageDebugHUD.Step($"Stop and spawn wave: {(wave != null ? wave.waveName : "NULL")}", this);

        if (exteriorScroller != null)
        {
            yield return ChangeScrollSpeedSmooth(0f, decelSeconds);
            exteriorScroller.Stop();
            StopWindowMotionOnly();

            if (buildingSetRail != null)
                buildingSetRail.StopVertical();

            SpecialStageDebugHUD.Log("Stage", "Elevator stopped for door event.", this);
        }

        if (door != null)
        {
            SpecialStageDebugHUD.Log("Stage", "Door opening.", this);
            yield return door.Open();
        }

        yield return new WaitForSeconds(0.25f);

        if (waveSpawner != null)
        {
            SpecialStageDebugHUD.Log("Stage", "Wave spawning and enemy entry started.", this);
            yield return waveSpawner.SpawnWaveAndWaitEntry(wave);
            SpecialStageDebugHUD.Log("Stage", "Wave enemy entry finished.", this);
        }

        yield return new WaitForSeconds(0.2f);

        if (door != null)
        {
            SpecialStageDebugHUD.Log("Stage", "Door closing after enemy entry.", this);
            yield return door.Close();
        }

        SpecialStageDebugHUD.Log("Stage", "Door event finished. Elevator can move again.", this);
    }

    IEnumerator AscendUntilWaveCleared(ElevatorWave wave, float targetSpeed)
    {
        if (exteriorScroller != null)
        {
            exteriorScroller.Play(0f);
            SyncWindowSpeedToScroller();

            if (buildingSetRail != null)
                buildingSetRail.PlayVertical(0f);

            yield return ChangeScrollSpeedSmooth(targetSpeed, accelSeconds);
        }

        SpecialStageDebugHUD.Step($"Combat while moving: {(wave != null ? wave.waveName : "NULL")}", this);

        while (waveSpawner != null && !waveSpawner.IsWaveCleared())
            yield return null;

        SpecialStageDebugHUD.Log("Stage", $"Wave cleared while elevator moving: {(wave != null ? wave.waveName : "NULL")}", this);
    }

    IEnumerator ChangeScrollSpeedSmooth(float targetSpeed, float duration)
    {
        if (exteriorScroller == null)
            yield break;

        float startSpeed = exteriorScroller.speed;

        if (!exteriorScroller.gameObject.activeInHierarchy)
            yield break;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));

            float k = elevatorEaseCurve != null
                ? elevatorEaseCurve.Evaluate(u)
                : u * u * (3f - 2f * u);

            exteriorScroller.speed = Mathf.Lerp(startSpeed, targetSpeed, k);
            SyncWindowSpeedToScroller();
            SyncBuildingSetSpeedToScroller();

            yield return null;
        }

        exteriorScroller.speed = targetSpeed;
        SyncWindowSpeedToScroller();
        SyncBuildingSetSpeedToScroller();
    }

    IEnumerator ClearStage()
    {
        SpecialStageDebugHUD.Step("Arriving at final floor", this);

        if (player != null)
            player.SetInputLocked(true);

        // 마지막 도착 감속
        if (exteriorScroller != null)
        {
            SpecialStageDebugHUD.Log("Stage", "Final deceleration started.", this);

            // 현재 움직이고 있는 상태를 유지한 채 0으로 감속
            yield return ChangeScrollSpeedSmooth(0f, decelSeconds);

            exteriorScroller.Stop();
            SpecialStageDebugHUD.Log("Stage", "Elevator stopped at destination.", this);
        }

        yield return new WaitForSeconds(arrivePauseSeconds);

        if (door != null)
        {
            SpecialStageDebugHUD.Step("Opening final door", this);
            yield return door.Open();
        }

        // 플레이어가 엘리베이터 밖으로 나가는 연출
        if (player != null)
        {
            SpecialStageDebugHUD.Step("Player exits elevator", this);

            player.BeginScriptedMove(exitDirection, exitWalkSpeed);

            yield return new WaitForSeconds(exitWalkSeconds);

            player.EndScriptedMove(true);
        }

        if (windowMotion != null)
        {
            windowMotion.EndWindowMotion();
            SpecialStageDebugHUD.Log("Stage", "Window motion ended and restored.", this);

            // 창이 실제로 전체화면/원래 해상도로 돌아오는 것을 보여주기 위한 대기
            yield return new WaitForSecondsRealtime(windowRestoreWaitSeconds);
        }

        if (followCamera != null)
            followCamera.SetCinematicMode(false);

        if (cameraFramer != null)
            cameraFramer.End();

        if (player != null)
            player.SetBillboardLocked(false);

        if (playerVisualFaceCameraY != null)
            playerVisualFaceCameraY.SetFacingLocked(false);

        SpecialStageDebugHUD.Step("Start scene transition", this);

        if (SceneTransitionDirector.Instance != null)
        {
            SceneTransitionDirector.Instance.StartStageTransition(
                nextSceneName,
                nextEntryId,
                exitDirection
            );
        }
        else
        {
            SpecialStageDebugHUD.Warn("Stage", "SceneTransitionDirector.Instance is null. Cannot transition.", this);
        }

        playing = false;
    }

    IEnumerator HorizontalTransitionSegment(
    string label,
    int nextBackgroundPhaseIndex,
    int nextBuildingSetIndex,
    float windowStep01)
    {
        SpecialStageDebugHUD.Step(label, this);

        // 1. 수직 이동 정지
        if (exteriorScroller != null)
        {
            yield return ChangeScrollSpeedSmooth(0f, 0.35f);
            exteriorScroller.Stop();
            StopWindowMotionOnly();

            SpecialStageDebugHUD.Log("Stage", $"{label}: vertical scroll stopped.", this);
        }

        yield return new WaitForSecondsRealtime(horizontalPrePauseSeconds);

        // 2. 내부 흔들림과 실제 창 흔들림을 분리합니다.
        // 에디터에서는 motionFX만 보이고, 빌드에서는 windowMotion도 보입니다.
        if (motionFX != null && horizontalCameraShakeSeconds > 0f)
            StartCoroutine(motionFX.Shake(horizontalCameraShakeSeconds));

        if (windowMotion != null && horizontalWindowShakeSeconds > 0f)
            StartCoroutine(windowMotion.ShakeWindow(horizontalWindowShakeSeconds));

        // 흔들림 길이에 따라 전환 시작이 늦어지지 않도록 고정 딜레이 사용
        yield return new WaitForSecondsRealtime(horizontalEffectStartDelaySeconds);

        if (backgroundPhases != null)
        {
            backgroundPhases.SetPhase(nextBackgroundPhaseIndex);
            SpecialStageDebugHUD.Log(
                "Stage",
                $"{label}: background phase changed to {nextBackgroundPhaseIndex}.",
                this
            );
        }

        // 4. 실제 창은 오른쪽으로 조금 이동
        if (windowMotion != null)
        {
            StartCoroutine(windowMotion.MoveWindowHorizontalBy01(
                windowStep01,
                horizontalTransitionSeconds
            ));
        }

        if (buildingSetRail != null)
            buildingSetRail.StopVertical();

        // 5. 건물 레일은 왼쪽으로 천천히 이동
        if (buildingSetRail != null)
        {
            StartCoroutine(buildingSetRail.TransitionTo(
                nextBuildingSetIndex,
                horizontalTransitionSeconds
            ));
        }

        yield return new WaitForSecondsRealtime(horizontalTransitionSeconds);

        if (stopWindowShakeAtHorizontalEnd && windowMotion != null)
            windowMotion.StopWindowShake();
        
        yield return new WaitForSecondsRealtime(horizontalPostPauseSeconds);

        SpecialStageDebugHUD.Step($"{label} end", this);
    }

    void LockPlayerVisualFacingForSpecialStage()
    {
        if (player != null && lockPlayerBillboardDuringSpecialCamera)
        {
            player.SetBillboardLocked(true);
            SpecialStageDebugHUD.Log("Stage", "PlayerController billboard locked.", this);
        }

        if (playerVisualFaceCameraY != null && lockPlayerVisualFaceCameraYDuringSpecialCamera)
        {
            playerVisualFaceCameraY.SetFacingLocked(true);
            SpecialStageDebugHUD.Log("Stage", "Player Visual FaceCameraY locked.", playerVisualFaceCameraY);
        }
    }
    void SyncWindowSpeedToScroller()
    {
        if (windowMotion == null || exteriorScroller == null)
            return;

        windowMotion.SetMotionSpeed(exteriorScroller.speed);
    }

    void StopWindowMotionOnly()
    {
        if (windowMotion == null)
            return;

        windowMotion.StopMotion();
    }

    void SyncBuildingSetSpeedToScroller()
    {
        if (buildingSetRail == null || exteriorScroller == null)
            return;

        buildingSetRail.SetVerticalSpeed(exteriorScroller.speed);
    }

    IEnumerator PlayOpeningDialogue()
    {
        if (!playOpeningDialogue)
            yield break;

        if (dialogue == null)
            yield break;

        if (openingLines == null || openingLines.Length == 0)
            yield break;

        openingDialoguePlaying = true;

        yield return dialogue.Show(
            openingLines,
            lockPlayer: false,
            forceExitShadow: false,
            pauseGame: false
        );

        openingDialoguePlaying = false;
    }

    IEnumerator ShakeGameAndWindow(float seconds)
    {
        if (motionFX != null)
            StartCoroutine(motionFX.Shake(seconds));

        if (windowMotion != null)
            StartCoroutine(windowMotion.ShakeWindow(seconds));

        // 어느 쪽 코루틴이 멈추더라도 스테이지 진행은 보장
        yield return new WaitForSecondsRealtime(seconds);

        SpecialStageDebugHUD.Log("Stage", "ShakeGameAndWindow finished by realtime timer.", this);
    }
}