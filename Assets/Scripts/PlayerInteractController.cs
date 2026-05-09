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

    // 기존 PushableObject가 이 값을 보고 상호작용 유지 여부를 판단하므로,
    // 이제는 "F를 누르고 있음"이 아니라 "상호작용 모드가 켜져 있음"으로 의미를 바꾼다.
    public bool IsHoldingInteract => activeInteractable != null;

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

        if (player.InputLocked)
        {
            StopInteraction();
            return;
        }

        if (!allowWhileInShadowMode && shadowCtrl != null && shadowCtrl.IsInShadowMode)
        {
            StopInteraction();
            return;
        }

        bool pressed = Input.GetKeyDown(interactKey);

        // F를 다시 누르면 현재 상호작용 종료
        if (pressed && activeInteractable != null)
        {
            StopInteraction();
            return;
        }

        // F를 눌렀고 아직 상호작용 중이 아니면 가장 가까운 상호작용 대상을 잡는다.
        if (pressed && activeInteractable == null)
        {
            PlayerInteractable candidate = FindBestInteractable();

            if (candidate != null)
                StartInteraction(candidate);
        }

        // 토글 모드가 켜져 있는 동안 계속 유지
        if (activeInteractable != null)
        {
            if (!activeInteractable.CanInteract(this))
            {
                StopInteraction();
                return;
            }

            activeInteractable.TickInteract(this);
        }

        RefreshPlayerInteractionState();
    }

    void RefreshPlayerInteractionState()
    {
        if (player == null)
            return;

        bool pushing = activeInteractable is PushableObject;

        float speedMul = pushing
            ? pushingMoveSpeedMultiplier
            : 1f;

        player.SetExternalMoveSpeedMultiplier(speedMul);
        player.SetPushing(pushing);
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
        RefreshPlayerInteractionState();
    }

    void StopInteraction()
    {
        if (activeInteractable != null)
        {
            activeInteractable.EndInteract(this);
            activeInteractable = null;
        }

        if (player != null)
        {
            player.SetExternalMoveSpeedMultiplier(1f);
            player.SetPushing(false);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, searchRadius);
    }
#endif
}