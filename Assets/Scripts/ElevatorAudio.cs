using UnityEngine;

public class ElevatorAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource loopSource;
    public AudioSource oneShotSource;
    public AudioSource horizontalSource;

    [Header("Elevator Loop")]
    public AudioClip elevatorLoopClip;
    [Range(0f, 1f)] public float elevatorLoopVolume = 0.35f;
    public float loopFadeSpeed = 1.5f;
    public float minLoopPitch = 0.9f;
    public float maxLoopPitch = 1.1f;

    [Header("Door / Arrival")]
    public AudioClip elevatorDoorClip;
    [Range(0f, 1f)] public float elevatorDoorVolume = 0.65f;

    [Header("Horizontal Transition")]
    public AudioClip horizontalMoveClip;
    [Range(0f, 1f)] public float horizontalMoveVolume = 0.7f;

    float targetLoopVolume;
    float targetLoopPitch = 1f;

    void Awake()
    {
        SetupSources();
    }

    void Update()
    {
        UpdateLoopFade();
    }

    void SetupSources()
    {
        if (loopSource == null)
            loopSource = gameObject.AddComponent<AudioSource>();

        if (oneShotSource == null)
            oneShotSource = gameObject.AddComponent<AudioSource>();

        if (horizontalSource == null)
            horizontalSource = gameObject.AddComponent<AudioSource>();

        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;
        loopSource.volume = 0f;
        loopSource.pitch = 1f;
        loopSource.clip = elevatorLoopClip;

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;

        horizontalSource.playOnAwake = false;
        horizontalSource.loop = false;
        horizontalSource.spatialBlend = 0f;
    }

    void UpdateLoopFade()
    {
        if (loopSource == null)
            return;

        if (elevatorLoopClip == null)
        {
            if (loopSource.isPlaying)
                loopSource.Stop();

            return;
        }

        if (loopSource.clip != elevatorLoopClip)
            loopSource.clip = elevatorLoopClip;

        loopSource.volume = Mathf.MoveTowards(
            loopSource.volume,
            targetLoopVolume,
            loopFadeSpeed * Time.unscaledDeltaTime
        );

        loopSource.pitch = Mathf.MoveTowards(
            loopSource.pitch,
            targetLoopPitch,
            loopFadeSpeed * Time.unscaledDeltaTime
        );

        if (targetLoopVolume > 0f)
        {
            if (!loopSource.isPlaying)
                loopSource.Play();
        }
        else
        {
            if (loopSource.isPlaying && loopSource.volume <= 0.001f)
                loopSource.Stop();
        }
    }

    public void SetElevatorLoopBySpeed(float speed, float referenceSpeed)
    {
        if (elevatorLoopClip == null)
            return;

        float speed01 = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(0.01f, referenceSpeed));

        targetLoopVolume = speed01 > 0.001f
            ? elevatorLoopVolume * Mathf.Lerp(0.65f, 1f, speed01)
            : 0f;

        targetLoopPitch = Mathf.Lerp(minLoopPitch, maxLoopPitch, speed01);
    }

    public void StopElevatorLoop()
    {
        targetLoopVolume = 0f;
    }

    public void PlayDoor()
    {
        if (oneShotSource == null || elevatorDoorClip == null)
            return;

        oneShotSource.PlayOneShot(elevatorDoorClip, elevatorDoorVolume);
    }

    public void PlayHorizontalMove()
    {
        if (horizontalSource == null || horizontalMoveClip == null)
            return;

        horizontalSource.Stop();
        horizontalSource.clip = horizontalMoveClip;
        horizontalSource.volume = horizontalMoveVolume;
        horizontalSource.pitch = 1f;
        horizontalSource.Play();
    }

    public void StopHorizontalMove()
    {
        if (horizontalSource == null)
            return;

        horizontalSource.Stop();
    }

    public void StopAll()
    {
        StopElevatorLoop();
        StopHorizontalMove();
    }
}