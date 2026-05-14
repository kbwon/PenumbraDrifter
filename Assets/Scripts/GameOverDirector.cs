using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverDirector : MonoBehaviour
{
    [Header("Refs")]
    public PlayerHealth playerHealth;
    public Image fadeImage;
    public GameObject gameOverPanel;

    [Header("Timing")]
    public float freezeDelay = 0.08f;
    public float fadeSeconds = 1.2f;

    [Header("Fade")]
    [Range(0f, 1f)] public float targetFadeAlpha = 0.9f;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    bool running;

    void Awake()
    {
        if (playerHealth == null && GameManager.Instance != null)
            playerHealth = GameManager.Instance.health;

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (fadeImage != null)
        {
            SetFadeAlpha(0f);
            fadeImage.raycastTarget = false;
        }

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDead += HandlePlayerDead;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDead -= HandlePlayerDead;
    }

    void HandlePlayerDead()
    {
        if (running) return;
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        running = true;

        // hurt 애니메이션이 아주 짧게 보이도록 실제 시간 기준으로 기다린다.
        yield return new WaitForSecondsRealtime(freezeDelay);

        // 현재 피격 애니메이션 프레임에서 멈춘다.
        if (playerHealth != null && playerHealth.anim != null)
            playerHealth.anim.speed = 0f;

        // 게임 전체 정지. 적, 투사체, 물리 이동이 멈춘다.
        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(true);
        else
            Time.timeScale = 0f;

        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        float t = 0f;

        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeSeconds));

            SetFadeAlpha(Mathf.Lerp(0f, targetFadeAlpha, k));

            yield return null;
        }

        SetFadeAlpha(targetFadeAlpha);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;

        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
    }

    public void RestartCurrentScene()
    {
        ResumeTimeBeforeSceneChange();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        ResumeTimeBeforeSceneChange();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    void ResumeTimeBeforeSceneChange()
    {
        if (playerHealth != null && playerHealth.anim != null)
            playerHealth.anim.speed = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);
        else
            Time.timeScale = 1f;
    }
}