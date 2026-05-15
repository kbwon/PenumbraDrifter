using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class CameraImpactBump : MonoBehaviour
{
    [Header("Bump")]
    public float duration = 0.08f;
    public float magnitude = 0.07f;

    [Tooltip("화면 기준 충격 방향입니다. X=-1이면 왼쪽, X=1이면 오른쪽입니다.")]
    public Vector2 screenDirection = new Vector2(-1f, 0.08f);

    [Tooltip("1이면 한 번 덜컥, 2 이상이면 더 흔들립니다.")]
    public int oscillations = 1;

    Vector3 currentOffset;
    Coroutine bumpRoutine;

    void LateUpdate()
    {
        if (currentOffset.sqrMagnitude > 0.000001f)
            transform.position += currentOffset;
    }

    public void PlayBump()
    {
        if (bumpRoutine != null)
            StopCoroutine(bumpRoutine);

        bumpRoutine = StartCoroutine(BumpRoutine());
    }

    IEnumerator BumpRoutine()
    {
        float t = 0f;

        Vector3 dir =
            transform.right * screenDirection.x +
            transform.up * screenDirection.y;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = -transform.right;

        dir.Normalize();

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));

            float envelope = 1f - p;
            float wave = Mathf.Sin(p * Mathf.PI * 2f * Mathf.Max(1, oscillations));

            currentOffset = dir * (wave * envelope * magnitude);

            yield return null;
        }

        currentOffset = Vector3.zero;
        bumpRoutine = null;
    }
}