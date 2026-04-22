using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteractController : MonoBehaviour
{
    [Header("Input")]
    public KeyCode interactKey = KeyCode.F;

    [Header("Search")]
    public LayerMask interactableMask = ~0;
    public float searchRadius = 1.2f;

    [Header("Rules")]
    public bool allowWhileInShadowMode = false;
    [Range(0.1f, 1f)] public float pushingMoveSpeedMultiplier = 0.55f;

    readonly Collider[] overlapResults = new Collider[16];

    PlayerController player;
    ShadowInteractController shadowCtrl;
    PlayerInteractable activeInteractable;

    public PlayerController Player => player;
    public bool IsHoldingInteract => Input.GetKey(interactKey);
    public bool IsInteracting => activeInteractable != null;
    public PlayerInteractable ActiveInteractable => activeInteractable;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        shadowCtrl = GetComponent<ShadowInteractController>();
    }

    void Update()
    {
        if (player == null)
            return;

        // 입력 잠금 중에는 상호작용 중단
        if (player.InputLocked)
        {
            StopInteraction();
            return;
        }

        // 그림자 모드에서 밀기 금지
        if (!allowWhileInShadowMode && shadowCtrl != null && shadowCtrl.IsInShadowMode)
        {
            StopInteraction();
            return;
        }

        bool holding = Input.GetKey(interactKey);
        PlayerInteractable candidate = FindBestInteractable();

        if (!holding)
        {
            StopInteraction();
            return;
        }

        if (activeInteractable == null)
        {
            if (candidate != null)
                StartInteraction(candidate);
        }
        else
        {
            if (!activeInteractable.CanInteract(this))
            {
                StopInteraction();

                if (candidate != null)
                    StartInteraction(candidate);
            }
            else
            {
                activeInteractable.TickInteract(this);
            }
        }

        float speedMul = activeInteractable is PushableObject
            ? pushingMoveSpeedMultiplier
            : 1f;

        player.SetExternalMoveSpeedMultiplier(speedMul);
    }

    void OnDisable()
    {
        StopInteraction();
    }

    PlayerInteractable FindBestInteractable()
    {
        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            searchRadius,
            overlapResults,
            interactableMask,
            QueryTriggerInteraction.Collide
        );

        PlayerInteractable best = null;
        float bestDistSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapResults[i];
            if (!col) continue;

            PlayerInteractable interactable = col.GetComponentInParent<PlayerInteractable>();
            if (interactable == null) continue;
            if (!interactable.CanInteract(this)) continue;

            Vector3 point = interactable.GetInteractionPoint(this);
            point.y = transform.position.y;

            float distSqr = (point - transform.position).sqrMagnitude;
            if (distSqr >= bestDistSqr) continue;

            best = interactable;
            bestDistSqr = distSqr;
        }

        return best;
    }

    void StartInteraction(PlayerInteractable interactable)
    {
        activeInteractable = interactable;
        activeInteractable.BeginInteract(this);
    }

    void StopInteraction()
    {
        if (activeInteractable != null)
        {
            activeInteractable.EndInteract(this);
            activeInteractable = null;
        }

        if (player != null)
            player.SetExternalMoveSpeedMultiplier(1f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
#endif
}