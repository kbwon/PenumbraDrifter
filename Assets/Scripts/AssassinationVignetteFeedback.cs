using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AssassinationVignetteFeedback : MonoBehaviour
{
    [Header("Refs")]
    public RawImage vignetteImage;
    public CanvasGroup canvasGroup;

    [Header("Shape")]
    [Tooltip("값이 작을수록 화면 중앙까지 더 어두워집니다.")]
    [Range(0f, 1f)] public float innerRadius = 0.35f;

    [Tooltip("값이 클수록 가장자리로 갈수록 부드럽게 어두워집니다.")]
    [Range(0f, 1f)] public float outerRadius = 0.9f;

    public int textureSize = 256;

    [Header("Timing")]
    [Range(0f, 1f)] public float maxAlpha = 0.38f;
    public float fadeInSeconds = 0.035f;
    public float holdSeconds = 0.035f;
    public float fadeOutSeconds = 0.14f;

    Coroutine routine;

    void Awake()
    {
        if (vignetteImage == null)
            vignetteImage = GetComponentInChildren<RawImage>(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (vignetteImage != null)
        {
            vignetteImage.raycastTarget = false;
            vignetteImage.color = Color.white;
            vignetteImage.texture = GenerateVignetteTexture();
        }
    }

    public void Play()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(PlayRoutine());
    }

    public void StopAndClear()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    IEnumerator PlayRoutine()
    {
        yield return FadeAlpha(0f, maxAlpha, fadeInSeconds);

        if (holdSeconds > 0f)
            yield return new WaitForSecondsRealtime(holdSeconds);

        yield return FadeAlpha(maxAlpha, 0f, fadeOutSeconds);

        routine = null;
    }

    IEnumerator FadeAlpha(float from, float to, float seconds)
    {
        if (canvasGroup == null)
            yield break;

        if (seconds <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float t = 0f;

        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);

            // 빠르게 들어오고 부드럽게 빠지는 느낌
            k = k * k * (3f - 2f * k);

            canvasGroup.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    Texture2D GenerateVignetteTexture()
    {
        int size = Mathf.Max(32, textureSize);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDist = center.magnitude;

        float inner = innerRadius * maxDist;
        float outer = Mathf.Max(inner + 0.001f, outerRadius * maxDist);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);

                float a = Mathf.InverseLerp(inner, outer, dist);
                a = Mathf.Clamp01(a);
                a = a * a * (3f - 2f * a);

                tex.SetPixel(x, y, new Color(0f, 0f, 0f, a));
            }
        }

        tex.Apply();
        return tex;
    }
}