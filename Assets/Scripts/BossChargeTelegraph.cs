using System.Collections;
using UnityEngine;

public class BossChargeTelegraph : MonoBehaviour
{
    [Header("Refs")]
    public Transform baseRect;
    public Transform fillRect;

    [Header("Ground Snap")]
    public LayerMask groundMask;
    public float rayUp = 3f;
    public float rayDown = 10f;
    public float yOffset = 0.05f;

    [Header("Visual")]
    public bool setChildRotationToGround = true;
    public float baseHeight = 0.02f;
    public float fillHeight = 0.04f;

    Coroutine fillRoutine;
    Coroutine hideRoutine;

    float currentLength;
    float currentWidth;

    void Awake()
    {
        SetupChildRotation();
        SetVisible(false);
    }

    void OnDisable()
    {
        fillRoutine = null;
        hideRoutine = null;
    }

    void SetupChildRotation()
    {
        if (!setChildRotationToGround)
            return;

        if (baseRect != null)
            baseRect.localRotation = Quaternion.Euler(90f, 0f, 0f);

        if (fillRect != null)
            fillRect.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void Begin(Vector3 startPos, Vector3 dir, float length, float width, float seconds)
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        // 루트 오브젝트는 비활성화하지 않습니다.
        // 자식 Sprite만 켜고 끕니다.
        SetVisible(true);
        SetupChildRotation();

        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector3.forward;

        dir.Normalize();

        currentLength = Mathf.Max(0.01f, length);
        currentWidth = Mathf.Max(0.01f, width);

        Vector3 groundStart = SnapToGround(startPos);

        // 직사각형 중심을 보스 앞쪽으로 보냅니다.
        transform.position = groundStart + dir * (currentLength * 0.5f);
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        if (baseRect != null)
        {
            baseRect.localPosition = new Vector3(0f, baseHeight, 0f);
            baseRect.localScale = new Vector3(currentWidth, currentLength, 1f);
        }

        if (fillRect != null)
        {
            fillRect.localPosition = new Vector3(0f, fillHeight, -currentLength * 0.5f);
            fillRect.localScale = new Vector3(currentWidth, 0f, 1f);
        }

        fillRoutine = StartCoroutine(FillRoutine(seconds));
    }

    IEnumerator FillRoutine(float seconds)
    {
        float t = 0f;
        float duration = Mathf.Max(0.01f, seconds);

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            ApplyFill(u);

            yield return null;
        }

        ApplyFill(1f);
        fillRoutine = null;
    }

    void ApplyFill(float u)
    {
        if (fillRect == null)
            return;

        u = Mathf.Clamp01(u);

        float filledLength = currentLength * u;

        // Sprite pivot이 중앙이어도 보스 발밑에서 앞쪽으로 차오르게 보정합니다.
        fillRect.localScale = new Vector3(currentWidth, filledLength, 1f);
        fillRect.localPosition = new Vector3(
            0f,
            fillHeight,
            -currentLength * 0.5f + filledLength * 0.5f
        );
    }

    public void CompleteAndHide(float hideDelay = 0.05f)
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        ApplyFill(1f);
        hideRoutine = StartCoroutine(HideAfter(hideDelay));
    }

    public void HideImmediately()
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        SetVisible(false);
    }

    IEnumerator HideAfter(float seconds)
    {
        if (seconds > 0f)
            yield return new WaitForSeconds(seconds);

        SetVisible(false);
        hideRoutine = null;
    }

    Vector3 SnapToGround(Vector3 position)
    {
        if (groundMask.value == 0)
        {
            position.y += yOffset;
            return position;
        }

        Vector3 origin = position + Vector3.up * rayUp;

        if (Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            rayUp + rayDown,
            groundMask,
            QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * yOffset;
        }

        position.y += yOffset;
        return position;
    }

    void SetVisible(bool visible)
    {
        if (baseRect != null)
            baseRect.gameObject.SetActive(visible);

        if (fillRect != null)
            fillRect.gameObject.SetActive(visible);
    }
}