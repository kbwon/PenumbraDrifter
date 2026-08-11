using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleMenuController : MonoBehaviour
{
    public Button startButton;
    public Button continueButton;
    public Button settingsButton;
    public Button quitButton;

    [Header("Continue Button Visual")]
    public TMP_Text continueButtonText;

    [Range(0f, 1f)]
    public float continueDisabledTextAlpha = 0.4f;

    public GameObject settingsPanel;
    public Slider volumeSlider;

    public DemoSceneLoader sceneLoader;

    void Start()
    {
        if (sceneLoader == null)
            sceneLoader = FindFirstObjectByType<DemoSceneLoader>();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        RefreshContinueButton();

        float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = volume;

        if (volumeSlider != null)
        {
            volumeSlider.value = volume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        if (startButton != null)
            startButton.onClick.AddListener(sceneLoader.StartNewGame);

        if (continueButton != null)
            continueButton.onClick.AddListener(sceneLoader.ContinueGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(ToggleSettings);

        if (quitButton != null)
            quitButton.onClick.AddListener(sceneLoader.QuitGame);
    }

    void RefreshContinueButton()
    {
        bool hasContinue = DemoProgress.HasContinue;

        if (continueButton != null)
            continueButton.interactable = hasContinue;

        if (continueButtonText != null)
        {
            Color color = continueButtonText.color;
            color.a = hasContinue ? 1f : continueDisabledTextAlpha;
            continueButtonText.color = color;
        }
    }

    public void ToggleSettings()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
}