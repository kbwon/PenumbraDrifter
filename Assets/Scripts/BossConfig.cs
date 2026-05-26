using UnityEngine;

[CreateAssetMenu(fileName = "BossConfig", menuName = "Scriptable Objects/BossConfig")]
public class BossConfig : EnemyConfig
{
    [Header("Boss HP")]
    public int maxHp = 3;

    [Header("Boss Pattern Ranges")]
    public float punchRange = 1.8f;
    public float groundSlamRange = 4.2f;
    public float chargeMinRange = 4.0f;
    public float chargeMaxRange = 8.0f;

    [Header("Pattern Cooldowns")]
    public float punchCooldown = 1.2f;
    public float groundSlamCooldown = 3.5f;
    public float chargeCooldown = 5.0f;
    public float shadowGrabCooldown = 1.5f;

    [Header("Damage")]
    public int punchDamagePips = 1;
    public int groundSlamDamagePips = 1;
    public int chargeDamagePips = 1;

    [Header("Ground Slam")]
    public float groundSlamRadius = 4.0f;
    [Range(0f, 180f)] public float groundSlamAngle = 120f;

    [Header("Charge")]
    public float chargeSpeed = 9.5f;
    public float chargeDuration = 0.75f;
    public float chargeHitRadius = 1.1f;

    [Header("Shadow Grab")]
    public float shadowGrabThrowDistance = 3.0f;
}