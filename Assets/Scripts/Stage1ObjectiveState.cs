using System;
using UnityEngine;

public class Stage1ObjectiveState : MonoBehaviour
{
    public static Stage1ObjectiveState Instance { get; private set; }

    [Header("State")]
    public bool hasKeyItem;
    public bool hasCollectible;

    public event Action OnKeyItemCollected;
    public event Action OnCollectibleCollected;

    void Awake()
    {
        Instance = this;
    }

    public void CollectKeyItem()
    {
        if (hasKeyItem) return;

        hasKeyItem = true;
        Debug.Log("[Stage1] Ä«µåÅ° È¹µæ");

        OnKeyItemCollected?.Invoke();
    }

    public void CollectCollectible()
    {
        if (hasCollectible) return;

        hasCollectible = true;
        Debug.Log("[Stage1] ¼öÁý ¿ä¼Ò È¹µæ");

        OnCollectibleCollected?.Invoke();
    }
}