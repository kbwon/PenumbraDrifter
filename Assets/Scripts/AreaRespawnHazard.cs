using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaRespawnHazard : MonoBehaviour
{
    [Header("Respawn")]
    public Transform respawnPoint;

    [Header("Options")]
    public bool forceExitShadowMode = true;

    bool respawning;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (respawning) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        RespawnPlayer(player);
    }

    void RespawnPlayer(PlayerController player)
    {
        if (respawnPoint == null)
        {
            Debug.LogWarning("[AreaRespawnHazard] RespawnPoint가 없습니다.");
            return;
        }

        respawning = true;

        ShadowInteractController shadow = player.GetComponent<ShadowInteractController>();

        if (forceExitShadowMode && shadow != null)
        {
            shadow.ForceExitShadowMode();
            shadow.ClearSurfaceAnchor();
            shadow.ClearMovingShadowHost();
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = respawnPoint.position;
        }
        else
        {
            player.transform.position = respawnPoint.position;
        }

        player.SyncShadowStateWithoutTransition();

        Debug.Log("[CollectibleArea] 위험 바닥에 닿아 구역 시작점으로 복귀");

        respawning = false;
    }
}