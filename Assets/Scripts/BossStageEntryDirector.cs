using System.Collections;
using UnityEngine;

public class BossStageEntryDirector : MonoBehaviour
{
    [Header("Refs")]
    public PlayerController player;
    public ElevatorDoorController elevatorDoor;
    public Transform walkTarget;

    [Header("Timing")]
    public float startDelay = 0.3f;
    public float afterDoorOpenDelay = 0.15f;
    public float walkSpeed = 2.5f;
    public float walkSeconds = 0.8f;
    public float afterWalkDelay = 0.2f;
    public bool closeDoorAfterEntry = true;

    [Header("Optional Boss")]
    public BossController boss;
    public bool activateBossAfterEntry = true;

    IEnumerator Start()
    {
        yield return null;

        if (player == null && GameManager.Instance != null)
            player = GameManager.Instance.player;

        if (boss == null)
            boss = FindFirstObjectByType<BossController>();

        if (activateBossAfterEntry && boss != null)
            boss.SetCombatActive(false);

        if (player != null)
            player.SetInputLocked(true);

        yield return new WaitForSecondsRealtime(startDelay);

        if (elevatorDoor != null)
            yield return elevatorDoor.Open();

        yield return new WaitForSecondsRealtime(afterDoorOpenDelay);

        if (player != null && walkTarget != null)
        {
            Vector3 dir = walkTarget.position - player.transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                dir.Normalize();
            else
                dir = Vector3.forward;

            player.BeginScriptedMove(dir, walkSpeed);
            yield return new WaitForSecondsRealtime(walkSeconds);
            player.EndScriptedMove(true);
        }

        yield return new WaitForSecondsRealtime(afterWalkDelay);

        if (closeDoorAfterEntry && elevatorDoor != null)
            yield return elevatorDoor.Close();

        if (activateBossAfterEntry && boss != null)
            boss.SetCombatActive(true);

        if (player != null)
            player.SetInputLocked(false);
    }
}