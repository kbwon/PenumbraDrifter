using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    public CanvasGroup rootGroup;
    public GameObject rootObject;
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;
    public TMP_Text continueText;
    public Image portraitImage;

    [Header("Portrait")]
    public Sprite defaultPortrait;
    public bool hidePortraitIfMissing = true;

    [Header("Typing")]
    public bool useTypewriter = true;
    public float charsPerSecond = 45f;

    [Header("Typing Audio")]
    public bool playTypingSound = true;
    public int charsPerTypingSound = 2;

    [Header("Input")]
    public KeyCode continueKey = KeyCode.Space;
    public KeyCode alternateContinueKey = KeyCode.Return;
    public int continueMouseButton = 0;

    [Header("Behavior")]
    public bool hideOnAwake = true;

    public bool IsPlaying { get; private set; }

    PlayerController cachedPlayer;
    ShadowInteractController cachedShadow;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (rootObject == null && rootGroup != null)
            rootObject = rootGroup.gameObject;

        if (hideOnAwake)
            HideImmediate();
    }

    void ResolveRefs()
    {
        if (cachedPlayer == null)
        {
            if (GameManager.Instance != null)
                cachedPlayer = GameManager.Instance.player;

            if (cachedPlayer == null)
                cachedPlayer = FindFirstObjectByType<PlayerController>();
        }

        if (cachedShadow == null)
        {
            if (GameManager.Instance != null)
                cachedShadow = GameManager.Instance.shadow;

            if (cachedShadow == null)
                cachedShadow = FindFirstObjectByType<ShadowInteractController>();
        }
    }

    public IEnumerator Show(
     DialogueLine[] lines,
     bool lockPlayer = true,
     bool forceExitShadow = true,
     bool pauseGame = false)
    {
        if (lines == null || lines.Length == 0)
            yield break;

        while (IsPlaying)
            yield return null;

        IsPlaying = true;
        SkipAllRequested = false;

        ResolveRefs();

        bool pausedByThisDialogue = false;

        if (forceExitShadow && cachedShadow != null)
        {
            cachedShadow.ForceExitShadowMode();
            cachedShadow.ClearSurfaceAnchor();
            cachedShadow.ClearMovingShadowHost();
        }

        if (lockPlayer && cachedPlayer != null)
            cachedPlayer.SetInputLocked(true);

        if (pauseGame && GameManager.Instance != null && !GameManager.Instance.IsPaused)
        {
            GameManager.Instance.SetPaused(true);
            pausedByThisDialogue = true;
        }

        ShowRoot();

        for (int i = 0; i < lines.Length; i++)
        {
            if (SkipAllRequested)
                break;

            DialogueLine line = lines[i];
            if (line == null)
                continue;

            yield return ShowLineRoutine(line);

            if (SkipAllRequested)
                break;
        }

        HideImmediate();

        if (pausedByThisDialogue && GameManager.Instance != null)
            GameManager.Instance.SetPaused(false);

        if (lockPlayer && cachedPlayer != null)
            cachedPlayer.SetInputLocked(false);

        SkipAllRequested = false;
        IsPlaying = false;
    }

    public IEnumerator ShowSingle(
        string speakerName,
        string text,
        bool lockPlayer = true,
        bool forceExitShadow = true,
        bool pauseGame = false)
    {
        DialogueLine[] lines =
        {
            new DialogueLine(speakerName, text)
        };

        yield return Show(lines, lockPlayer, forceExitShadow, pauseGame);
    }

    IEnumerator ShowLineRoutine(DialogueLine line)
    {
        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        ApplyPortrait(line.portrait);

        if (dialogueText == null)
            yield break;

        string fullText = line.text ?? string.Empty;
        dialogueText.text = string.Empty;

        if (continueText != null)
            continueText.gameObject.SetActive(false);

        if (useTypewriter && charsPerSecond > 0f)
        {
            float interval = 1f / charsPerSecond;
            float timer = 0f;
            int index = 0;

            while (index < fullText.Length)
            {
                if (SkipAllRequested)
                    yield break;

                if (WasContinuePressed())
                {
                    dialogueText.text = fullText;
                    index = fullText.Length;
                    break;
                }

                timer += Time.unscaledDeltaTime;

                while (timer >= interval && index < fullText.Length)
                {
                    timer -= interval;
                    index++;
                    dialogueText.text = fullText.Substring(0, index);

                    if (playTypingSound && UIAudioManager.Instance != null)
                    {
                        char typedChar = fullText[index - 1];

                        if (!char.IsWhiteSpace(typedChar) &&
                            charsPerTypingSound > 0 &&
                            index % charsPerTypingSound == 0)
                        {
                            UIAudioManager.Instance.PlayDialogueTyping();
                        }
                    }
                }

                yield return null;
            }
        }
        else
        {
            dialogueText.text = fullText;
        }

        if (continueText != null)
            continueText.gameObject.SetActive(true);

        // 타이핑 스킵 입력이 같은 프레임에 바로 다음 대사 넘김으로 처리되지 않게 한 프레임 대기합니다.
        yield return null;

        while (!WasContinuePressed())
        {
            if (SkipAllRequested)
                yield break;

            yield return null;
        }

        yield return null;
    }

    void ApplyPortrait(Sprite portrait)
    {
        if (portraitImage == null) return;

        Sprite sprite = portrait != null ? portrait : defaultPortrait;
        portraitImage.sprite = sprite;

        if (hidePortraitIfMissing)
            portraitImage.gameObject.SetActive(sprite != null);
        else
            portraitImage.gameObject.SetActive(true);
    }

    bool WasContinuePressed()
    {
        if (Input.GetKeyDown(continueKey)) return true;
        if (Input.GetKeyDown(alternateContinueKey)) return true;
        if (continueMouseButton >= 0 && Input.GetMouseButtonDown(continueMouseButton)) return true;
        return false;
    }

    void ShowRoot()
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (rootGroup != null)
        {
            rootGroup.alpha = 1f;
            rootGroup.interactable = true;
            rootGroup.blocksRaycasts = true;
        }
    }

    public void HideImmediate()
    {
        if (dialogueText != null)
            dialogueText.text = string.Empty;

        if (continueText != null)
            continueText.gameObject.SetActive(false);

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        if (rootObject != null)
            rootObject.SetActive(false);
    }

    public bool SkipAllRequested { get; private set; }

    public void RequestSkipAll()
    {
        SkipAllRequested = true;
    }

    public void ClearSkipRequest()
    {
        SkipAllRequested = false;
    }
}
