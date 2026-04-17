using UnityEngine;

[CreateAssetMenu(fileName = "RangedEnemyConfig", menuName = "Scriptable Objects/RangedEnemyConfig")]
public class RangedEnemyConfig : EnemyConfig
{
    [Header("Ranged Attack")]
    public float attackRange = 6f;
    public float fireCooldown = 1.2f;
    public int projectileDamagePips = 1;
    public float projectileSpeed = 10f;
    public float projectileLifeTime = 3f;

    [Header("After Lost Sight")]
    public float fireAfterLostSightSeconds = 1.0f; // 벽 뒤에 숨은 뒤에도 이 시간만큼은 계속 사격
}