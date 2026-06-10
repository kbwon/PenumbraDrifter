using UnityEngine;
using UnityEngine.AI;

public class EnemyAudio : MonoBehaviour
{
    [Header("Refs")]
    public EnemyController enemy;
    public Rigidbody rb;
    public NavMeshAgent agent;

    [Header("Audio Sources")]
    public AudioSource footstepSource;
    public AudioSource oneShotSource;

    [Header("Footstep")]
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float footstepVolume = 0.25f;
    public float footstepPitch = 0.9f;
    public float footstepFadeSpeed = 8f;
    public float moveThreshold = 0.05f;

    [Header("Alert")]
    public AudioClip spottedClip;
    [Range(0f, 1f)] public float spottedVolume = 0.7f;
    public float spottedMinInterval = 0.25f;

    [Header("Attack")]
    public AudioClip meleeAttackClip;
    public AudioClip rangedAttackClip;
    [Range(0f, 1f)] public float meleeAttackVolume = 0.55f;
    [Range(0f, 1f)] public float rangedAttackVolume = 0.55f;

    [Header("Pitch Random")]
    public bool randomizeOneShotPitch = true;
    public float minPitch = 0.96f;
    public float maxPitch = 1.04f;

    [Header("3D Sound Optional")]
    public bool use3DSound = false;
    public float minDistance = 2f;
    public float maxDistance = 14f;

    float lastSpottedTime = -999f;

    void Awake()
    {
        ResolveRefs();
        SetupSources();
    }

    void OnDisable()
    {
        if (footstepSource != null)
            footstepSource.Stop();
    }

    void Update()
    {
        UpdateFootstepLoop();
    }

    void ResolveRefs()
    {
        if (enemy == null)
            enemy = GetComponent<EnemyController>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
    }

    void SetupSources()
    {
        if (footstepSource == null)
            footstepSource = gameObject.AddComponent<AudioSource>();

        if (oneShotSource == null)
            oneShotSource = gameObject.AddComponent<AudioSource>();

        footstepSource.playOnAwake = false;
        footstepSource.loop = true;
        footstepSource.volume = 0f;
        footstepSource.pitch = footstepPitch;
        footstepSource.clip = footstepClip;

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.volume = 1f;

        ApplySourceMode(footstepSource);
        ApplySourceMode(oneShotSource);
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

    void UpdateFootstepLoop()
    {
        if (footstepSource == null)
            return;

        if (footstepClip == null)
        {
            if (footstepSource.isPlaying)
                footstepSource.Stop();

            return;
        }

        if (footstepSource.clip != footstepClip)
            footstepSource.clip = footstepClip;

        bool shouldPlay = IsMoving();

        float targetVolume = shouldPlay ? footstepVolume : 0f;

        footstepSource.pitch = footstepPitch;

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

    bool IsMoving()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            Vector3 v = agent.velocity;
            v.y = 0f;
            return v.sqrMagnitude > moveThreshold * moveThreshold;
        }

        if (rb != null)
        {
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            return v.sqrMagnitude > moveThreshold * moveThreshold;
        }

        return false;
    }

    public void PlaySpotted()
    {
        if (Time.time - lastSpottedTime < spottedMinInterval)
            return;

        lastSpottedTime = Time.time;
        PlayOneShot(spottedClip, spottedVolume);
    }

    public void PlayMeleeAttack()
    {
        PlayOneShot(meleeAttackClip, meleeAttackVolume);
    }

    public void PlayRangedAttack()
    {
        PlayOneShot(rangedAttackClip, rangedAttackVolume);
    }

    void PlayOneShot(AudioClip clip, float volume)
    {
        if (oneShotSource == null || clip == null)
            return;

        oneShotSource.pitch = randomizeOneShotPitch
            ? Random.Range(minPitch, maxPitch)
            : 1f;

        oneShotSource.PlayOneShot(clip, volume);
    }
}