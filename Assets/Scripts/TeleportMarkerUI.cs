using UnityEngine;
using UnityEngine.UI;

public class TeleportMarkerUI : MonoBehaviour
{
    public ShadowTeleport teleport;
    public ShadowInteractController shadowCtrl;

    [Header("Normal Cursor")]
    public RectTransform cursorRect;
    public Image cursorImage;

    [Header("Teleport Marker")]
    public RectTransform markerRect;
    public Image markerImage;

    [Header("Canvas")]
    public Canvas canvas;

    void Awake()
    {
        if (!teleport && GameManager.Instance != null)
            teleport = GameManager.Instance.teleport;

        if (!shadowCtrl && GameManager.Instance != null)
            shadowCtrl = GameManager.Instance.shadow;

        if (!canvas)
            canvas = GetComponentInParent<Canvas>();

        SetCursorVisible(true);
        SetMarkerVisible(false);
    }

    void OnEnable()
    {
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.visible = true;
    }

    void Update()
    {
        if (!teleport || !shadowCtrl || !canvas)
        {
            SetCursorVisible(false);
            SetMarkerVisible(false);
            return;
        }

        UpdatePosition(cursorRect);
        UpdatePosition(markerRect);

        bool canTeleport = false;

        if (shadowCtrl.IsInShadowMode)
            canTeleport = teleport.TryGetTeleportTarget(out _);

        SetCursorVisible(!canTeleport);
        SetMarkerVisible(canTeleport);
    }

    void UpdatePosition(RectTransform targetRect)
    {
        if (targetRect == null) return;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            targetRect.position = Input.mousePosition;
        }
        else
        {
            RectTransform canvasRect = canvas.transform as RectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, canvas.worldCamera, out Vector2 localPos);
            targetRect.localPosition = localPos;
        }
    }

    void SetCursorVisible(bool visible)
    {
        if (cursorImage != null)
            cursorImage.enabled = visible;
    }

    void SetMarkerVisible(bool visible)
    {
        if (markerImage != null)
            markerImage.enabled = visible;
    }
}