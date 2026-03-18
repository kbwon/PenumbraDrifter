using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public Animator anim;

    [Header("Health")]
    [Min(1)] public int maxPips = 5;
    public int currentPips { get; private set; }
    public bool isDead { get; private set; }

    [Header("Game Over")]
    public GameObject gameOverUI;
    public MonoBehaviour[] controlScriptsToDisable;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDead;

    void Awake()
    {
        currentPips = maxPips;
        isDead = false;

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (gameOverUI)
            gameOverUI.SetActive(false);
    }

    void Start()
    {
        NotifyHealthChanged();
    }

    public void TakeDamage(int damagePips)
    {
        if (isDead) return;
        if (damagePips <= 0) return;

        currentPips = Mathf.Max(0, currentPips - damagePips);

        if (anim != null)
            anim.SetBool("hurt", true);

        NotifyHealthChanged();

        if (currentPips <= 0)
        {
            currentPips = 0;
            Die();
        }
    }

    public void Heal(int healPips)
    {
        if (isDead) return;
        if (healPips <= 0) return;

        currentPips = Mathf.Min(maxPips, currentPips + healPips);
        NotifyHealthChanged();
    }

    public void SetMaxPips(int newMax, bool fillToMax = false)
    {
        maxPips = Mathf.Max(1, newMax);

        if (fillToMax)
            currentPips = maxPips;
        else
            currentPips = Mathf.Min(currentPips, maxPips);

        NotifyHealthChanged();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (anim != null)
            anim.SetBool("die", true);

        if (controlScriptsToDisable != null)
        {
            foreach (MonoBehaviour script in controlScriptsToDisable)
            {
                if (script != null)
                    script.enabled = false;
            }
        }

        if (gameOverUI)
            gameOverUI.SetActive(true);

        OnDead?.Invoke();
    }

    void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentPips, maxPips);
    }
}
