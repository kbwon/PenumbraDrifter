using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUIController : MonoBehaviour
{
    public static TutorialUIController Instance { get; private set; }

    [Header("Refs")]
    public PlayerController player;

    [Header("Popup")]
    public GameObject popupRoot;
    public TMP_Text popupTitleText;
    public TMP_Text popupBodyText;
    public Image popupIconImage;
    public Image popupKeyImageA;
    public Image popupKeyImageB;
    public Button confirmButton;

    [Header("Mission")]
    public GameObject missionRoot;
    public TMP_Text missionText;

    [Header("Hint")]
    public GameObject hintRoot;
    public TMP_Text hintText;
    public Image hintIconImage;
    public Image hintKeyImage;

    Coroutine missionHideRoutine;
    Object currentHintOwner;
    bool popupLockedInput;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!player && GameManager.Instance != null)
            player = GameManager.Instance.player;

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(HidePopup);
            confirmButton.onClick.AddListener(HidePopup);
        }

        if (popupRoot) popupRoot.SetActive(false);
        if (missionRoot) missionRoot.SetActive(false);
        if (hintRoot) hintRoot.SetActive(false);
    }

    void OnDisable()
    {
        if (popupLockedInput && player != null)
            player.SetInputLocked(false);
    }

    public void ShowPopup(
        string title,
        string body,
        Sprite icon = null,
        Sprite keyA = null,
        Sprite keyB = null,
        bool lockInput = true)
    {
        if (!player && GameManager.Instance != null)
            player = GameManager.Instance.player;

        if (popupRoot) popupRoot.SetActive(true);

        if (popupTitleText) popupTitleText.text = title;
        if (popupBodyText) popupBodyText.text = body;

        SetImage(popupIconImage, icon);
        SetImage(popupKeyImageA, keyA);
        SetImage(popupKeyImageB, keyB);

        popupLockedInput = lockInput;
        if (lockInput && player != null)
            player.SetInputLocked(true);
    }

    public void HidePopup()
    {
        if (popupRoot) popupRoot.SetActive(false);

        if (popupLockedInput && player != null)
            player.SetInputLocked(false);

        popupLockedInput = false;
    }

    public void SetMission(string text, float autoHideSeconds = 0f)
    {
        if (missionText) missionText.text = text;
        if (missionRoot) missionRoot.SetActive(true);

        if (missionHideRoutine != null)
            StopCoroutine(missionHideRoutine);

        if (autoHideSeconds > 0f)
            missionHideRoutine = StartCoroutine(HideMissionAfter(autoHideSeconds));
    }

    public void HideMission()
    {
        if (missionHideRoutine != null)
        {
            StopCoroutine(missionHideRoutine);
            missionHideRoutine = null;
        }

        if (missionRoot) missionRoot.SetActive(false);
    }

    IEnumerator HideMissionAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        HideMission();
    }

    public void ShowHint(string text, Sprite icon = null, Sprite key = null, Object owner = null)
    {
        currentHintOwner = owner;

        if (hintText) hintText.text = text;
        if (hintRoot) hintRoot.SetActive(true);

        SetImage(hintIconImage, icon);
        SetImage(hintKeyImage, key);
    }

    public void HideHint(Object owner = null, bool force = false)
    {
        if (!force && owner != null && currentHintOwner != owner)
            return;

        currentHintOwner = null;
        if (hintRoot) hintRoot.SetActive(false);
    }

    void SetImage(Image image, Sprite sprite)
    {
        if (image == null) return;

        image.sprite = sprite;
        image.gameObject.SetActive(sprite != null);
    }
}