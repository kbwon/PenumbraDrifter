using System.Collections;
using UnityEngine;

public class AssassinationFeedback : MonoBehaviour
{
    [Header("Scale")]
    public Transform visualScaleRoot;

    [Header("Hit Stop")]
    public bool useHitStop = true;
    [Range(0.01f, 0.12f)]
    public float hitStopDuration = 0.055f;
    [Range(0f, 0.2f)]
    public float hitStopTimeScale = 0f;

    [Header("Camera Bump")]
    public CameraImpactBump cameraBump;
    public bool useCameraBump = true;

    [Header("Bullet Time")]
    public bool useBulletTime = true;

    [Header("Vignette")]
    public bool useVignette = true;
    public AssassinationVignetteFeedback vignetteFeedback;

    [Range(0.05f, 1f)]
    public float bulletTimeScale = 0.3f;

    [Range(0.05f, 0.5f)]
    public float bulletTimeDuration = 0.22f;

    [Range(0f, 0.2f)]
    public float bulletTimeRestoreDuration = 0.06f;

    Coroutine bulletTimeRoutine;

    Vector3 baseLocalScale = Vector3.one;
    bool hasBaseScale;
    Coroutine hitStopRoutine;

    Vector3 baseLocalPosition;
    bool hasBaseTransform;

    void Awake()
    {
        CacheBaseScale();

        if (cameraBump == null && GameManager.Instance != null && GameManager.Instance.followCamera != null)
            cameraBump = GameManager.Instance.followCamera.GetComponent<CameraImpactBump>();

        if (vignetteFeedback == null)
            vignetteFeedback = FindFirstObjectByType<AssassinationVignetteFeedback>(FindObjectsInactive.Include);
    }

    public void CacheBaseScale()
    {
        if (visualScaleRoot == null)
            return;

        baseLocalScale = visualScaleRoot.localScale;
        hasBaseScale = true;
    }

    public void SetVisualScale(float uniformScale)
    {
        if (visualScaleRoot == null)
            return;

        if (!hasBaseScale)
            CacheBaseScale();

        visualScaleRoot.localScale = new Vector3(
            baseLocalScale.x * uniformScale,
            baseLocalScale.y * uniformScale,
            baseLocalScale.z * uniformScale
        );
    }

    public void ResetVisualScale()
    {
        if (visualScaleRoot == null)
            return;

        if (!hasBaseScale)
            CacheBaseScale();

        visualScaleRoot.localScale = baseLocalScale;
    }

    public void PlayImpactFeedback()
    {
        if (useHitStop)
            PlayHitStop();

        if (useCameraBump && cameraBump != null)
            cameraBump.PlayBump();

        if (useVignette && vignetteFeedback != null)
            vignetteFeedback.Play();
    }

    void PlayHitStop()
    {
        if (hitStopRoutine != null)
            StopCoroutine(hitStopRoutine);

        hitStopRoutine = StartCoroutine(HitStopRoutine());
    }

    IEnumerator HitStopRoutine()
    {
        float previousTimeScale = Time.timeScale;
        float previousFixedDeltaTime = Time.fixedDeltaTime;

        if (previousTimeScale <= 0f)
            yield break;

        Time.timeScale = hitStopTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime * Mathf.Max(hitStopTimeScale, 0.001f);

        yield return new WaitForSecondsRealtime(hitStopDuration);

        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime;

        hitStopRoutine = null;
    }

    public void PlayBulletTime()
    {
        if (!useBulletTime)
            return;

        if (bulletTimeRoutine != null)
            StopCoroutine(bulletTimeRoutine);

        bulletTimeRoutine = StartCoroutine(BulletTimeRoutine());
    }

    public void CacheBaseTransform()
    {
        if (visualScaleRoot == null)
            return;

        baseLocalPosition = visualScaleRoot.localPosition;
        baseLocalScale = visualScaleRoot.localScale;
        hasBaseTransform = true;
    }

    public void SetVisualPose(float uniformScale, float liftLocalY)
    {
        if (visualScaleRoot == null)
            return;

        if (!hasBaseTransform)
            CacheBaseTransform();

        visualScaleRoot.localScale = new Vector3(
            baseLocalScale.x * uniformScale,
            baseLocalScale.y * uniformScale,
            baseLocalScale.z * uniformScale
        );

        Vector3 pos = baseLocalPosition;
        pos.y += liftLocalY;
        visualScaleRoot.localPosition = pos;
    }

    public void ResetVisualPose()
    {
        if (visualScaleRoot == null)
            return;

        if (!hasBaseTransform)
            CacheBaseTransform();

        visualScaleRoot.localScale = baseLocalScale;
        visualScaleRoot.localPosition = baseLocalPosition;
    }

    IEnumerator BulletTimeRoutine()
    {
        float originalTimeScale = Time.timeScale;
        float originalFixedDeltaTime = Time.fixedDeltaTime;

        if (originalTimeScale <= 0f)
            originalTimeScale = 1f;

        Time.timeScale = bulletTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * Mathf.Max(0.001f, bulletTimeScale);

        yield return new WaitForSecondsRealtime(bulletTimeDuration);

        if (bulletTimeRestoreDuration <= 0f)
        {
            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
            bulletTimeRoutine = null;
            yield break;
        }

        float t = 0f;
        float startScale = Time.timeScale;

        while (t < bulletTimeRestoreDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / bulletTimeRestoreDuration);

            Time.timeScale = Mathf.Lerp(startScale, originalTimeScale, k);
            Time.fixedDeltaTime = originalFixedDeltaTime * Mathf.Max(0.001f, Time.timeScale);

            yield return null;
        }

        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        bulletTimeRoutine = null;
    }

    public void StopBulletTime()
    {
        if (bulletTimeRoutine != null)
        {
            StopCoroutine(bulletTimeRoutine);
            bulletTimeRoutine = null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
    public void StopVisualFeedback()
    {
        if (vignetteFeedback != null)
            vignetteFeedback.StopAndClear();
    }
}