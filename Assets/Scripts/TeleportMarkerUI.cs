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

    [Header("Mode")]
    public bool useCustomCursor = false;
    public bool useTeleportMarker = false;

    void Awake()
    {
        if (!teleport && GameManager.Instance != null)
            teleport = GameManager.Instance.teleport;

        if (!shadowCtrl && GameManager.Instance != null)
            shadowCtrl = GameManager.Instance.shadow;

        if (!canvas)
            canvas = GetComponentInParent<Canvas>();

        ApplyDefaultCursorMode();
    }

    void OnEnable()
    {
        ApplyDefaultCursorMode();
    }

    void OnDisable()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (!useCustomCursor && !useTeleportMarker)
        {
            SetCursorVisible(false);
            SetMarkerVisible(false);
            return;
        }

        if (!teleport || !shadowCtrl || !canvas)
        {
            SetCursorVisible(false);
            SetMarkerVisible(false);
            return;
        }

        bool canTeleport = false;

        if (shadowCtrl.IsInShadowMode)
            canTeleport = teleport.TryGetTeleportTarget(out _);

        if (useCustomCursor)
        {
            UpdatePosition(cursorRect);
            SetCursorVisible(!canTeleport);
        }
        else
        {
            SetCursorVisible(false);
        }

        if (useTeleportMarker && canTeleport)
        {
            UpdatePosition(markerRect);
            SetMarkerVisible(true);
        }
        else
        {
            SetMarkerVisible(false);
        }
    }

    void ApplyDefaultCursorMode()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetCursorVisible(false);
        SetMarkerVisible(false);
    }

    void UpdatePosition(RectTransform targetRect)
    {
        if (targetRect == null)
            return;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            targetRect.position = Input.mousePosition;
        }
        else
        {
            RectTransform canvasRect = canvas.transform as RectTransform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                canvas.worldCamera,
                out Vector2 localPos
            );

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