using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableObject : PlayerInteractable
{
    [Header("Refs")]
    public Rigidbody rb;
    public Collider mainCollider;

    [Header("Range")]
    public float interactDistance = 1.2f;

    [Header("Push")]
    public float pushSpeed = 1.25f;
    public float minInputMagnitude = 0.15f;
    [Range(-1f, 1f)] public float minTowardDot = 0.05f;
    public bool snapToWorldAxis = true;

    [Header("Locking")]
    public bool freezeHorizontalWhenIdle = true;

    PlayerInteractController currentInteractor;

    static readonly RigidbodyConstraints FreezeRotAll =
        RigidbodyConstraints.FreezeRotationX |
        RigidbodyConstraints.FreezeRotationY |
        RigidbodyConstraints.FreezeRotationZ;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!mainCollider) mainCollider = GetComponent<Collider>();

        ApplyIdleConstraints();
    }

    public override bool CanInteract(PlayerInteractController interactor)
    {
        if (interactor == null || interactor.Player == null)
            return false;

        Vector3 playerPos = interactor.Player.transform.position;
        Vector3 point = GetInteractionPoint(interactor);
        point.y = playerPos.y;

        return (point - playerPos).sqrMagnitude <= interactDistance * interactDistance;
    }

    public override Vector3 GetInteractionPoint(PlayerInteractController interactor)
    {
        if (mainCollider == null || interactor == null || interactor.Player == null)
            return transform.position;

        return mainCollider.ClosestPoint(interactor.Player.transform.position);
    }

    public override void BeginInteract(PlayerInteractController interactor)
    {
        currentInteractor = interactor;
        ApplyActiveConstraints();
        StopHorizontalMotion();
    }

    public override void TickInteract(PlayerInteractController interactor)
    {
        currentInteractor = interactor;
    }

    public override void EndInteract(PlayerInteractController interactor)
    {
        if (interactor != null && currentInteractor != interactor)
            return;

        currentInteractor = null;
        StopHorizontalMotion();
        ApplyIdleConstraints();
    }

    void FixedUpdate()
    {
        if (currentInteractor == null || !currentInteractor.IsHoldingInteract)
        {
            if (freezeHorizontalWhenIdle)
                StopHorizontalMotion();

            return;
        }

        if (!CanInteract(currentInteractor))
        {
            EndInteract(currentInteractor);
            return;
        }

        Vector3 moveDir = currentInteractor.Player.MoveDirection;
        moveDir.y = 0f;

        if (moveDir.sqrMagnitude < minInputMagnitude * minInputMagnitude)
        {
            StopHorizontalMotion();
            return;
        }

        moveDir.Normalize();

        if (snapToWorldAxis)
            moveDir = SnapToWorldAxis(moveDir);

        Vector3 toObject = GetInteractionPoint(currentInteractor) - currentInteractor.Player.transform.position;
        toObject.y = 0f;

        if (toObject.sqrMagnitude > 0.0001f)
        {
            float dot = Vector3.Dot(moveDir, toObject.normalized);
            if (dot < minTowardDot)
            {
                StopHorizontalMotion();
                return;
            }
        }

        Vector3 delta = moveDir * (pushSpeed * Time.fixedDeltaTime);
        Vector3 nextPos = rb.position + new Vector3(delta.x, 0f, delta.z);
        rb.MovePosition(nextPos);
    }

    Vector3 SnapToWorldAxis(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.z))
            return new Vector3(Mathf.Sign(dir.x), 0f, 0f);

        return new Vector3(0f, 0f, Mathf.Sign(dir.z));
    }

    void ApplyIdleConstraints()
    {
        rb.constraints = freezeHorizontalWhenIdle
            ? FreezeRotAll | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ
            : FreezeRotAll;
    }

    void ApplyActiveConstraints()
    {
        rb.constraints = FreezeRotAll;
    }

    void StopHorizontalMotion()
    {
        Vector3 v = rb.linearVelocity;
        v.x = 0f;
        v.z = 0f;
        rb.linearVelocity = v;
    }
}