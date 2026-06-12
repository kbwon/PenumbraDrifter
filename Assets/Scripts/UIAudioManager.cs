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
    public AudioClip itemPickupClip;

    [Header("Volumes")]
    [Range(0f, 1f)] public float buttonClickVolume = 0.5f;
    [Range(0f, 1f)] public float dialogueTypingVolume = 0.25f;
    [Range(0f, 1f)] public float gameOverVolume = 0.75f;
    [Range(0f, 1f)] public float stageClearVolume = 0.75f;
    [Range(0f, 1f)] public float itemPickupVolume = 0.5f;

    float lastItemPickupTime = -999f;
    float itemPickupMinInterval = 0.05f;

    [Header("Typing")]
    public float typingMinInterval = 0.035f;
    public bool randomizeTypingPitch = true;
    public float typingMinPitch = 0.96f;
    public float typingMaxPitch = 1.04f;

    [Header("Register Buttons")]
    public bool autoRegisterSceneButtons = true;
    public bool includeInactiveButtons = true;

    [Header("Register Filter")]
    public string ignoreButtonRootTag = "PauseMenu";

    [Header("Dialogue Typing Loop")]
    public AudioSource dialogueTypingSource;
    public bool useDialogueTypingLoop = true;
    [Range(0f, 1f)] public float dialogueTypingLoopVolume = 0.18f;

    float lastTypingTime = -999f;
    float lastStageClearTime = -999f;
    float stageClearMinInterval = 1.0f;

    GameManager subscribedGameManager;
    public bool skipWhenGamePaused = true;

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

        if (dialogueTypingSource == null)
            dialogueTypingSource = gameObject.AddComponent<AudioSource>();

        dialogueTypingSource.playOnAwake = false;
        dialogueTypingSource.loop = true;
        dialogueTypingSource.spatialBlend = 0f;
        dialogueTypingSource.volume = dialogueTypingLoopVolume;
        dialogueTypingSource.clip = dialogueTypingClip;
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

            if (IsUnderIgnoredRoot(button.transform))
                continue;

            UIAudioButtonClick clickAudio = button.GetComponent<UIAudioButtonClick>();

            if (clickAudio == null)
                clickAudio = button.gameObject.AddComponent<UIAudioButtonClick>();

            clickAudio.manager = this;
        }
    }

    bool IsUnderIgnoredRoot(Transform target)
    {
        if (target == null)
            return false;

        Transform t = target;

        while (t != null)
        {
            if (t.CompareTag(ignoreButtonRootTag))
                return true;

            t = t.parent;
        }

        return false;
    }

    public void PlayButtonClick()
    {
        if (skipWhenGamePaused &&
        GameManager.Instance != null &&
        GameManager.Instance.IsPaused)
        {
            return;
        }
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

    public void PlayItemPickup()
    {
        if (Time.unscaledTime - lastItemPickupTime < itemPickupMinInterval)
            return;

        lastItemPickupTime = Time.unscaledTime;
        PlayOneShot(itemPickupClip, itemPickupVolume, 1f);
    }

    void PlayOneShot(AudioClip clip, float volume, float pitch)
    {
        if (source == null || clip == null)
            return;

        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
    }

    public void StartDialogueTypingLoop()
    {
        if (!useDialogueTypingLoop)
            return;

        if (dialogueTypingSource == null || dialogueTypingClip == null)
            return;

        if (dialogueTypingSource.clip != dialogueTypingClip)
            dialogueTypingSource.clip = dialogueTypingClip;

        dialogueTypingSource.volume = dialogueTypingLoopVolume;

        if (!dialogueTypingSource.isPlaying)
            dialogueTypingSource.Play();
    }

    public void StopDialogueTypingLoop()
    {
        if (!useDialogueTypingLoop)
            return;

        if (dialogueTypingSource == null)
            return;

        dialogueTypingSource.Stop();
    }
}