using UnityEngine;

public class EnemyKillable : MonoBehaviour
{
    [Header("Assassination")]
    public bool canBeAssassinated = true;

    // 현재는 비활성화로 즉시 제거한다.
    public void KillByAssassination()
    {
        if (!canBeAssassinated) return;
        gameObject.SetActive(false);
    }
}
