using UnityEngine;

public class OccluderFadeTarget : MonoBehaviour
{
    [Header("Fade")]
    [Range(0f, 1f)] public float normalAlpha = 1f;
    [Range(0f, 1f)] public float fadedAlpha = 0.25f;
    public float fadeSpeed = 8f;

    [Header("Renderers")]
    public Renderer[] renderers;

    float targetAlpha = 1f;
    float currentAlpha = 1f;

    MaterialPropertyBlock block;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        block = new MaterialPropertyBlock();
        currentAlpha = normalAlpha;
        targetAlpha = normalAlpha;

        ApplyAlpha(currentAlpha);
    }

    void Update()
    {
        currentAlpha = Mathf.MoveTowards(
            currentAlpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime
        );

        ApplyAlpha(currentAlpha);
    }

    public void SetFaded(bool faded)
    {
        targetAlpha = faded ? fadedAlpha : normalAlpha;
    }

    void ApplyAlpha(float alpha)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (!r) continue;

            r.GetPropertyBlock(block);

            // URP Lit의 기본 색상 프로퍼티
            Color color = Color.white;

            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                color = r.sharedMaterial.GetColor("_BaseColor");
            else if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                color = r.sharedMaterial.GetColor("_Color");

            color.a = alpha;

            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                block.SetColor("_BaseColor", color);

            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                block.SetColor("_Color", color);

            r.SetPropertyBlock(block);
        }
    }
}