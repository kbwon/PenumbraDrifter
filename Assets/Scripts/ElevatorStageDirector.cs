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
    public float fixedCameraYaw = 45f;
    public float gameplayOrthoSize = 7f;

    [Header("Elevator Timing")]
    public float startDelay = 0.5f;
    public float ascend01Seconds = 8f;
    public float ascend02Seconds = 10f;
    public float malfunctionSeconds = 4f;
    public float ascendFinalSeconds = 8f;

    [Header("Scroll Speed")]
    public float normalScrollSpeed = 2.5f;
    public float fastScrollSpeed = 4f;

    [Header("Next Scene")]
    public string nextSceneName = "Stage_Rooftop";
    public string nextEntryId = "Rooftop_Start";
    public Vector3 exitDirection = Vector3.forward;

    public ElevatorMotionFX motionFX;
    bool playing;

    IEnumerator Start()
    {
        yield return null;
        ResolveRefs();

        if (!playing)
            StartCoroutine(StageRoutine());
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
    }

    IEnumerator StageRoutine()
    {
        playing = true;

        SpecialStageDebugHUD.Step("StageRoutine started", this);

        ResolveRefs();

        SpecialStageDebugHUD.Step("Setup camera", this);
        SetupCameraForElevator();

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
        yield return AscendSegment(ascend01Seconds, normalScrollSpeed);

        yield return StopAndSpawnWave(wave01);

        SpecialStageDebugHUD.Step("Ascend 02", this);
        yield return AscendSegment(ascend02Seconds, fastScrollSpeed);

        yield return StopAndSpawnWave(wave02);

        SpecialStageDebugHUD.Step("Malfunction", this);
        yield return MalfunctionSegment();

        SpecialStageDebugHUD.Step("Final ascend", this);
        yield return AscendSegment(ascendFinalSeconds, normalScrollSpeed);

        yield return StopAndSpawnWave(finalWave);

        SpecialStageDebugHUD.Step("Clear stage", this);
        yield return ClearStage();
    }

    void SetupCameraForElevator()
    {
        if (followCamera == null)
            return;

        followCamera.SetCinematicMode(true);
        followCamera.SetGameplayYaw(fixedCameraYaw);
        followCamera.SetYawImmediate(fixedCameraYaw);

        if (gameplayOrthoSize > 0f)
            followCamera.SetOrthoSizeImmediate(gameplayOrthoSize);

        followCamera.SnapNow();
    }

    IEnumerator AscendSegment(float seconds, float scrollSpeed)
    {
        SpecialStageDebugHUD.Log("Stage", $"Ascend start. seconds={seconds}, scrollSpeed={scrollSpeed}", this);

        if (exteriorScroller != null)
            exteriorScroller.Play(scrollSpeed);
        else
            SpecialStageDebugHUD.Warn("Stage", "ExteriorScroller is not assigned.", this);

        yield return new WaitForSeconds(seconds);

        if (exteriorScroller != null)
            exteriorScroller.Stop();

        SpecialStageDebugHUD.Log("Stage", "Ascend end.", this);
    }

    IEnumerator StopAndSpawnWave(ElevatorWave wave)
    {
        SpecialStageDebugHUD.Step($"Stop and spawn wave: {(wave != null ? wave.waveName : "NULL")}", this);

        if (exteriorScroller != null)
        {
            exteriorScroller.Stop();
            SpecialStageDebugHUD.Log("Stage", "Exterior scroller stopped.", this);
        }

        if (door != null)
        {
            SpecialStageDebugHUD.Log("Stage", "Door opening.", this);
            yield return door.Open();
        }
        else
        {
            SpecialStageDebugHUD.Warn("Stage", "Door is not assigned.", this);
        }

        yield return new WaitForSeconds(0.25f);

        if (waveSpawner != null)
        {
            SpecialStageDebugHUD.Log("Stage", "Wave spawning and enemy entry started.", this);
            yield return waveSpawner.SpawnWaveAndWaitEntry(wave);
            SpecialStageDebugHUD.Log("Stage", "Wave enemy entry finished.", this);
        }
        else
        {
            SpecialStageDebugHUD.Warn("Stage", "WaveSpawner is not assigned.", this);
        }

        yield return new WaitForSeconds(0.2f);

        if (door != null)
        {
            SpecialStageDebugHUD.Log("Stage", "Door closing after enemy entry.", this);
            yield return door.Close();
        }

        SpecialStageDebugHUD.Step($"Combat wave active: {(wave != null ? wave.waveName : "NULL")}", this);

        while (waveSpawner != null && !waveSpawner.IsWaveCleared())
            yield return null;

        SpecialStageDebugHUD.Log("Stage", $"Wave cleared: {(wave != null ? wave.waveName : "NULL")}", this);

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator MalfunctionSegment()
    {
        if (exteriorScroller != null)
            exteriorScroller.Stop();

        if (motionFX != null)
            yield return motionFX.Shake(malfunctionSeconds);
        else
            yield return new WaitForSeconds(malfunctionSeconds);
    }

    IEnumerator ClearStage()
    {
        if (player != null)
            player.SetInputLocked(true);

        if (exteriorScroller != null)
            exteriorScroller.Stop();

        if (windowMotion != null)
            windowMotion.EndWindowMotion();

        yield return new WaitForSeconds(0.5f);

        if (door != null)
            yield return door.Open();

        if (followCamera != null)
            followCamera.SetCinematicMode(false);

        if (SceneTransitionDirector.Instance != null)
        {
            SceneTransitionDirector.Instance.StartStageTransition(
                nextSceneName,
                nextEntryId,
                exitDirection
            );
        }

        playing = false;
    }
}