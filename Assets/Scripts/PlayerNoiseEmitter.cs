using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    public PlayerController player;
    public ShadowInteractController shadowCtrl;

    [Header("Footstep Noise")]
    public float walkNoiseRadius = 4.0f;
    public float footstepInterval = 0.35f;

    [Header("Rules")]
    public bool emitNoiseInShadowMode = false;
    public bool emitNoiseWhileCrouching = false;

    float footstepTimer;

    void Awake()
    {
        if (!player)
            player = GetComponent<PlayerController>();

        if (!shadowCtrl)
            shadowCtrl = GetComponent<ShadowInteractController>();
    }

    void Update()
    {
        if (!player) return;

        if (player.InputLocked)
        {
            footstepTimer = 0f;
            return;
        }

        bool inShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;
        if (inShadow && !emitNoiseInShadowMode)
        {
            footstepTimer = 0f;
            return;
        }

        if (player.IsCrouching && !emitNoiseWhileCrouching)
        {
            footstepTimer = 0f;
            return;
        }

        bool moving = player.MoveDirection.sqrMagnitude > 0.0001f;
        if (!moving || !player.IsGrounded)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer > 0f)
            return;

        NoiseSystem.Emit(
            transform.position,
            walkNoiseRadius,
            1f,
            transform,
            NoiseKind.Footstep
        );

        footstepTimer = footstepInterval;
    }
}