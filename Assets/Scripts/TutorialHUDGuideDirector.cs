using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class TutorialHUDGuideStep
{
    public string stepName;

    [Header("Target")]
    public RectTransform target;

    [Header("Text")]
    public string title;

    [TextArea(2, 5)]
    public string body;

    [Header("Layout")]
    public Vector2 textOffset = new Vector2(300f, 0f);
    public Vector2 focusPadding = new Vector2(24f, 16f);

    [Header("Temporary UI")]
    public GameObject[] temporaryVisibleObjects;
}

public class TutorialHUDGuideDirector : MonoBehaviour
{
    [Header("Refs")]
    public PlayerController player;
    public ShadowInteractController shadow;

    [Header("Overlay")]
    public CanvasGroup rootGroup;
    public GameObject rootObject;

    [Header("Focus")]
    public RectTransform focusFrame;

    [Header("Text Panel")]
    public RectTransform textPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI continueText;

    [Header("Steps")]
    public TutorialHUDGuideStep[] steps;

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.Space;
    public bool allowMouseClick = true;

    [Header("Options")]
    public bool lockPlayer = true;
    public bool forceExitShadowMode = true;
    public bool playOnStart = false;
    public float inputDelay = 0.15f;

    [Header("Canvas Space")]
    public RectTransform overlayRect;
    public Canvas rootCanvas;

    [Header("Panel Layout")]
    public Vector2 textPanelSize = new Vector2(420f, 160f);
    public Vector2 screenMargin = new Vector2(24f, 24f);

    [Header("Text Panel Position")]
    public bool moveTextPanelWithTarget = false;

    bool playing;

    readonly List<GameObject> tempObjects = new List<GameObject>();
    readonly List<bool> tempObjectOriginalStates = new List<bool>();

    void Awake()
    {
        ResolveRefs();
        HideImmediate();
    }

    IEnumerator Start()
    {
        yield return null;

        if (playOnStart)
            PlayGuide();
    }

    void ResolveRefs()
    {
        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.player;

        if (shadow == null && GameManager.Instance != null)
            shadow = GameManager.Instance.shadow;

        if (rootObject == null && rootGroup != null)
            rootObject = rootGroup.gameObject;

        if (overlayRect == null && rootGroup != null)
            overlayRect = rootGroup.GetComponent<RectTransform>();

        if (rootCanvas == null && rootGroup != null)
            rootCanvas = rootGroup.GetComponentInParent<Canvas>();
    }

    public void PlayGuide()
    {
        if (playing) return;
        StartCoroutine(GuideRoutine());
    }

