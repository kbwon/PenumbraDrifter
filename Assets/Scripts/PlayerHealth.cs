using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public Animator anim;

    [Header("Health Pips")]
    [Min(1)] public int maxPips = 5;  // ✅ 나중에 늘어날 수 있음
    public int currentPips { get; private set; }

    [Header("Events (Optional)")]
    public bool isDead { get; private set; }

    [Header("Game Over")]
    public GameObject gameOverUI;                 // ✅ Canvas의 GameOver Text(또는 패널) 연결
    public MonoBehaviour[] controlScriptsToDisable; // ✅ 죽을 때 꺼야 할 스크립트들

    void Awake()
    {
        currentPips = maxPips;
        isDead = false;

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (gameOverUI) gameOverUI.SetActive(false);
    }

    // ✅ 외부에서 호출: damagePips 만큼 체력 칸 감소 (1~2칸 등)
    public void TakeDamage(int damagePips)
    {
        if (isDead) return;
        if (damagePips <= 0) return;

        int prev = currentPips;
        currentPips = Mathf.Max(0, currentPips - damagePips);

        // TODO: Damage animation / hit feedback trigger here
        // (예: 피격 플래시, 카메라 흔들림, 사운드 등)
        anim.SetBool("hurt", true);

        if (currentPips <= 0)
        {
            currentPips = 0;
            Die();
        }
    }

    // ✅ (필요시) 회복/최대체력 변경 지원
    public void Heal(int healPips)
    {
        if (isDead) return;
        if (healPips <= 0) return;

        currentPips = Mathf.Min(maxPips, currentPips + healPips);

        // TODO: Heal animation / feedback here (optional)
    }

    public void SetMaxPips(int newMax, bool fillToMax = false)
    {
        newMax = Mathf.Max(1, newMax);
        maxPips = newMax;

        if (fillToMax) currentPips = maxPips;
        else currentPips = Mathf.Min(currentPips, maxPips);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // TODO: Game Over animation / sequence trigger here
        // (예: 사망 연출, UI 페이드, 컨트롤 잠금, 게임오버 화면)
        anim.SetBool("die", true);

        // 예시: 컨트롤 비활성화 등을 여기서 처리 가능
        // ✅ 컨트롤 비활성화
        if (controlScriptsToDisable != null)
        {
            foreach (var c in controlScriptsToDisable)
            {
                if (c != null) c.enabled = false;
            }
        }

        // ✅ GameOver UI 표시
        if (gameOverUI) gameOverUI.SetActive(true);
    }
}
