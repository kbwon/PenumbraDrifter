using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyProjectile : MonoBehaviour
{
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
        moveDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        moveSpeed = speed;
        damagePips = damage;
        lifeTime = destroyAfter;
        this.targetTag = targetTag;
        initialized = true;

        if (ownerCollider != null && myCollider != null)
            Physics.IgnoreCollision(myCollider, ownerCollider);

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!initialized) return;
        transform.position += moveDirection * (moveSpeed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!initialized || other == null) return;

        // 트리거끼리는 무시
        if (other.isTrigger) return;

        PlayerHealth hp = other.GetComponentInParent<PlayerHealth>();
        if (hp != null && hp.CompareTag(targetTag))
        {
            hp.TakeDamage(damagePips);
            Destroy(gameObject);
            return;
        }

        // 플레이어가 아니면 벽/바닥 등에 맞은 것으로 보고 제거
        Destroy(gameObject);
    }
}