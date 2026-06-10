using UnityEngine;

public class PlayerMovementAudio : MonoBehaviour
{
    [Header("Refs")]
    public PlayerController player;
    public ShadowInteractController shadow;

    [Header("Audio Sources")]
    public AudioSource footstepSource;
    public AudioSource oneShotSource;

    [Header("Footstep")]
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float walkVolume = 0.45f;
    [Range(0f, 1f)] public float crouchVolume = 0.12f;
    public float walkPitch = 1f;
    public float crouchPitch = 0.85f;
    public float footstepFadeSpeed = 8f;
    public float moveThreshold = 0.05f;

    [Header("Hurt")]
    public AudioClip hurtClip;
    [Range(0f, 1f)] public float hurtVolume = 0.65f;
    public float hurtMinInterval = 0.08f;

    float lastHurtTime = -999f;

    void Awake()
    {
        ResolveRefs();
        SetupSources();
    }

    void Update()
    {
        UpdateFootstepLoop();
    }

    void ResolveRefs()
    {
        if (player == null)
            player = GetComponent<PlayerController>();

        if (shadow == null)
            shadow = GetComponent<ShadowInteractController>();
    }

    void SetupSources()
    {
        if (footstepSource == null)
            footstepSource = gameObject.AddComponent<AudioSource>();

        if (oneShotSource == null)
            oneShotSource = gameObject.AddComponent<AudioSource>();

        footstepSource.playOnAwake = false;
        footstepSource.loop = true;
        footstepSource.spatialBlend = 0f;
        footstepSource.volume = 0f;
        footstepSource.clip = footstepClip;

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;
    }

    void UpdateFootstepLoop()
    {
        if (player == null || footstepSource == null)
            return;

        if (footstepClip == null)
        {
            if (footstepSource.isPlaying)
                footstepSource.Stop();

            return;
        }

        if (footstepSource.clip != footstepClip)
            footstepSource.clip = footstepClip;

        bool inShadow = shadow != null && shadow.IsInShadowMode;

        bool shouldPlay =
            !inShadow &&
            player.IsGrounded &&
            !player.InputLocked &&
            !player.IsShadowTransitionPlaying &&
            player.MoveDirection.sqrMagnitude > moveThreshold * moveThreshold;

        float targetVolume = 0f;
        float targetPitch = walkPitch;

        if (shouldPlay)
        {
            if (player.IsCrouching)
            {
                targetVolume = crouchVolume;
                targetPitch = crouchPitch;
            }
            else
            {
                targetVolume = walkVolume;
                targetPitch = walkPitch;
            }
        }

        footstepSource.pitch = Mathf.Lerp(
            footstepSource.pitch,
            targetPitch,
            1f - Mathf.Exp(-footstepFadeSpeed * Time.deltaTime)
        );

        footstepSource.volume = Mathf.MoveTowards(
            footstepSource.volume,
            targetVolume,
            footstepFadeSpeed * Time.deltaTime
        );

        if (targetVolume > 0f)
        {
            if (!footstepSource.isPlaying)
                footstepSource.Play();
        }
        else
        {
            if (footstepSource.isPlaying && footstepSource.volume <= 0.001f)
                footstepSource.Stop();
        }
    }

    public void PlayHurt()
    {
        if (hurtClip == null || oneShotSource == null)
            return;

        if (Time.time - lastHurtTime < hurtMinInterval)
            return;

        lastHurtTime = Time.time;
        oneShotSource.pitch = Random.Range(0.96f, 1.04f);
        oneShotSource.PlayOneShot(hurtClip, hurtVolume);
    }
}