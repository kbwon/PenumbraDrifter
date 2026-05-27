using System.Collections;
using UnityEngine;
using SpriteGlow;

public class BossVulnerableFeedback : MonoBehaviour
{
    [Header("Refs")]
    public BossController boss;
    public Camera targetCamera;

    [Header("Icon")]
    public GameObject iconRoot;
    public Transform iconBillboardRoot;
    public Vector3 iconLocalOffset = new Vector3(0f, 2.4f, 0f);
    public float iconPulseScale = 1.15f;
    public float iconPulseSpeed = 6f;

    [Header("Sprite Glow")]
    public SpriteGlowEffect[] spriteGlowEffects;

    [Tooltip("비어 있으면 자식에서 SpriteGlowEffect를 자동으로 찾습니다.")]
    public bool autoFindSpriteGlowComponents = true;

    int[] originalGlowWidths;
    float[] originalGlowBrightness;
    Color[] originalGlowColors;

    [Header("Camera Zoom")]
    public bool useCameraPulse = true;
    [Range(0.6f, 1f)] public float zoomSizeMultiplier = 0.88f;
    public float zoomInSeconds = 0.08f;
    public float zoomHoldSeconds = 0.08f;
    public float zoomOutSeconds = 0.16f;

    [Header("Debug")]
    public bool debugLog;

    Vector3 iconBaseScale = Vector3.one;
    Coroutine cameraRoutine;
    bool vulnerable;

    void Awake()
    {
        if (boss == null)
            boss = GetComponentInParent<BossController>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (iconRoot != null)
        {
            iconRoot.transform.localPosition = iconLocalOffset;
            iconBaseScale = iconRoot.transform.localScale;
            iconRoot.SetActive(false);
        }

        if (autoFindSpriteGlowComponents && (spriteGlowEffects == null || spriteGlowEffects.Length == 0))
        {
            spriteGlowEffects = GetComponentsInChildren<SpriteGlowEffect>(true);
        }

        CacheSpriteGlowValues();
        SetSpriteGlow(false);
    }

    void OnEnable()
    {
        if (boss != null)
        {
            boss.OnVulnerableChanged += HandleVulnerableChanged;
            HandleVulnerableChanged(boss.IsVulnerable);
        }
    }

    void OnDisable()
    {
        if (boss != null)
            boss.OnVulnerableChanged -= HandleVulnerableChanged;

        SetSpriteGlow(false);

        if (iconRoot != null)
            iconRoot.SetActive(false);
    }

    void Update()
    {
        UpdateIconBillboard();
        UpdateIconPulse();
    }

    void HandleVulnerableChanged(bool value)
    {
        vulnerable = value;

        if (debugLog)
            Debug.Log($"[BossVulnerableFeedback] Vulnerable={value}", this);

        if (iconRoot != null)
        {
            iconRoot.transform.localPosition = iconLocalOffset;
            iconRoot.transform.localScale = iconBaseScale;
            iconRoot.SetActive(value);
        }

        SetSpriteGlow(value);

        if (value && useCameraPulse)
            PlayCameraPulse();
    }

    void CacheSpriteGlowValues()
    {
        if (spriteGlowEffects == null)
            return;

        originalGlowWidths = new int[spriteGlowEffects.Length];
        originalGlowBrightness = new float[spriteGlowEffects.Length];
        originalGlowColors = new Color[spriteGlowEffects.Length];

        for (int i = 0; i < spriteGlowEffects.Length; i++)
        {
            SpriteGlowEffect glow = spriteGlowEffects[i];
            if (glow == null) continue;

            originalGlowWidths[i] = glow.OutlineWidth;
            originalGlowBrightness[i] = glow.GlowBrightness;
            originalGlowColors[i] = glow.GlowColor;
        }
    }

    void SetSpriteGlow(bool active)
    {
        if (spriteGlowEffects == null)
            return;

        for (int i = 0; i < spriteGlowEffects.Length; i++)
        {
            SpriteGlowEffect glow = spriteGlowEffects[i];
            if (glow == null) continue;

            // 핵심:
            // 컴포넌트 자체는 끄지 않습니다.
            // 끄면 SpriteGlowEffect.OnDisable에서 Material을 다시 바꿔버립니다.
            if (!glow.enabled)
                glow.enabled = true;

            int originalWidth = 2;
            float originalBrightness = 2f;
            Color originalColor = Color.white;

            if (originalGlowWidths != null && i < originalGlowWidths.Length)
                originalWidth = originalGlowWidths[i];

            if (originalGlowBrightness != null && i < originalGlowBrightness.Length)
                originalBrightness = originalGlowBrightness[i];

            if (originalGlowColors != null && i < originalGlowColors.Length)
                originalColor = originalGlowColors[i];

            glow.GlowColor = originalColor;
            glow.GlowBrightness = originalBrightness;
            glow.OutlineWidth = active ? originalWidth : 0;
        }
    }

    void UpdateIconBillboard()
    {
        if (!vulnerable) return;
        if (iconBillboardRoot == null) return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        iconBillboardRoot.forward = -targetCamera.transform.forward;
    }

    void UpdateIconPulse()
    {
        if (!vulnerable) return;
        if (iconRoot == null) return;

        float pulse =
            1f + Mathf.Sin(Time.unscaledTime * iconPulseSpeed) * (iconPulseScale - 1f);

        iconRoot.transform.localScale = iconBaseScale * pulse;
    }

    void PlayCameraPulse()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        if (!targetCamera.orthographic)
            return;

        if (cameraRoutine != null)
            StopCoroutine(cameraRoutine);

        cameraRoutine = StartCoroutine(CameraPulseRoutine());
    }

    IEnumerator CameraPulseRoutine()
    {
        float originalSize = targetCamera.orthographicSize;
        float targetSize = originalSize * zoomSizeMultiplier;

        yield return LerpCameraSize(originalSize, targetSize, zoomInSeconds);

        if (zoomHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(zoomHoldSeconds);

        yield return LerpCameraSize(targetCamera.orthographicSize, originalSize, zoomOutSeconds);

        cameraRoutine = null;
    }

    IEnumerator LerpCameraSize(float from, float to, float seconds)
    {
        float t = 0f;
        float duration = Mathf.Max(0.01f, seconds);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);

            // SmoothStep
            u = u * u * (3f - 2f * u);

            targetCamera.orthographicSize = Mathf.Lerp(from, to, u);

            yield return null;
        }

        targetCamera.orthographicSize = to;
    }
}