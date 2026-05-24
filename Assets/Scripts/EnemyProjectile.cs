using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Visual")]
    public Transform visualRoot;

    [Tooltip("SpriteRenderer가 들어있는 자식 오브젝트를 XZ 평면에 눕히는 각도입니다. 보통 90입니다.")]
    public float flatPitch = 90f;

    [Tooltip("총알 머리 방향이 안 맞을 때 90, -90, 180 등으로 보정합니다.")]
    public float spriteYawOffset = 0f;

    protected Vector3 moveDirection = Vector3.forward;
    protected float moveSpeed = 10f;
    protected int damagePips = 1;
    protected float lifeTime = 3f;
    protected string targetTag = "Player";
    protected bool initialized;

    protected Collider myCollider;

    void Awake()
    {
        myCollider = GetComponent<Collider>();
        myCollider.isTrigger = true;

        if (visualRoot == null)
        {
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                visualRoot = sr.transform;
        }
    }

    public void Initialize(
    Vector3 direction,
    float speed,
    int damage,
    float destroyAfter,
    string targetTag,
    Collider ownerCollider = null
)
    {
        moveDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : transform.forward;

        // 제거해야 함:
        // moveDirection.y = 0f;
        // moveDirection.Normalize();

        moveSpeed = speed;
        damagePips = damage;
        lifeTime = destroyAfter;
        this.targetTag = targetTag;
        initialized = true;

        // 루트 자체가 실제 발사 방향을 바라보게 한다.
        transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

        ApplyFlatVisualRotation();

        if (ownerCollider != null && myCollider != null)
            Physics.IgnoreCollision(myCollider, ownerCollider);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!initialized) return;

        transform.position += moveDirection * (moveSpeed * Time.deltaTime);
    }

    void ApplyFlatVisualRotation()
    {
        if (visualRoot == null) return;
        if (visualRoot == transform) return;

        visualRoot.localRotation = Quaternion.Euler(
            flatPitch,
            0f,
            spriteYawOffset
        );
    }

    void OnTriggerEnter(Collider other)
    {
        if (!initialized || other == null) return;
        if (other.isTrigger) return;

        PlayerHealth hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null && hp.CompareTag(targetTag))
        {
            hp.TakeDamage(damagePips);
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}