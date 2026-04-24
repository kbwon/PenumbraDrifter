using UnityEngine;
using UnityEngine.UI;

public enum EnemyAwarenessDisplay
{
    Hidden,
    VisualAlert,   // ³ë¶õ ´À³¦Ç¥
    Attack,        // »¡°£ ´À³¦Ç¥
    SoundAlert     // ³ë¶õ ¹°À½Ç¥
}

public class EnemyAlertUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject root;

    [Header("Images")]
    public Image baseImage;
    public Image fillImage;

    [Header("Sprites")]
    public Sprite visualAlertSprite;
    public Sprite attackSprite;
    public Sprite soundAlertSprite;

    [Header("Colors")]
    public Color visualAlertColor = new Color(1f, 0.85f, 0.05f, 1f);
    public Color attackColor = new Color(1f, 0.05f, 0.03f, 1f);
    public Color soundAlertColor = new Color(1f, 0.85f, 0.05f, 1f);
    public Color baseColor = new Color(0f, 0f, 0f, 0.35f);

    void Awake()
    {
        if (!root)
            root = gameObject;

        Show(EnemyAwarenessDisplay.Hidden, 0f);
    }

    public void Show(EnemyAwarenessDisplay display, float fill01)
    {
        fill01 = Mathf.Clamp01(fill01);

        if (display == EnemyAwarenessDisplay.Hidden)
        {
            if (root)
                root.SetActive(false);

            return;
        }

        if (root)
            root.SetActive(true);

        Sprite sprite = null;
        Color fillColor = Color.white;

        switch (display)
        {
            case EnemyAwarenessDisplay.VisualAlert:
                sprite = visualAlertSprite;
                fillColor = visualAlertColor;
                break;

            case EnemyAwarenessDisplay.Attack:
                sprite = attackSprite;
                fillColor = attackColor;
                fill01 = 1f;
                break;

            case EnemyAwarenessDisplay.SoundAlert:
                sprite = soundAlertSprite;
                fillColor = soundAlertColor;
                break;
        }

        if (baseImage)
        {
            baseImage.sprite = sprite;
            baseImage.color = baseColor;
            baseImage.enabled = sprite != null;
        }

        if (fillImage)
        {
            fillImage.sprite = sprite;
            fillImage.color = fillColor;
            fillImage.fillAmount = fill01;
            fillImage.enabled = sprite != null;
        }
    }
}