using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class StageBriefingFocus
{
    public string name;
    public Transform focusPoint;
    public float orthoSize = 10f;
    public float yaw = 45f;
    public float moveSeconds = 1.2f;
    public float holdSeconds = 0.25f;
    public DialogueLine[] lines;
}

public class Stage1BriefingDirector : MonoBehaviour
{
    [Header("Refs")]
    public DialogueManager dialogue;
    public PlayerController player;
    public ShadowInteractController shadow;
    public FollowCamera followCamera;

    [Header("Player Visual Lock")]
    public FaceCameraY playerVisualFaceCameraY;
    public bool lockPlayerBillboardDuringBriefing = true;
    public bool lockPlayerFaceCameraYDuringBriefing = true;

    [Header("Start")]
    public bool playOnStart = true;
    public float startDelay = 0.2f;

    [Header("Opening Dialogue")]
    public DialogueLine[] openingLines;

    [Header("Area Briefing")]
    public StageBriefingFocus[] focusStops;

    [Header("Return To Player")]
    public float returnMoveSeconds = 1.2f;
    public float gameplayOrthoSize = 7f;
    public float finalGameplayYaw = 45f;

    [Header("Final Camera Rotation")]
    public bool rotateOnceBeforeStart = true;
    public float rotateSeconds = 2.0f;
    public float rotateDegrees = 360f;

    [Header("Entry Walk Sync")]
    public bool waitForEntryWalkBeforeBriefing = true;
    public float maxEntryWalkWaitSeconds = 5f;

    [Header("Debug")]
    public bool debugLog = true;

    bool playing;
    bool briefingInputLockActive;

    IEnumerator Start()
    {
        yield return null;

        if (!playOnStart)
            yield break;

        ResolveRefs();

        if (!playing)
            StartCoroutine(BriefingRoutine());
    }

    void Update()
    {
        if (!briefingInputLockActive)
            return;

        MaintainBriefingInputLock();
    }

    void ResolveRefs()
    {
        if (dialogue == null)
            dialogue = DialogueManager.Instance != null ? DialogueManager.Instance : FindFirstObjectByType<DialogueManager>();

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

        if (playerVisualFaceCameraY == null && player != null)
            playerVisualFaceCameraY = player.GetComponentInChildren<FaceCameraY>(true);
    }

    [ContextMenu("TEST/Play Stage 1 Briefing")]
    public void PlayBriefing()
    {
        if (!playing)
            StartCoroutine(BriefingRoutine());
    }

    IEnumerator BriefingRoutine()
    {
        playing = true;
        briefingInputLockActive = true;

        ResolveRefs();

        // 브리핑 시작 즉시 입력 잠금
        SetBriefingControlLocked(true);
        MaintainBriefingInputLock();

        if (shadow != null)
        {
            shadow.ForceExitShadowMode();
            shadow.ClearSurfaceAnchor();
            shadow.ClearMovingShadowHost();
        }

        if (followCamera != null)
        {
            followCamera.SetCinematicMode(true);
            followCamera.SetCinematicInstantPosition(false);
            followCamera.SetGameplayYaw(finalGameplayYaw);
            followCamera.SetYawImmediate(finalGameplayYaw);
            followCamera.SetOrthoSizeImmediate(gameplayOrthoSize);
            followCamera.SnapNow();
        }

        // 플레이어가 걸어 나오는 장면을 아주 잠깐 보여준 뒤 첫 대사 출력
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        MaintainBriefingInputLock();

        if (dialogue != null && openingLines != null && openingLines.Length > 0)
        {
            yield return dialogue.Show(
                openingLines,
                lockPlayer: false,
                forceExitShadow: false,
                pauseGame: false
            );
        }

        if (focusStops != null)
        {
            for (int i = 0; i < focusStops.Length; i++)
            {
                StageBriefingFocus stop = focusStops[i];
                if (stop == null || stop.focusPoint == null)
                    continue;

                Log($"Focus: {stop.name}");

                yield return MoveCameraFocus(
                    stop.focusPoint.position,
                    stop.yaw,
                    stop.orthoSize,
                    stop.moveSeconds
                );

                if (stop.holdSeconds > 0f)
                    yield return new WaitForSeconds(stop.holdSeconds);

                if (dialogue != null && stop.lines != null && stop.lines.Length > 0)
                {
                    yield return dialogue.Show(
                        stop.lines,
                        lockPlayer: false,
                        forceExitShadow: false,
                        pauseGame: false
                    );
                }
            }
        }

        if (followCamera != null)
        {
            Vector3 playerFocus = followCamera.GetGameplayFocusPoint();

            yield return MoveCameraFocus(
                playerFocus,
                finalGameplayYaw,
                gameplayOrthoSize,
                returnMoveSeconds
            );

            if (rotateOnceBeforeStart)
                yield return RotateCameraOnce();

            followCamera.SetCinematicInstantPosition(true);
            followCamera.ClearFocusOverride();
            followCamera.SetGameplayYaw(finalGameplayYaw);
            followCamera.SetYawImmediate(finalGameplayYaw);
            followCamera.SetOrthoSizeImmediate(gameplayOrthoSize);
            followCamera.SetCinematicMode(false);
            followCamera.SnapNow();
        }

        briefingInputLockActive = false;
        SetBriefingControlLocked(false);

        playing = false;
    }

