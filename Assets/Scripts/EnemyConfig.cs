using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Scriptable Objects/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [Header("Vision")]
    public float viewDistance = 8f;
    [Range(0f, 180f)] public float viewAngle = 90f;
    public float detectTimeRequired = 1.2f;   // 1~2초 추천
    public float loseSightGrace = 0.2f;       // 잠깐 가려져도 바로 놓치지 않게

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float returnSpeed = 2.0f;
    public float stopDistance = 0.8f;         // 플레이어 앞에서 멈출 거리

    [Header("After Lost")]
    public float waitAfterLost = 1.0f;        // 못 찾으면 잠깐 멈춰있는 시간

    [Header("Damage")]
    public int contactDamagePips = 1;
    public float contactDamageCooldown = 0.6f;

    [Header("Chase Loss")]
    public float loseChaseAfterNotSeenSeconds = 1.0f;
}
