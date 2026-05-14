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
    public MonoBehaviour[] controlScriptsToDisable;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDead;

    void Awake()
    {
        currentPips = maxPips;
        isDead = false;

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterHealth(this);
    }

    void Start()
    {
        NotifyHealthChanged();
    }

    // 데미지를 받아 체력 칸을 줄인다.
    public void TakeDamage(int damagePips)
    {
        if (isDead) return;
        if (damagePips <= 0) return;

        currentPips = Mathf.Max(0, currentPips - damagePips);

        if (anim != null)
            anim.SetTrigger("Hurt");

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

        // 별도 사망 애니메이션은 사용하지 않음.
        // TakeDamage()에서 이미 hurt 트리거가 실행되므로,
        // 그 피격 애니메이션이 잠깐 나온 뒤 GameOverDirector가 멈추게 한다.

        if (controlScriptsToDisable != null)
        {
            for (int i = 0; i < controlScriptsToDisable.Length; i++)
            {
                MonoBehaviour script = controlScriptsToDisable[i];
                if (script != null)
                    script.enabled = false;
            }
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameOver(true);

        OnDead?.Invoke();
    }

    void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentPips, maxPips);
    }
}
