using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SceneBGM
{
    public string sceneName;
    public AudioClip clip;
}

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource musicSource;

    [Header("Scene BGM")]
    public SceneBGM[] sceneBGMs;

    [Header("Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.35f;

    [Header("Fade")]
    public bool useFade = true;
    public float fadeOutSeconds = 0.5f;
    public float fadeInSeconds = 0.7f;

    Coroutine fadeRoutine;
    string currentSceneName;
    AudioClip currentClip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = bgmVolume;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void Start()
    {
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    public void PlayForScene(string sceneName)
    {
        currentSceneName = sceneName;

        AudioClip nextClip = GetClipForScene(sceneName);

        if (nextClip == null)
        {
            StopBGM();
            return;
        }

        if (currentClip == nextClip && musicSource.isPlaying)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (useFade)
            fadeRoutine = StartCoroutine(ChangeMusicRoutine(nextClip));
        else
            PlayImmediate(nextClip);
    }

    AudioClip GetClipForScene(string sceneName)
    {
        if (sceneBGMs == null)
            return null;

        for (int i = 0; i < sceneBGMs.Length; i++)
        {
            SceneBGM item = sceneBGMs[i];

            if (item == null)
                continue;

            if (item.sceneName == sceneName)
                return item.clip;
        }

        return null;
    }

    IEnumerator ChangeMusicRoutine(AudioClip nextClip)
    {
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float t = 0f;
            float duration = Mathf.Max(0.01f, fadeOutSeconds);

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                musicSource.volume = Mathf.Lerp(startVolume, 0f, u);
                yield return null;
            }
        }

        musicSource.Stop();
        musicSource.clip = nextClip;
        currentClip = nextClip;

        musicSource.volume = 0f;
        musicSource.Play();

        {
            float t = 0f;
            float duration = Mathf.Max(0.01f, fadeInSeconds);

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                musicSource.volume = Mathf.Lerp(0f, bgmVolume, u);
                yield return null;
            }
        }

        musicSource.volume = bgmVolume;
        fadeRoutine = null;
    }

    void PlayImmediate(AudioClip clip)
    {
        currentClip = clip;
        musicSource.clip = clip;
        musicSource.volume = bgmVolume;
        musicSource.Play();
    }

    public void StopBGM()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        currentClip = null;

        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);

        if (musicSource != null)
            musicSource.volume = bgmVolume;
    }
}