using UnityEngine;
using UnityEngine.UI;

public class TeleportUI : MonoBehaviour
{
    public ShadowTeleport tp;
    public ShadowInteractController shadowCtrl;
    public Image lamp;

    [Header("Visual")]
    public float onAlpha = 1f;
    public float offAlpha = 0.25f;

    void Awake()
    {
        if (!lamp)
            lamp = GetComponent<Image>();

        if (!tp && GameManager.Instance != null)
            tp = GameManager.Instance.teleport;

        if (!shadowCtrl && tp)
            shadowCtrl = tp.shadowCtrl;

        if (!shadowCtrl && GameManager.Instance != null)
            shadowCtrl = GameManager.Instance.shadow;
    }

    void Update()
    {
        if (!lamp || !tp || !shadowCtrl) return;

        // 그림자 모드이며 순간이동이 준비되었을 때만 밝게 표시한다.
        bool on = shadowCtrl.IsInShadowMode && tp.IsReady && shadowCtrl.Gauge01 > 0f;

        Color color = lamp.color;
        color.a = on ? onAlpha : offAlpha;
        lamp.color = color;
    }
}
