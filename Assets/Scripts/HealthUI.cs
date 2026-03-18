using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public PlayerHealth health;

    [Header("Assign")]
    public Image[] pips;

    [Header("Auto Build")]
    public bool autoBuild = false;
    public Image pipPrefab;
    public RectTransform container;

    void OnEnable()
    {
        if (health != null)
            health.OnHealthChanged += HandleHealthChanged;
    }

    void Start()
    {
        if (autoBuild)
            BuildOrResizePips();

        Refresh();
    }

    void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= HandleHealthChanged;
    }

    void HandleHealthChanged(int current, int max)
    {
        if (autoBuild && (pips == null || pips.Length != max))
            BuildOrResizePips();

        Refresh();
    }

    void Refresh()
    {
        if (!health) return;
        if (pips == null) return;

        int current = health.currentPips;
        int max = health.maxPips;

        if (autoBuild && pips.Length != max)
            BuildOrResizePips();

        for (int i = 0; i < pips.Length; i++)
        {
            if (!pips[i]) continue;
            pips[i].enabled = i < current;
        }
    }

    void BuildOrResizePips()
    {
        if (!health || !pipPrefab || !container) return;

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        pips = new Image[health.maxPips];

        for (int i = 0; i < health.maxPips; i++)
        {
            Image pip = Instantiate(pipPrefab, container);
            pip.name = $"HP_{i + 1}";
            pips[i] = pip;
        }
    }
}
