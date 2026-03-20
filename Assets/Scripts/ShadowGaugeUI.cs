using UnityEngine;
using UnityEngine.UI;

public class ShadowGaugeUI : MonoBehaviour
{
    public ShadowInteractController shadowCtrl;
    public Slider slider;

    void Awake()
    {
        if (!slider)
            slider = GetComponent<Slider>();

        TryBindShadow();
    }

    void OnEnable()
    {
        TryBindShadow();

        if (shadowCtrl != null)
            shadowCtrl.OnGaugeChanged += HandleGaugeChanged;
    }

    void Start()
    {
        RefreshNow();
    }

    void OnDisable()
    {
        if (shadowCtrl != null)
            shadowCtrl.OnGaugeChanged -= HandleGaugeChanged;
    }

    void TryBindShadow()
    {
        if (shadowCtrl == null && GameManager.Instance != null)
            shadowCtrl = GameManager.Instance.shadow;
    }

    void HandleGaugeChanged(float value)
    {
        if (!slider) return;
        slider.value = value;
    }

    void RefreshNow()
    {
        if (!shadowCtrl || !slider) return;
        slider.value = shadowCtrl.Gauge01;
    }
}
