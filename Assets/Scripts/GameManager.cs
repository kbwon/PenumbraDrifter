using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Refs")]
    public PlayerController player;
    public ShadowInteractController shadow;
    public ShadowTeleport teleport;
    public PlayerHealth health;
    public FollowCamera followCamera;

    public bool IsGameOver { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheRefs();
    }

    void OnEnable()
    {
        CacheRefs();

        if (health != null)
            health.OnDead += HandlePlayerDead;
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDead -= HandlePlayerDead;
    }

    public void CacheRefs()
    {
        if (!player) player = FindFirstObjectByType<PlayerController>();
        if (!shadow) shadow = FindFirstObjectByType<ShadowInteractController>();
        if (!teleport) teleport = FindFirstObjectByType<ShadowTeleport>();
        if (!health) health = FindFirstObjectByType<PlayerHealth>();
        if (!followCamera) followCamera = FindFirstObjectByType<FollowCamera>();
    }

    void HandlePlayerDead()
    {
        IsGameOver = true;
    }
}
