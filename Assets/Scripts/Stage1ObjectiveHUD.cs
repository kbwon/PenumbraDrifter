using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Stage1ObjectiveHUD : MonoBehaviour
{
    [Header("Icon Roots")]
    public GameObject keyItemIconRoot;
    public GameObject collectibleIconRoot;

    [Header("Optional Images")]
    public Image keyItemImage;
    public Image collectibleImage;

    [Header("Optional Sprites")]
    public Sprite keyItemSprite;
    public Sprite collectibleSprite;

    [Header("Options")]
    public bool hideIconsOnStart = true;
    public bool waitForStateIfMissing = true;

    Stage1ObjectiveState subscribedState;
    Coroutine waitRoutine;

    void OnEnable()
    {
        ApplySprites();

        if (hideIconsOnStart)
            HideAllIcons();

        TryBindState();

        if (subscribedState == null && waitForStateIfMissing)
            waitRoutine = StartCoroutine(WaitAndBindState());
    }

    void OnDisable()
    {
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        UnbindState();
    }

    void ApplySprites()
    {
        if (keyItemImage != null && keyItemSprite != null)
            keyItemImage.sprite = keyItemSprite;

        if (collectibleImage != null && collectibleSprite != null)
            collectibleImage.sprite = collectibleSprite;
    }

    void HideAllIcons()
    {
        if (keyItemIconRoot != null)
            keyItemIconRoot.SetActive(false);

        if (collectibleIconRoot != null)
            collectibleIconRoot.SetActive(false);
    }

    IEnumerator WaitAndBindState()
    {
        while (subscribedState == null)
        {
            TryBindState();
            yield return null;
        }

        waitRoutine = null;
    }

    void TryBindState()
    {
        Stage1ObjectiveState state = Stage1ObjectiveState.Instance;
        if (state == null) return;
        if (subscribedState == state) return;

        UnbindState();

        subscribedState = state;
        subscribedState.OnKeyItemCollected += HandleKeyItemCollected;
        subscribedState.OnCollectibleCollected += HandleCollectibleCollected;

        RefreshIcons();
    }

    void UnbindState()
    {
        if (subscribedState == null) return;

        subscribedState.OnKeyItemCollected -= HandleKeyItemCollected;
        subscribedState.OnCollectibleCollected -= HandleCollectibleCollected;

        subscribedState = null;
    }

    void RefreshIcons()
    {
        if (subscribedState == null)
            return;

        if (keyItemIconRoot != null)
            keyItemIconRoot.SetActive(subscribedState.hasKeyItem);

        if (collectibleIconRoot != null)
            collectibleIconRoot.SetActive(subscribedState.hasCollectible);
    }

    void HandleKeyItemCollected()
    {
        if (keyItemIconRoot != null)
            keyItemIconRoot.SetActive(true);
    }

    void HandleCollectibleCollected()
    {
        if (collectibleIconRoot != null)
            collectibleIconRoot.SetActive(true);
    }
}