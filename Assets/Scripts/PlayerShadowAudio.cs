using UnityEngine;

public class PlayerShadowAudio : MonoBehaviour
{
    [Header("Refs")]
    public ShadowInteractController shadow;
    public PlayerController player;

    [Header("Audio Sources")]
    public AudioSource oneShotSource;
    public AudioSource assassinationSource;
    public AudioSource shadowMoveLoopSource;

    [Header("Shadow Mode")]
    public AudioClip shadowEnterClip;
    public AudioClip shadowExitClip;
    [Range(0f, 1f)] public float shadowEnterVolume = 0.5f;
    [Range(0f, 1f)] public float shadowExitVolume = 0.45f;

    [Header("Shadow Move Loop")]
    public AudioClip shadowMoveLoopClip;
    [Range(0f, 1f)] public float shadowMoveLoopVolume = 0.18f;
    public float moveLoopFadeSpeed = 6f;
    public float moveThreshold = 0.05f;

    [Header("Shadow Blink")]
    public AudioClip shadowBlinkClip;
    [Range(0f, 1f)] public float shadowBlinkVolume = 0.6f;

    [Header("Assassination")]
    public AudioClip bulletTimeClip;
    public AudioClip shadowAssassinationClip;
    [Range(0f, 1f)] public float bulletTimeVolume = 0.55f;
    [Range(0f, 1f)] public float shadowAssassinationVolume = 0.75f;
    public bool stopBulletTimeWhenAssassinationHits = false;

    [Header("Pitch")]
    public bool usePitchRandomness = true;
    public float minPitch = 0.96f;
    public float maxPitch = 1.04f;

    bool suppressNextExitSound;

    void Awake()
    {
        ResolveRefs();
        SetupSources();
    }

    void OnEnable()
    {
        ResolveRefs();

        if (shadow != null)
            shadow.OnShadowModeChanged += HandleShadowModeChanged;
    }

    void OnDisable()
    {
        if (shadow != null)
            shadow.OnShadowModeChanged -= HandleShadowModeChanged;
    }

    void Update()
    {
        UpdateShadowMoveLoop();
    }

    void ResolveRefs()
    {
        if (shadow == null)
            shadow = GetComponent<ShadowInteractController>();

        if (player == null)
            player = GetComponent<PlayerController>();
    }

    void SetupSources()
    {
        if (oneShotSource == null)
            oneShotSource = gameObject.AddComponent<AudioSource>();

        if (assassinationSource == null)
            assassinationSource = gameObject.AddComponent<AudioSource>();

        if (shadowMoveLoopSource == null)
            shadowMoveLoopSource = gameObject.AddComponent<AudioSource>();

        SetupOneShotSource(oneShotSource);
        SetupOneShotSource(assassinationSource);

        shadowMoveLoopSource.playOnAwake = false;
        shadowMoveLoopSource.loop = true;
        shadowMoveLoopSource.spatialBlend = 0f;
        shadowMoveLoopSource.volume = 0f;
        shadowMoveLoopSource.clip = shadowMoveLoopClip;
    }

    void SetupOneShotSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        source.pitch = 1f;
    }

    void HandleShadowModeChanged(bool inShadow)
    {
        if (inShadow)
        {
            PlayOneShot(oneShotSource, shadowEnterClip, shadowEnterVolume);
            return;
        }

        if (suppressNextExitSound)
        {
            suppressNextExitSound = false;
            return;
        }

        PlayOneShot(oneShotSource, shadowExitClip != null ? shadowExitClip : shadowEnterClip, shadowExitVolume);
    }

    void UpdateShadowMoveLoop()
    {
        if (shadowMoveLoopSource == null)
            return;

        if (shadowMoveLoopClip == null)
        {
            if (shadowMoveLoopSource.isPlaying)
                shadowMoveLoopSource.Stop();

            return;
        }

        if (shadowMoveLoopSource.clip != shadowMoveLoopClip)
            shadowMoveLoopSource.clip = shadowMoveLoopClip;

        bool shouldPlay =
            shadow != null &&
            player != null &&
            shadow.IsInShadowMode &&
            !player.InputLocked &&
            !player.IsShadowTransitionPlaying &&
            player.MoveDirection.sqrMagnitude > moveThreshold * moveThreshold;

        float targetVolume = shouldPlay ? shadowMoveLoopVolume : 0f;

        shadowMoveLoopSource.volume = Mathf.MoveTowards(
            shadowMoveLoopSource.volume,
            targetVolume,
            moveLoopFadeSpeed * Time.unscaledDeltaTime
        );

        if (targetVolume > 0f)
        {
            if (!shadowMoveLoopSource.isPlaying)
                shadowMoveLoopSource.Play();
        }
        else
        {
            if (shadowMoveLoopSource.isPlaying && shadowMoveLoopSource.volume <= 0.001f)
                shadowMoveLoopSource.Stop();
        }
    }

    public void PlayShadowBlink()
    {
        PlayOneShot(oneShotSource, shadowBlinkClip, shadowBlinkVolume);
    }

    public void PlayAssassinationBulletTime()
    {
        PlayOneShot(assassinationSource, bulletTimeClip, bulletTimeVolume);
    }

    public void PlayShadowAssassination()
    {
        if (stopBulletTimeWhenAssassinationHits && assassinationSource != null)
            assassinationSource.Stop();

        PlayOneShot(assassinationSource, shadowAssassinationClip, shadowAssassinationVolume);
    }

    public void SuppressNextShadowExitSound()
    {
        suppressNextExitSound = true;
    }

    void PlayOneShot(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
            return;

        source.pitch = usePitchRandomness
            ? Random.Range(minPitch, maxPitch)
            : 1f;

        source.PlayOneShot(clip, volume);
    }
}