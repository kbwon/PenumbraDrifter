using System.Collections;
using UnityEngine;

public class BossStageIntroDirector : MonoBehaviour
{
    [Header("Refs")]
    public DialogueManager dialogue;
    public PlayerController player;
    public ShadowInteractController shadow;
    public FollowCamera followCamera;
    public BossController boss;

    [Header("Camera")]
    public Transform bossFocusPoint;
    public float bossFocusYaw = 45f;
    public float bossFocusOrthoSize = 7f;
    public float focusMoveSeconds = 1.0f;
    public float returnMoveSeconds = 1.0f;

    [Header("Dialogue")]
    public bool playOnStart = true;
    public DialogueLine[] introLines;

    [Header("Entry Walk Sync")]
    public bool waitForEntryWalkBeforeIntro = true;
    public float maxEntryWalkWaitSeconds = 5f;

    [Header("Start Control")]
    public float startDelay = 0.1f;

    bool playing;
    bool introLockActive;

    [Header("Skip")]
    public bool allowSkip = true;
    public KeyCode skipKey = KeyCode.Escape;
    public bool skipOnlyAfterEntryWalk = true;

    [Header("Audio")]
    public BossAudio bossAudio;

    bool skipRequested;

    void Awake()
    {
        ResolveRefs();

        // 보스가 Start에서 startActive 값을 다시 읽기 전이라도 안전하게 꺼둡니다.
        if (boss != null)
        {
            boss.startActive = false;
            boss.SetCombatActive(false);
        }
    }

    IEnumerator Start()
    {
        yield return null;

        ResolveRefs();

        if (boss != null)
        {
            boss.startActive = false;
            boss.SetCombatActive(false);
        }

        if (playOnStart && !playing)
            yield return IntroRoutine();
    }

    void Update()
    {
        if (!introLockActive)
            return;

        if (allowSkip && Input.GetKeyDown(skipKey))
            RequestSkipIntro();

        MaintainIntroLock();
    }

    void ResolveRefs()
    {
        if (dialogue == null)
            dialogue = DialogueManager.Instance != null
                ? DialogueManager.Instance
                : FindFirstObjectByType<DialogueManager>();

        if (player == null)
        {
            if (GameManager.Instance != null)
                player = GameManager.Instance.player;

            if (player == null)
                player = FindFirstObjectByType<PlayerController>();
        }

        if (shadow == null)
        {
            if (GameManager.Instance != null)
                shadow = GameManager.Instance.shadow;

            if (shadow == null)
                shadow = FindFirstObjectByType<ShadowInteractController>();
        }

        if (followCamera == null)
        {
            if (GameManager.Instance != null)
                followCamera = GameManager.Instance.followCamera;

            if (followCamera == null)
                followCamera = FindFirstObjectByType<FollowCamera>();
        }

        if (boss == null)
            boss = FindFirstObjectByType<BossController>();

        if (bossAudio == null && boss != null)
            bossAudio = boss.GetComponent<BossAudio>();

        if (bossAudio == null)
            bossAudio = FindFirstObjectByType<BossAudio>();
    }

    IEnumerator IntroRoutine()
    {
        playing = true;
        introLockActive = true;

        ResolveRefs();
        MaintainIntroLock();

        if (startDelay > 0f)
            yield return WaitSkippableSeconds(startDelay);

        if (waitForEntryWalkBeforeIntro)
            yield return WaitForEntryWalkFinished();

        MaintainIntroLock();

        if (!ShouldSkip() && followCamera != null && bossFocusPoint != null)
        {
            followCamera.SetCinematicMode(true);
            followCamera.SetCinematicInstantPosition(false);

            yield return MoveCameraFocus(
                bossFocusPoint.position,
                bossFocusYaw,
                bossFocusOrthoSize,
                focusMoveSeconds
            );

            if (bossAudio != null)
                bossAudio.PlayEntrance();
        }

        MaintainIntroLock();

        if (!ShouldSkip() && dialogue != null && introLines != null && introLines.Length > 0)
        {
            yield return dialogue.Show(
                introLines,
                lockPlayer: false,
                forceExitShadow: false,
                pauseGame: false
            );
        }

        if (!ShouldSkip() && followCamera != null)
        {
            Vector3 playerFocus = followCamera.GetGameplayFocusPoint();

            yield return MoveCameraFocus(
                playerFocus,
                bossFocusYaw,
                bossFocusOrthoSize,
                returnMoveSeconds
            );
        }

        FinishIntroImmediate();

        introLockActive = false;

        if (boss != null)
            boss.SetCombatActive(true);

        if (player != null)
            player.SetInputLocked(false);

        if (shadow != null)
            shadow.SetShadowToggleLocked(false);

        playing = false;
    }

