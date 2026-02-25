using UnityEngine;

public class EnemyKillable : MonoBehaviour
{
    [Header("Assassination")]
    public bool canBeAssassinated = true; // 나중에 보스/특수 적은 false로 두고 조건 구현 가능

    public void KillByAssassination()
    {
        if (!canBeAssassinated) return;

        // TODO: Enemy death animation / VFX / SFX (원하시면 여기에서 트리거)
        // TODO: Drop items / score / etc.

        gameObject.SetActive(false); // 현재 단계: "사라짐"
    }
}
