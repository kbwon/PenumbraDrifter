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
        if (!lamp) lamp = GetComponent<Image>();
        if (!shadowCtrl && tp) shadowCtrl = tp.shadowCtrl;
    }
    void Update()
    {
        if (!lamp || !tp || !shadowCtrl) return;

        // "그림자 속 + 쿨다운 끝"일 때만 불 ON
        bool on = shadowCtrl.IsInShadowMode && tp.IsReady && shadowCtrl.Gauge01 > 0f;

        var c = lamp.color;
        c.a = on ? onAlpha : offAlpha;
        lamp.color = c;
    }
}
