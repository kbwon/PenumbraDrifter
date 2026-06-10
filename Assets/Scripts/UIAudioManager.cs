using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource source;

    [Header("Clips")]
    public AudioClip buttonClickClip;
    public AudioClip dialogueTypingClip;
    public AudioClip gameOverClip;
    public AudioClip stageClearClip;

    [Header("Volumes")]
    [Range(0f, 1f)] public float buttonClickVolume = 0.5f;
    [Range(0f, 1f)] public float dialogueTypingVolume = 0.25f;
    [Range(0f, 1f)] public float gameOverVolume = 0.75f;
    [Range(0f, 1f)] public float stageClearVolume = 0.75f;

    [Header("Typing")]
    public float typingMinInterval = 0.035f;
    public bool randomizeTypingPitch = true;
    public float typingMinPitch = 0.96f;
    public float typingMaxPitch = 1.04f;

    [Header("Register Buttons")]
    public bool autoRegisterSceneButtons = true;
    public bool includeInactiveButtons = true;

    float lastTypingTime = -999f;
    float lastStageClearTime = -999f;
    float stageClearMinInterval = 1.0f;

    GameManager subscribedGameManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        SubscribeGameManager();

        if (autoRegisterSceneButtons)
            StartCoroutine(RegisterButtonsNextFrame());
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeGameManager();
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SubscribeGameManager();

        if (autoRegisterSceneButtons)
            StartCoroutine(RegisterButtonsNextFrame());
    }

    IEnumerator RegisterButtonsNextFrame()
    {
        yield return null;
        RegisterSceneButtons();
    }

    void SubscribeGameManager()
    {
        if (subscribedGameManager == GameManager.Instance)
            return;

        UnsubscribeGameManager();

        subscribedGameManager = GameManager.Instance;

        if (subscribedGameManager != null)
            subscribedGameManager.OnGameOverChanged += HandleGameOverChanged;
    }

    void UnsubscribeGameManager()
    {
        if (subscribedGameManager != null)
            subscribedGameManager.OnGameOverChanged -= HandleGameOverChanged;

        subscribedGameManager = null;
    }

    void HandleGameOverChanged(bool isGameOver)
    {
        if (isGameOver)
            PlayGameOver();
    }

    public void RegisterSceneButtons()
    {
        FindObjectsInactive inactiveOption =
            includeInactiveButtons ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;

        Button[] buttons = FindObjectsByType<Button>(
            inactiveOption,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            UIAudioButtonClick clickAudio = button.GetComponent<UIAudioButtonClick>();

            if (clickAudio == null)
                clickAudio = button.gameObject.AddComponent<UIAudioButtonClick>();

            clickAudio.manager = this;
        }
    }

    public void PlayButtonClick()
    {
        PlayOneShot(buttonClickClip, buttonClickVolume, 1f);
    }

    public void PlayDialogueTyping()
    {
        if (Time.unscaledTime - lastTypingTime < typingMinInterval)
            return;

        lastTypingTime = Time.unscaledTime;

        float pitch = randomizeTypingPitch
            ? Random.Range(typingMinPitch, typingMaxPitch)
            : 1f;

        PlayOneShot(dialogueTypingClip, dialogueTypingVolume, pitch);
    }

    public void PlayGameOver()
    {
        PlayOneShot(gameOverClip, gameOverVolume, 1f);
    }

    public void PlayStageClear()
    {
        if (Time.unscaledTime - lastStageClearTime < stageClearMinInterval)
            return;

        lastStageClearTime = Time.unscaledTime;
        PlayOneShot(stageClearClip, stageClearVolume, 1f);
    }

    void PlayOneShot(AudioClip clip, float volume, float pitch)
    {
        if (source == null || clip == null)
            return;

        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }
}