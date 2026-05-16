using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Stage1KeyItemPickup : MonoBehaviour
{
    [Header("Options")]
    public bool hideOnPickup = true;

    bool collected;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        collected = true;

        if (Stage1ObjectiveState.Instance != null)
            Stage1ObjectiveState.Instance.CollectKeyItem();
        else
            Debug.LogWarning("[Stage1KeyItemPickup] Stage1ObjectiveState가 없습니다.");

        if (hideOnPickup)
            gameObject.SetActive(false);
    }
}