using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Refs")]
    public Camera mainCamera;
    public PlayerController player;
    public ShadowInteractController shadow;
    public ShadowTeleport teleport;
    public PlayerHealth health;
    public FollowCamera followCamera;

    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }
    public Transform PlayerTransform => player != null ? player.transform : null;
    public Transform MainCameraTransform => mainCamera != null ? mainCamera.transform : null;
    public string CurrentSceneName => SceneManager.GetActiveScene().name;

    public event Action<bool> OnGameOverChanged;
    public event Action<bool> OnPauseChanged;

    PlayerHealth subscribedHealth;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CacheSceneRefs();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BindHealthEvent(health);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        BindHealthEvent(null);
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheSceneRefs();
        SetPaused(false);
        SetGameOver(false);
    }

    public void CacheSceneRefs()
    {
        if (mainCamera == null || !mainCamera.gameObject.scene.IsValid())
            mainCamera = Camera.main;

        if (player == null || !player.gameObject.scene.IsValid())
            player = FindFirstObjectByType<PlayerController>();

        if (shadow == null || !shadow.gameObject.scene.IsValid())
            shadow = FindFirstObjectByType<ShadowInteractController>();

        if (teleport == null || !teleport.gameObject.scene.IsValid())
            teleport = FindFirstObjectByType<ShadowTeleport>();

        if (health == null || !health.gameObject.scene.IsValid())
            health = FindFirstObjectByType<PlayerHealth>();

        if (followCamera == null || !followCamera.gameObject.scene.IsValid())
            followCamera = FindFirstObjectByType<FollowCamera>();

        if (followCamera != null && player != null && followCamera.target == null)
            followCamera.target = player.transform;

        BindHealthEvent(health);
    }

    public void RegisterPlayer(PlayerController value)
    {
        if (value == null) return;
        player = value;

        if (followCamera != null && followCamera.target == null)
            followCamera.target = value.transform;
    }

    public void RegisterShadow(ShadowInteractController value)
    {
        if (value == null) return;
        shadow = value;
    }

    public void RegisterTeleport(ShadowTeleport value)
    {
        if (value == null) return;
        teleport = value;
    }

    public void RegisterHealth(PlayerHealth value)
    {
        if (value == null) return;
        health = value;
        BindHealthEvent(value);
    }

    public void RegisterFollowCamera(FollowCamera value)
    {
        if (value == null) return;
        followCamera = value;

        if (player != null && followCamera.target == null)
            followCamera.target = player.transform;
    }

    public void RegisterMainCamera(Camera value)
    {
        if (value == null) return;
        mainCamera = value;
    }

    public void SetGameOver(bool value)
    {
        if (IsGameOver == value) return;

        IsGameOver = value;
        OnGameOverChanged?.Invoke(value);
    }

    public void SetPaused(bool value)
    {
        if (IsPaused == value) return;

        IsPaused = value;
        Time.timeScale = value ? 0f : 1f;
        OnPauseChanged?.Invoke(value);
    }

    void HandlePlayerDead()
    {
        SetGameOver(true);
    }

    void BindHealthEvent(PlayerHealth next)
    {
        if (subscribedHealth != null)
            subscribedHealth.OnDead -= HandlePlayerDead;

        subscribedHealth = next;

        if (subscribedHealth != null)
            subscribedHealth.OnDead += HandlePlayerDead;
    }
}