    IEnumerator MoveCameraFocus(Vector3 targetFocus, float targetYaw, float targetOrthoSize, float seconds)
    {
        if (followCamera == null)
            yield break;

        float duration = Mathf.Max(0.01f, seconds);
        float startYaw = followCamera.CurrentYaw;
        Vector3 startFocus = followCamera.GetGameplayFocusPoint();

        // 이미 이전 포커스 오버라이드가 있다면 현재 카메라가 바라보던 지점 근처에서 시작하도록 합니다.
        if (duration > 0.02f)
            startFocus = EstimateCurrentFocus(startFocus);

        Camera cam = followCamera.CachedCamera;
        float startOrtho = cam != null ? cam.orthographicSize : targetOrthoSize;

        float t = 0f;

        while (t < duration)
        {
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

        // 씬 전환 입장 연출이 진행 중이면 끝날 때까지 기다림
        while (player.IsScriptedMoveActive && t < maxEntryWalkWaitSeconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        // EndScriptedMove(false)가 실행된 직후 한 프레임 더 기다린 뒤
        // Stage1BriefingDirector가 입력 잠금을 다시 잡도록 함
        yield return null;
    }
    Vector3 EstimateCurrentFocus(Vector3 fallback)
    {
        Camera cam = followCamera != null ? followCamera.CachedCamera : null;
        if (cam == null)
            return fallback;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Plane plane = new Plane(Vector3.up, fallback);

        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return fallback;
    }

    IEnumerator RotateCameraOnce()
    {
        if (followCamera == null)
            yield break;

        float startYaw = followCamera.CurrentYaw;
        float targetYaw = startYaw + rotateDegrees;
        float duration = Mathf.Max(0.01f, rotateSeconds);
        Vector3 focus = followCamera.GetGameplayFocusPoint();

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float k = u * u * (3f - 2f * u);

            followCamera.SetFocusPoint(focus);
            followCamera.SetYawImmediate(Mathf.Lerp(startYaw, targetYaw, k));
            followCamera.SetOrthoSizeImmediate(gameplayOrthoSize);

            yield return null;
        }
    }

    void Log(string message)
    {
        if (debugLog)
            Debug.Log($"[Stage1Briefing] {message}", this);
    }

    void Reset()
    {
        openingLines = new[]
        {
            new DialogueLine("Client", "도시 중앙의 고층 건물. 오늘 목표는 그 안에 있다."),
            new DialogueLine("Drifter", "목표가 뭔지는 아직도 말 안 해줄 건가?"),
            new DialogueLine("Client", "가 보면 알게 돼. 네 능력이라면 어렵지 않을 거다."),
            new DialogueLine("Drifter", "항상 그런 말이 제일 수상한데."),
            new DialogueLine("Client", "보수는 충분히 준비했다."),
            new DialogueLine("Drifter", "…그럼 올라가 보지.")
        };

        focusStops = new[]
        {
            new StageBriefingFocus
            {
                name = "Entrance",
                yaw = 45f,
                orthoSize = 9f,
                lines = new[] { new DialogueLine("Client", "입구는 조용하지만, 시야를 피할 공간이 많지 않다.") }
            },
            new StageBriefingFocus
            {
                name = "Central Plaza",
                yaw = 45f,
                orthoSize = 11f,
                lines = new[] { new DialogueLine("Client", "중앙 구역. 여러 길이 여기서 갈라진다.") }
            },
            new StageBriefingFocus
            {
                name = "Key Item Area",
                yaw = 45f,
                orthoSize = 10f,
                lines = new[] { new DialogueLine("Client", "구석을 잘 살펴봐. 위로 올라갈 수 있는 길이 있을지도 모른다.") }
            },
            new StageBriefingFocus
            {
                name = "Collectible Area",
                yaw = 45f,
                orthoSize = 10f,
                lines = new[] { new DialogueLine("Client", "저쪽은 필수 경로는 아니다. 하지만 뭔가 숨겨져 있을 가능성은 있다.") }
            },
            new StageBriefingFocus
            {
                name = "Goal Area",
                yaw = 45f,
                orthoSize = 10f,
                lines = new[] { new DialogueLine("Client", "최종 진입구다. 그냥 열리지는 않을 거다.") }
            }
        };
    }

    void LockPlayerVisualForBriefing()
    {
        if (player != null && lockPlayerBillboardDuringBriefing)
            player.SetBillboardLocked(true);

        if (playerVisualFaceCameraY != null && lockPlayerFaceCameraYDuringBriefing)
            playerVisualFaceCameraY.SetFacingLocked(true);
    }

    void UnlockPlayerVisualForBriefing()
    {
        if (player != null && lockPlayerBillboardDuringBriefing)
            player.SetBillboardLocked(false);

        if (playerVisualFaceCameraY != null && lockPlayerFaceCameraYDuringBriefing)
            playerVisualFaceCameraY.SetFacingLocked(false);
    }

    void SetBriefingControlLocked(bool locked)
    {
        if (player != null)
        {
            player.SetInputLocked(locked);

            if (lockPlayerBillboardDuringBriefing)
                player.SetBillboardLocked(locked);
        }

        if (playerVisualFaceCameraY != null && lockPlayerFaceCameraYDuringBriefing)
            playerVisualFaceCameraY.SetFacingLocked(locked);

        if (locked && shadow != null)
        {
            shadow.ForceExitShadowMode();
            shadow.ClearSurfaceAnchor();
            shadow.ClearMovingShadowHost();
        }
    }

    void MaintainBriefingInputLock()
    {
        ResolveRefs();

        if (player != null)
        {
            if (!player.InputLocked)
                player.SetInputLocked(true);

            if (lockPlayerBillboardDuringBriefing)
                player.SetBillboardLocked(true);
        }

        if (playerVisualFaceCameraY != null && lockPlayerFaceCameraYDuringBriefing)
            playerVisualFaceCameraY.SetFacingLocked(true);

        if (shadow != null && shadow.IsInShadowMode)
        {
            shadow.ForceExitShadowMode();
            shadow.ClearSurfaceAnchor();
            shadow.ClearMovingShadowHost();
        }
    }
}