    IEnumerator GuideRoutine()
    {
        playing = true;

        ResolveRefs();

        if (forceExitShadowMode && shadow != null)
        {
            shadow.ForceExitShadowMode();
            shadow.ClearSurfaceAnchor();
            shadow.ClearMovingShadowHost();
        }

        if (lockPlayer && player != null)
            player.SetInputLocked(true);

        ShowOverlay(true);

        if (steps != null)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                TutorialHUDGuideStep step = steps[i];
                if (step == null) continue;

                ShowStep(step);
                yield return WaitForAdvanceInput();
                RestoreTemporaryObjects();
            }
        }

        ShowOverlay(false);

        if (lockPlayer && player != null)
            player.SetInputLocked(false);

        playing = false;
    }

    void ShowStep(TutorialHUDGuideStep step)
    {
        RestoreTemporaryObjects();
        ApplyTemporaryObjects(step);

        if (titleText != null)
            titleText.text = step.title;

        if (bodyText != null)
            bodyText.text = step.body;

        if (continueText != null)
            continueText.text = "Press Space / Click to continue";

        if (textPanel != null)
        {
            textPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textPanelSize.x);
            textPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textPanelSize.y);
        }

        if (step.target != null)
        {
            Vector2 targetCenter;
            Vector2 targetSize;

            GetTargetRectInOverlay(step.target, out targetCenter, out targetSize);

            if (focusFrame != null)
            {
                focusFrame.gameObject.SetActive(true);
                focusFrame.anchoredPosition = targetCenter;
                focusFrame.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Horizontal,
                    targetSize.x + step.focusPadding.x
                );
                focusFrame.SetSizeWithCurrentAnchors(
                    RectTransform.Axis.Vertical,
                    targetSize.y + step.focusPadding.y
                );
            }

            if (textPanel != null)
            {
                textPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textPanelSize.x);
                textPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textPanelSize.y);

                if (moveTextPanelWithTarget && step.target != null)
                {
                    Vector2 desiredPos = targetCenter + step.textOffset;
                    textPanel.anchoredPosition = ClampPanelPosition(desiredPos, textPanelSize);
                }
            }
        }
        else
        {
            if (focusFrame != null)
                focusFrame.gameObject.SetActive(false);

            if (textPanel != null)
                textPanel.anchoredPosition = Vector2.zero;
        }
    }

    void GetTargetRectInOverlay(RectTransform target, out Vector2 center, out Vector2 size)
    {
        center = Vector2.zero;
        size = Vector2.zero;

        if (target == null || overlayRect == null)
            return;

        Camera uiCamera = GetUICamera();

        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < 4; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[i]);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                overlayRect,
                screenPoint,
                uiCamera,
                out Vector2 localPoint
            );

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        center = (min + max) * 0.5f;
        size = max - min;
    }

    Vector2 ClampPanelPosition(Vector2 desiredPos, Vector2 panelSize)
    {
        if (overlayRect == null)
            return desiredPos;

        Rect rect = overlayRect.rect;
        Vector2 half = panelSize * 0.5f;

        desiredPos.x = Mathf.Clamp(
            desiredPos.x,
            rect.xMin + half.x + screenMargin.x,
            rect.xMax - half.x - screenMargin.x
        );

        desiredPos.y = Mathf.Clamp(
            desiredPos.y,
            rect.yMin + half.y + screenMargin.y,
            rect.yMax - half.y - screenMargin.y
        );

        return desiredPos;
    }

    Camera GetUICamera()
    {
        if (rootCanvas == null)
            return null;

        if (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return rootCanvas.worldCamera;
    }

    void ApplyTemporaryObjects(TutorialHUDGuideStep step)
    {
        if (step.temporaryVisibleObjects == null)
            return;

        for (int i = 0; i < step.temporaryVisibleObjects.Length; i++)
        {
            GameObject obj = step.temporaryVisibleObjects[i];
            if (obj == null) continue;

            tempObjects.Add(obj);
            tempObjectOriginalStates.Add(obj.activeSelf);

            obj.SetActive(true);
        }
    }

    void RestoreTemporaryObjects()
    {
        for (int i = 0; i < tempObjects.Count; i++)
        {
            if (tempObjects[i] != null)
                tempObjects[i].SetActive(tempObjectOriginalStates[i]);
        }

        tempObjects.Clear();
        tempObjectOriginalStates.Clear();
    }

    IEnumerator WaitForAdvanceInput()
    {
        float t = 0f;

        while (t < inputDelay)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        while (true)
        {
            if (Input.GetKeyDown(advanceKey))
                yield break;

            if (Input.GetKeyDown(KeyCode.Return))
                yield break;

            if (allowMouseClick && Input.GetMouseButtonDown(0))
                yield break;

            yield return null;
        }
    }

    void ShowOverlay(bool visible)
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (rootGroup != null)
        {
            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
        }

        if (!visible && rootObject != null)
            rootObject.SetActive(false);
    }

    void HideImmediate()
    {
        RestoreTemporaryObjects();

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        if (rootObject != null)
            rootObject.SetActive(false);
    }

    [ContextMenu("TEST/Play HUD Guide")]
    void TestPlayGuide()
    {
        PlayGuide();
    }
}