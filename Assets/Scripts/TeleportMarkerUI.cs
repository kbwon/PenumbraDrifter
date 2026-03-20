using UnityEngine;
using UnityEngine.UI;

public class TeleportMarkerUI : MonoBehaviour
{
    public ShadowTeleport teleport;
    public ShadowInteractController shadowCtrl;

    [Header("Marker")]
    public RectTransform markerRect;
    public Image markerImage;
    public Canvas canvas;

    void Awake()
    {
        if (!teleport && GameManager.Instance != null)
            teleport = GameManager.Instance.teleport;

        if (!shadowCtrl && GameManager.Instance != null)
            shadowCtrl = GameManager.Instance.shadow;

        if (!markerRect && markerImage)
            markerRect = markerImage.rectTransform;

        if (!markerImage && markerRect)
            markerImage = markerRect.GetComponent<Image>();

        if (!canvas)
            canvas = GetComponentInParent<Canvas>();

        SetVisible(false);
    }

    void Update()
    {
        if (!teleport || !shadowCtrl || !canvas || !markerRect || !markerImage)
        {
            SetVisible(false);
            return;
        }

        if (!shadowCtrl.IsInShadowMode)
        {
            SetVisible(false);
            return;
        }

        bool canTeleport = teleport.TryGetTeleportTarget(out _);
        if (!canTeleport)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            markerRect.position = Input.mousePosition;
        }
        else
        {
            RectTransform canvasRect = canvas.transform as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, canvas.worldCamera, out Vector2 localPos);
            markerRect.localPosition = localPos;
        }
    }

    void SetVisible(bool visible)
    {
        if (markerImage != null)
            markerImage.enabled = visible;
    }
}