    void MaintainIntroLock()
    {
        ResolveRefs();

        if (player != null)
            player.SetInputLocked(true);

        if (shadow != null)
        {
            shadow.ForceExitShadowMode();
            shadow.ClearSurfaceAnchor();
            shadow.ClearMovingShadowHost();
            shadow.SetShadowToggleLocked(true);
        }

        if (boss != null)
        {
            boss.startActive = false;
            boss.SetCombatActive(false);
        }
    }

    IEnumerator WaitForEntryWalkFinished()
    {
        float t = 0f;

        while (player == null && t < maxEntryWalkWaitSeconds)
        {
            ResolveRefs();
            t += Time.deltaTime;
            yield return null;
        }

        if (player == null)
            yield break;

        while (player.IsScriptedMoveActive && t < maxEntryWalkWaitSeconds)
        {
            MaintainIntroLock();
            t += Time.deltaTime;
            yield return null;
        }

        yield return null;
    }

    IEnumerator MoveCameraFocus(Vector3 targetFocus, float targetYaw, float targetOrthoSize, float seconds)
    {
        if (followCamera == null)
            yield break;

        float duration = Mathf.Max(0.01f, seconds);
        float startYaw = followCamera.CurrentYaw;
        Vector3 startFocus = followCamera.GetGameplayFocusPoint();

        Camera cam = followCamera.CachedCamera;
        float startOrtho = cam != null ? cam.orthographicSize : targetOrthoSize;

        float t = 0f;

        while (t < duration)
        {
            if (ShouldSkip())
                yield break;

            MaintainIntroLock();

            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float k = u * u * (3f - 2f * u);

            Vector3 focus = Vector3.Lerp(startFocus, targetFocus, k);
            float yaw = Mathf.LerpAngle(startYaw, targetYaw, k);
            float ortho = Mathf.Lerp(startOrtho, targetOrthoSize, k);

            followCamera.SetFocusPoint(focus);
            followCamera.SetYawImmediate(yaw);
            followCamera.SetOrthoSizeImmediate(ortho);

            yield return null;
        }

        followCamera.SetFocusPoint(targetFocus);
        followCamera.SetYawImmediate(targetYaw);
        followCamera.SetOrthoSizeImmediate(targetOrthoSize);
    }

    public void RequestSkipIntro()
    {
        if (!playing)
            return;

        skipRequested = true;

        if (skipOnlyAfterEntryWalk && player != null && player.IsScriptedMoveActive)
            return;

        if (dialogue != null)
            dialogue.RequestSkipAll();
    }

    bool ShouldSkip()
    {
        return allowSkip && skipRequested;
    }

    IEnumerator WaitSkippableSeconds(float seconds)
    {
        float t = 0f;

        while (t < seconds)
        {
            if (ShouldSkip())
                yield break;

            MaintainIntroLock();

            t += Time.deltaTime;
            yield return null;
        }
    }

    void FinishIntroImmediate()
    {
        if (dialogue != null)
            dialogue.RequestSkipAll();

        if (followCamera != null)
        {
            followCamera.ClearFocusOverride();
            followCamera.SetCinematicMode(false);
            followCamera.SnapNow();
        }

        introLockActive = false;

        if (shadow != null)
            shadow.SetShadowToggleLocked(false);

        if (boss != null)
            boss.SetCombatActive(true);

        if (player != null)
            player.SetInputLocked(false);

        skipRequested = false;
        playing = false;
    }
}