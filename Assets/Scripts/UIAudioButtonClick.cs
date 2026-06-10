using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIAudioButtonClick : MonoBehaviour
{
    public UIAudioManager manager;

    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.RemoveListener(PlayClickSound);
        button.onClick.AddListener(PlayClickSound);
    }

    void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClickSound);
    }

    void PlayClickSound()
    {
        UIAudioManager target = manager != null ? manager : UIAudioManager.Instance;

        if (target != null)
            target.PlayButtonClick();
    }
}