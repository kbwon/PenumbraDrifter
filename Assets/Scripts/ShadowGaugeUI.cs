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
    }

    void OnEnable()
    {
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
