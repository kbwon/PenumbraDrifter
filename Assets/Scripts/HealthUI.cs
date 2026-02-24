using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public PlayerHealth health;

    [Header("Option A: Assign existing pips")]
    public Image[] pips; // 빨간 원들(이미 배치된 것)

    [Header("Option B: Auto build (for later)")]
    public bool autoBuild = false;
    public Image pipPrefab;          // 빨간 원 Image 프리팹
    public RectTransform container;  // 원들이 들어갈 부모(가로 배치 등)

    void Start()
    {
        if (autoBuild)
        {
            BuildOrResizePips();
        }
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (!health) return;

        int max = health.maxPips;
        int cur = health.currentPips;

        // autoBuild 켰으면 maxPips가 바뀔 때도 대응 가능
        if (autoBuild && (pips == null || pips.Length != max))
            BuildOrResizePips();

        if (pips == null) return;

        // 오른쪽부터 꺼지게: 인덱스가 오른쪽에 가까울수록 꺼질 대상이 되도록
        // 여기서는 "배열의 끝이 오른쪽"이라고 가정합니다.
        // (즉, pips[0]..pips[n-1]이 왼→오 순서)
        for (int i = 0; i < pips.Length; i++)
        {
            if (!pips[i]) continue;
            pips[i].enabled = (i < cur); // cur=3이면 0,1,2 켜지고 3,4 꺼짐 → 오른쪽부터 꺼짐
        }
    }

    void BuildOrResizePips()
    {
        if (!health || !pipPrefab || !container) return;

        // 기존 자식 정리(간단 버전). 나중에 풀링으로 바꿔도 됨.
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
