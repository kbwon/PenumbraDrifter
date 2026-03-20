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

    void Awake()
    {
        TryBindHealth();
    }

    void OnEnable()
    {
        TryBindHealth();

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

    void TryBindHealth()
    {
        if (health == null && GameManager.Instance != null)
            health = GameManager.Instance.health;
    }

    void HandleHealthChanged(int current, int max)
    {
        if (autoBuild && (pips == null || pips.Length != max))
            BuildOrResizePips();

        Refresh();
    }

    void Refresh()
    {
        if (health == null) return;
        if (pips == null) return;

        int current = health.currentPips;
        int max = health.maxPips;

        if (autoBuild && pips.Length != max)
            BuildOrResizePips();

        for (int i = 0; i < pips.Length; i++)
        {
            if (pips[i] == null) continue;
            pips[i].enabled = i < current;
        }
    }

    // 최대 체력이 바뀌면 pip 개수를 다시 만든다.
    void BuildOrResizePips()
    {
        if (health == null || pipPrefab == null || container == null) return;

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
