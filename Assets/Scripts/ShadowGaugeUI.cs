using UnityEngine;
using UnityEngine.UI;

public class ShadowGaugeUI : MonoBehaviour
{
    public ShadowInteractController shadowCtrl;
    public Slider slider;

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
    }

    void Update()
    {
        if (!shadowCtrl || !slider) return;
        slider.value = shadowCtrl.Gauge01;
    }
}
