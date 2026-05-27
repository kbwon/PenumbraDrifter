using System.Collections;
using UnityEngine;

public class BossGroundSlamTelegraph : MonoBehaviour
{
    [Header("Refs")]
    public Transform outerCircle;
    public Transform fillCircle;

    [Header("Ground Snap")]
    public LayerMask groundMask;
    public float rayUp = 3f;
    public float rayDown = 10f;
    public float yOffset = 0.04f;

    [Header("Visual")]
    public bool setChildRotationToGround = true;
    public float outerHeight = 0.02f;
    public float fillHeight = 0.04f;

    Coroutine fillRoutine;
    Coroutine hideRoutine;

    float currentDiameter;

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

        if (outerCircle != null)
            outerCircle.localRotation = Quaternion.Euler(90f, 0f, 0f);

        if (fillCircle != null)
            fillCircle.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    public void Begin(Vector3 position, float radius, float seconds)
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

        transform.position = SnapToGround(position);
        transform.rotation = Quaternion.identity;

        currentDiameter = Mathf.Max(0.01f, radius * 2f);

        if (outerCircle != null)
        {
            outerCircle.localPosition = new Vector3(0f, outerHeight, 0f);
            outerCircle.localScale = new Vector3(currentDiameter, currentDiameter, 1f);
        }

        if (fillCircle != null)
        {
            fillCircle.localPosition = new Vector3(0f, fillHeight, 0f);
            fillCircle.localScale = Vector3.zero;
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
        if (fillCircle == null)
            return;

        u = Mathf.Clamp01(u);

        float size = currentDiameter * u;
        fillCircle.localScale = new Vector3(size, size, 1f);
        fillCircle.localPosition = new Vector3(0f, fillHeight, 0f);
    }

    public void CompleteAndHide(float hideDelay = 0.08f)
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
        if (outerCircle != null)
            outerCircle.gameObject.SetActive(visible);

        if (fillCircle != null)
            fillCircle.gameObject.SetActive(visible);
    }
}