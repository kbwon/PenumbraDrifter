using UnityEngine;
using UnityEngine.UI;

public class TeleportMarkerUI : MonoBehaviour
{
    public ShadowTeleport teleport;
    public ShadowInteractController shadowCtrl;

    [Header("Marker (Image)")]
    public RectTransform markerRect;  // 원 이미지 RectTransform
    public Image markerImage;         // ✅ enabled로 on/off (SetActive 금지)

    public Canvas canvas;

    void Awake()
    {
        if (!markerRect && markerImage) markerRect = markerImage.rectTransform;
        if (!markerImage && markerRect) markerImage = markerRect.GetComponent<Image>();
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        SetVisible(false);
    }

    void Update()
    {
        if (!teleport || !shadowCtrl || !canvas || !markerRect || !markerImage)
        {
            SetVisible(false);
            return;
        }

        // 그림자 모드가 아니면 숨김
        if (!shadowCtrl.IsInShadowMode)
        {
            SetVisible(false);
            return;
        }

        // 순간이동 가능 지점인지 검사
        bool ok = teleport.TryGetTeleportTarget(out _);

        // 디버그 확인용
        // Debug.Log("Teleport hover ok=" + ok);

        if (!ok)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        // 커서 위치로 이동
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

    void SetVisible(bool v)
    {
        // ✅ GameObject를 끄지 말고 Image만 끈다
        markerImage.enabled = v;
    }
}
