using UnityEngine;

[ExecuteAlways]
public class FloorColorController : MonoBehaviour
{
    [System.Serializable]
    public class MaterialColorTarget
    {
        [Tooltip("예: 콘크리트, 창틀, 층 구분 콘크리트")]
        public string memo;

        public int materialIndex;
        public Color color = Color.white;
    }

    [Header("색을 바꿀 머티리얼 슬롯")]
    public MaterialColorTarget[] targets;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock block;

    private void OnEnable()
    {
        CacheRenderer();
        ApplyColors();
    }

    private void OnValidate()
    {
        CacheRenderer();
        ApplyColors();
    }

    private void Reset()
    {
        CacheRenderer();

        if (meshRenderer == null) return;

        int count = meshRenderer.sharedMaterials.Length;
        targets = new MaterialColorTarget[count];

        for (int i = 0; i < count; i++)
        {
            targets[i] = new MaterialColorTarget
            {
                memo = "",
                materialIndex = i,
                color = Color.white
            };
        }

        ApplyColors();
    }

    private void CacheRenderer()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
    }

    [ContextMenu("색상 적용")]
    public void ApplyColors()
    {
        if (targets == null) return;

        CacheRenderer();

        if (meshRenderer == null) return;

        if (block == null)
            block = new MaterialPropertyBlock();

        foreach (var target in targets)
        {
            if (target == null) continue;

            if (target.materialIndex < 0 || target.materialIndex >= meshRenderer.sharedMaterials.Length)
                continue;

            meshRenderer.GetPropertyBlock(block, target.materialIndex);
            block.SetColor(BaseColor, target.color);
            meshRenderer.SetPropertyBlock(block, target.materialIndex);
        }
    }
}