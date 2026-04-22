using UnityEngine;

public abstract class PlayerInteractable : MonoBehaviour
{
    public virtual bool CanInteract(PlayerInteractController interactor) => true;

    public virtual void BeginInteract(PlayerInteractController interactor) { }

    public virtual void TickInteract(PlayerInteractController interactor) { }

    public virtual void EndInteract(PlayerInteractController interactor) { }

    public virtual Vector3 GetInteractionPoint(PlayerInteractController interactor)
    {
        return transform.position;
    }
}