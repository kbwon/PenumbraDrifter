using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class PauseStageShortcut
{
    public Button button;
    public string sceneName;
}

public class PauseMenuController : MonoBehaviour
{
    [Header("Root")]
    public GameObject pausePanel;
    public GameObject settingsPanel;

    [Header("Main Buttons")]
    public Button resumeButton;
    public Button settingsButton;
    public Button restartButton;
    public Button returnToTitleButton;
    public Button quitButton;

    [Header("Settings")]
    public Slider volumeSlider;
    public Button closeSettingsButton;

    [Header("Stage Shortcut Buttons")]
    public PauseStageShortcut[] stageShortcuts;

    [Header("Scene Loader")]
    public DemoSceneLoader sceneLoader;

    [Header("Input")]
    public KeyCode pauseKey = KeyCode.Escape;
    public string titleSceneName = "TitleScene";

    [Header("Audio Pause")]
    public bool pauseAudioOnPauseMenu = true;
    bool previousAudioListenerPause;

    bool isOpen;

    void Start()
    {
        ResolveRefs();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = volume;

        if (volumeSlider != null)
        {
            volumeSlider.value = volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartStage);

        if (returnToTitleButton != null)
            returnToTitleButton.onClick.AddListener(ReturnToTitle);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        BindStageShortcutButtons();
    }

    void Update()
    {
        if (!Input.GetKeyDown(pauseKey))
            return;

        if (SceneManager.GetActiveScene().name == titleSceneName)
            return;

        if (isOpen)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
                CloseSettings();
            else
                ResumeGame();

            return;
        }

        if (!CanOpenPause())
            return;

        OpenPause();
    }

    void ResolveRefs()
    {
        if (sceneLoader == null)
            sceneLoader = FindFirstObjectByType<DemoSceneLoader>();
    }

    void BindStageShortcutButtons()
    {
        if (stageShortcuts == null)
            return;

        for (int i = 0; i < stageShortcuts.Length; i++)
        {
            PauseStageShortcut shortcut = stageShortcuts[i];

            if (shortcut == null)
                continue;

            if (shortcut.button == null)
                continue;

            string targetScene = shortcut.sceneName;

            shortcut.button.onClick.AddListener(() =>
            {
                LoadStage(targetScene);
            });
        }
    }

    bool CanOpenPause()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return false;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
            return false;

        PlayerController player = null;

        if (GameManager.Instance != null)
            player = GameManager.Instance.player;

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            if (player.InputLocked)
                return false;

            if (player.IsScriptedMoveActive)
                return false;

            if (player.IsShadowTransitionPlaying)
                return false;
        }

        return true;
    }

    public void OpenPause()
    {
        ResolveRefs();

        isOpen = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        PlayerController player = GameManager.Instance != null
            ? GameManager.Instance.player
            : FindFirstObjectByType<PlayerController>();

        ShadowInteractController shadow = GameManager.Instance != null
            ? GameManager.Instance.shadow
            : FindFirstObjectByType<ShadowInteractController>();

        if (player != null)
            player.SetInputLocked(true);

        if (shadow != null)
            shadow.SetShadowToggleLocked(true);

        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(true);
        else
            Time.timeScale = 0f;

        if (pauseAudioOnPauseMenu)
        {
            previousAudioListenerPause = AudioListener.pause;
            AudioListener.pause = true;
        }
    }

    public void ResumeGame()
    {
        isOpen = false;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);
        else
            Time.timeScale = 1f;

        if (pauseAudioOnPauseMenu)
            AudioListener.pause = previousAudioListenerPause;

        PlayerController player = GameManager.Instance != null
            ? GameManager.Instance.player
            : FindFirstObjectByType<PlayerController>();

        ShadowInteractController shadow = GameManager.Instance != null
            ? GameManager.Instance.shadow
            : FindFirstObjectByType<ShadowInteractController>();

        if (player != null)
            player.SetInputLocked(false);

        if (shadow != null)
            shadow.SetShadowToggleLocked(false, 0.15f);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
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

    public void LoadStage(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        ResumeBeforeSceneAction();

        if (sceneLoader != null)
            sceneLoader.LoadStage(sceneName);
    }

    public void QuitGame()
    {
        ResumeBeforeSceneAction();

        if (sceneLoader != null)
            sceneLoader.QuitGame();
    }

    void ResumeBeforeSceneAction()
    {
        isOpen = false;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);

        Time.timeScale = 1f;

        if (pauseAudioOnPauseMenu)
            AudioListener.pause = previousAudioListenerPause;
    }
}