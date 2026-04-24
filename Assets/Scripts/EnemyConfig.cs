using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Scriptable Objects/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [Header("Vision")]
    public float viewDistance = 8f;                  // 바깥쪽 경계 범위
    public float attackViewDistance = 3f;            // 가까운 즉시 발각 범위
    [Range(0f, 180f)] public float viewAngle = 90f;  
    public float targetPointYOffset = 0.8f;

    [Header("Visual Alert")]
    public float detectTimeRequired = 1.2f;          // 경계 게이지가 가득 차는 시간
    public float alertDecaySeconds = 1.0f;           // 경계 게이지가 0까지 떨어지는 시간
    public float loseSightGrace = 0.2f;              // 잠깐 가려져도 바로 감소하지 않는 유예 시간
    [Range(0f, 1f)] public float lostAlertStart01 = 0.6f;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float returnSpeed = 2.0f;
    public float soundMoveSpeed = 2.0f;
    public float stopDistance = 0.8f;
    public float turnSpeed = 12f;

    [Header("After Lost")]
    public float waitAfterLost = 1.0f;

    [Header("Sound")]
    public float hearingSensitivity = 1.0f;
    public float soundAlertFillSeconds = 0.8f;
    public float soundAlertDecaySeconds = 1.2f;
    public float soundInvestigateWait = 0.7f;
    public float soundStopDistance = 0.35f;
    public float sameNoiseIgnoreSeconds = 0.6f;
    public float sameNoiseUpdateDistance = 1.0f;
    public float soundGiveUpSeconds = 3.0f;

    [Header("Damage")]
    public int contactDamagePips = 1;
    public float contactDamageCooldown = 0.6f;

    [Header("Chase Loss")]
    public float loseChaseAfterNotSeenSeconds = 0.6f;

    [Header("Assassination")]
    public bool canBeAssassinated = true;
}