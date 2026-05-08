using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionDirector : MonoBehaviour
{
    StageIntroDirector FindStageIntroDirector(StageEntryPoint entry)
    {
        if (entry != null && entry.stageIntroDirector != null)
            return entry.stageIntroDirector;

        return FindFirstObjectByType<StageIntroDirector>();
    }
    public static SceneTransitionDirector Instance { get; private set; }

    [Header("Fade")]
    public Image fadeImage;
    public float fadeOutTime = 0.8f;
    public float fadeInTime = 0.8f;

    [Header("Auto Walk")]
    public float exitWalkSeconds = 1.0f;
    public float enterWalkSpeed = 4.5f;
    public float exitWalkSpeed = 4.5f;

    bool isTransitioning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetFadeAlpha(0f);
    }

    public void StartStageTransition(
        string nextSceneName,
        string entryId,
        Vector3 exitDirection
    )
    {
        if (isTransitioning) return;
        StartCoroutine(StageTransitionRoutine(nextSceneName, entryId, exitDirection));
    }

    IEnumerator StageTransitionRoutine(
        string nextSceneName,
        string entryId,
        Vector3 exitDirection
    )
    {
        isTransitioning = true;

        PlayerController player = FindPlayer();

        if (player != null)
        {
            player.BeginScriptedMove(exitDirection, exitWalkSpeed);
            yield return new WaitForSeconds(exitWalkSeconds);
            player.EndScriptedMove(true);
        }

        yield return FadeTo(1f, fadeOutTime);

        AsyncOperation load = SceneManager.LoadSceneAsync(nextSceneName);
        while (!load.isDone)
            yield return null;

        yield return null;

        if (GameManager.Instance != null)
            GameManager.Instance.CacheSceneRefs();

        player = FindPlayer();

        StageEntryPoint entry = FindEntryPoint(entryId);

        if (player != null && entry != null)
        {
            player.transform.position = entry.SpawnPosition;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = entry.SpawnPosition;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (GameManager.Instance != null && GameManager.Instance.followCamera != null)
            {
                FollowCamera cam = GameManager.Instance.followCamera;

                cam.target = player.transform;
                cam.ClearFocusOverride();

                if (entry != null && entry.setCameraYawOnEnter)
                {
                    cam.SetGameplayYaw(entry.cameraYawOnEnter);
                    cam.SetYawImmediate(entry.cameraYawOnEnter);
                }

                cam.SnapNow();
            }
        }

        // 검은 화면 상태에서 한 프레임 대기해서 카메라와 위치를 안정화한다.
        yield return null;

        // 먼저 화면을 밝게 만든다.
        yield return FadeTo(0f, fadeInTime);

        // Fade In이 끝난 뒤, 플레이어가 보이는 상태에서 자동으로 걸어 들어오게 한다.
        StageIntroDirector intro = FindStageIntroDirector(entry);

        bool willPlayStageIntro =
            entry != null &&
            entry.playStageIntroAfterEnter &&
            intro != null;

        if (player != null && entry != null)
        {
            Vector3 enterDir = entry.GetWalkDirection();
            float enterDistance = entry.GetWalkDistance();

            float enterSeconds = enterDistance / Mathf.Max(0.01f, enterWalkSpeed);
            enterSeconds = Mathf.Clamp(enterSeconds, 0.6f, 1.8f);

            player.BeginScriptedMove(enterDir, enterWalkSpeed);

            yield return new WaitForSeconds(enterSeconds);

            // 바로 스테이지 인트로가 이어질 예정이면 입력 잠금 유지
            player.EndScriptedMove(willPlayStageIntro);
        }
        else if (player != null)
        {
            player.EndScriptedMove(willPlayStageIntro);
        }

        // 입장 걷기 후 스테이지 인트로 실행
        if (willPlayStageIntro)
        {
            yield return intro.PlayIntroAndWait();
        }
        else if (player != null)
        {
            player.SetInputLocked(false);
        }

        isTransitioning = false;
    }

    IEnumerator AutoWalkThenStop(
        PlayerController player,
        Vector3 direction,
        float speed,
        float seconds
    )
    {
        player.BeginScriptedMove(direction, speed);

        float t = 0f;

        while (t < seconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        player.EndScriptedMove(true);
    }

    PlayerController FindPlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.player != null)
            return GameManager.Instance.player;

        return FindFirstObjectByType<PlayerController>();
    }

    StageEntryPoint FindEntryPoint(string entryId)
    {
        StageEntryPoint[] entries = FindObjectsByType<StageEntryPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] != null && entries[i].entryId == entryId)
                return entries[i];
        }

        return entries.Length > 0 ? entries[0] : null;
    }

    IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null)
            yield break;

        float startAlpha = fadeImage.color.a;
        float t = 0f;

        fadeImage.raycastTarget = true;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));

            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, k));
            yield return null;
        }

        SetFadeAlpha(targetAlpha);

        if (Mathf.Approximately(targetAlpha, 0f))
            fadeImage.raycastTarget = false;
    }

    void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;

        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }
}