using UnityEngine;

public class BossAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource oneShotSource;
    public AudioSource chargeSource;
    public AudioSource footstepLoopSource;

    [Header("Clips")]
    public AudioClip entranceClip;
    public AudioClip punchClip;
    public AudioClip chargeClip;
    public AudioClip groundSlamClip;
    public AudioClip shadowGrabClip;
    public AudioClip hurtDeathClip;

    [Header("Volumes")]
    [Range(0f, 1f)] public float entranceVolume = 0.75f;
    [Range(0f, 1f)] public float punchVolume = 0.75f;
    [Range(0f, 1f)] public float chargeVolume = 0.7f;
    [Range(0f, 1f)] public float groundSlamVolume = 0.85f;
    [Range(0f, 1f)] public float shadowGrabVolume = 0.75f;
    [Range(0f, 1f)] public float hurtDeathVolume = 0.8f;

    [Header("Charge")]
    public bool loopChargeClip = false;

    [Header("Pitch Random")]
    public bool randomizePitch = true;
    public float minPitch = 0.96f;
    public float maxPitch = 1.04f;

    [Header("3D Sound Optional")]
    public bool use3DSound = false;
    public float minDistance = 3f;
    public float maxDistance = 18f;

    [Header("Footstep Loop")]
    public AudioClip footstepLoopClip;
    [Range(0f, 1f)] public float footstepLoopVolume = 0.45f;
    public float footstepFadeSpeed = 5f;
    public float footstepPitch = 0.9f;

    float targetFootstepVolume;

    void Awake()
    {
        SetupSources();
    }

    void OnDisable()
    {
        StopCharge();
        StopFootstepLoop();
    }

    void Update()
    {
        UpdateFootstepLoop();
    }

    void SetupSources()
    {
        if (oneShotSource == null)
            oneShotSource = gameObject.AddComponent<AudioSource>();

        if (chargeSource == null)
            chargeSource = gameObject.AddComponent<AudioSource>();

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.volume = 1f;

        chargeSource.playOnAwake = false;
        chargeSource.loop = loopChargeClip;
        chargeSource.volume = chargeVolume;

        if (footstepLoopSource == null)
            footstepLoopSource = gameObject.AddComponent<AudioSource>();

        footstepLoopSource.playOnAwake = false;
        footstepLoopSource.loop = true;
        footstepLoopSource.volume = 0f;
        footstepLoopSource.pitch = footstepPitch;
        footstepLoopSource.clip = footstepLoopClip;

        ApplySourceMode(footstepLoopSource);
        ApplySourceMode(oneShotSource);
        ApplySourceMode(chargeSource);
    }

    void ApplySourceMode(AudioSource source)
    {
        if (source == null)
            return;

        source.spatialBlend = use3DSound ? 1f : 0f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
    }

    public void PlayEntrance()
    {
        PlayOneShot(entranceClip, entranceVolume);
    }

    public void PlayPunch()
    {
        PlayOneShot(punchClip, punchVolume);
    }

    public void PlayCharge()
    {
        if (chargeSource == null || chargeClip == null)
            return;

        chargeSource.Stop();
        chargeSource.clip = chargeClip;
        chargeSource.loop = loopChargeClip;
        chargeSource.volume = chargeVolume;
        chargeSource.pitch = 1f;
        chargeSource.Play();
    }

    public void StopCharge()
    {
        if (chargeSource != null)
            chargeSource.Stop();
    }

    public void PlayGroundSlam()
    {
        PlayOneShot(groundSlamClip, groundSlamVolume);
    }

    public void PlayShadowGrab()
    {
        PlayOneShot(shadowGrabClip, shadowGrabVolume);
    }

    public void PlayHurtDeath()
    {
        PlayOneShot(hurtDeathClip, hurtDeathVolume);
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (oneShotSource == null || clip == null)
            return;

        oneShotSource.pitch = randomizePitch
            ? Random.Range(minPitch, maxPitch)
            : 1f;

        oneShotSource.PlayOneShot(clip, volume);
    }

    void UpdateFootstepLoop()
    {
        if (footstepLoopSource == null)
            return;

        if (footstepLoopClip == null)
        {
            if (footstepLoopSource.isPlaying)
                footstepLoopSource.Stop();

            return;
        }

        if (footstepLoopSource.clip != footstepLoopClip)
            footstepLoopSource.clip = footstepLoopClip;

        footstepLoopSource.pitch = footstepPitch;

        footstepLoopSource.volume = Mathf.MoveTowards(
            footstepLoopSource.volume,
            targetFootstepVolume,
            footstepFadeSpeed * Time.deltaTime
        );

        if (targetFootstepVolume > 0f)
        {
            if (!footstepLoopSource.isPlaying)
                footstepLoopSource.Play();
        }
        else
        {
            if (footstepLoopSource.isPlaying && footstepLoopSource.volume <= 0.001f)
                footstepLoopSource.Stop();
        }
    }
    public void SetFootstepLoop(bool active)
    {
        targetFootstepVolume = active ? footstepLoopVolume : 0f;
    }

    public void StopFootstepLoop()
    {
        targetFootstepVolume = 0f;

        if (footstepLoopSource != null && footstepLoopSource.volume <= 0.001f)
            footstepLoopSource.Stop();
    }
}