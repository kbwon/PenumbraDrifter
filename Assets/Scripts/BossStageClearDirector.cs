using UnityEngine;
using UnityEngine.UI;

public class BossStageClearDirector : MonoBehaviour
{
    [Header("Refs")]
    public BossController boss;
    public PlayerController player;
    public ShadowInteractController shadow;
    public DemoSceneLoader sceneLoader;

    [Header("UI")]
    public GameObject clearPanel;

    [Header("Buttons")]
    public Button restartButton;
    public Button returnToTitleButton;
    public Button quitButton;

    [Header("Clear Behavior")]
    public bool pauseOnClear = true;
    public bool lockPlayerOnClear = true;
    public bool forceExitShadowOnClear = true;
    public bool saveBossStageOnClear = true;

    [Header("Debug")]
    public bool debugLog = true;

    bool cleared;

    void Awake()
    {
        ResolveRefs();

        if (clearPanel != null)
            clearPanel.SetActive(false);

        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = volume;

    }

    void OnEnable()
    {
        ResolveRefs();

        if (boss != null)
            boss.OnDeathFinished += HandleBossDeathFinished;
    }

    void OnDisable()
    {
        if (boss != null)
            boss.OnDeathFinished -= HandleBossDeathFinished;
    }

    void Start()
    {
        ResolveRefs();

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartStage);

        if (returnToTitleButton != null)
            returnToTitleButton.onClick.AddListener(ReturnToTitle);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    void ResolveRefs()
    {
        if (boss == null)
            boss = FindFirstObjectByType<BossController>();

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

        if (sceneLoader == null)
            sceneLoader = FindFirstObjectByType<DemoSceneLoader>();
    }

    void HandleBossDeathFinished()
    {
        ShowClearPanel();
    }

    public void ShowClearPanel()
    {
        if (cleared)
            return;

        cleared = true;

        if (debugLog)
            Debug.Log("[BossStageClearDirector] Boss stage cleared.", this);

        if (saveBossStageOnClear)
            DemoProgress.SaveContinuePoint(DemoContinuePoint.BossStage);

        if (forceExitShadowOnClear && shadow != null)
        {
            shadow.ForceExitShadowMode();
            shadow.ClearSurfaceAnchor();
            shadow.ClearMovingShadowHost();
            shadow.SetShadowToggleLocked(true);
        }

        if (lockPlayerOnClear && player != null)
            player.SetInputLocked(true);

        if (clearPanel != null)
            clearPanel.SetActive(true);

        if (pauseOnClear)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetPaused(true);
            else
                Time.timeScale = 0f;
        }
    }

    public void RestartStage()
    {
        ResumeBeforeSceneAction();

        if (sceneLoader != null)
            sceneLoader.RestartCurrentStage();
    }

    public void ReturnToTitle()
    {
        ResumeBeforeSceneAction();

        if (sceneLoader != null)
            sceneLoader.ReturnToTitle();
    }

    public void QuitGame()
    {
        ResumeBeforeSceneAction();

        if (sceneLoader != null)
            sceneLoader.QuitGame();
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    void ResumeBeforeSceneAction()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);

        Time.timeScale = 1f;

        if (clearPanel != null)
            clearPanel.SetActive(false);
    }
}