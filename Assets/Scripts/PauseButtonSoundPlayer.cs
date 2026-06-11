using UnityEngine;
using UnityEngine.UI;

public class PauseButtonSoundPlayer : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip clickClip;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Bind Target")]
    public GameObject[] buttonRoots;

    [Header("Options")]
    public bool autoBindOnStart = true;
    public bool includeInactiveButtons = true;
    public bool debugLog = false;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
        }
    }

    void Start()
    {
        if (autoBindOnStart)
            BindButtons();
    }

    [ContextMenu("Bind Buttons")]
    public void BindButtons()
    {
        if (buttonRoots == null || buttonRoots.Length == 0)
            return;

        int count = 0;

        for (int i = 0; i < buttonRoots.Length; i++)
        {
            GameObject root = buttonRoots[i];

            if (root == null)
                continue;

            Button[] buttons = root.GetComponentsInChildren<Button>(includeInactiveButtons);

            for (int j = 0; j < buttons.Length; j++)
            {
                Button button = buttons[j];

                if (button == null)
                    continue;

                button.onClick.RemoveListener(PlayClick);
                button.onClick.AddListener(PlayClick);

                count++;
            }
        }

        if (debugLog)
            Debug.Log($"[PauseButtonSoundPlayer] Bound {count} buttons.", this);
    }

    public void PlayClick()
    {
        if (audioSource == null)
            return;

        if (clickClip == null)
            return;

        audioSource.PlayOneShot(clickClip, volume);
    }
}