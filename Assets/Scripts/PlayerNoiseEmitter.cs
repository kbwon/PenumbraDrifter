using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    public PlayerController player;
    public ShadowInteractController shadowCtrl;

    [Header("Keys")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Footstep Noise")]
    public float walkNoiseRadius = 3.0f;
    public float sprintNoiseRadius = 6.0f;
    public float crouchNoiseRadius = 0.4f;
    public float footstepInterval = 0.35f;

    [Header("Object Noise")]
    public float objectNoiseRadius = 5.0f;

    [Header("Rules")]
    public bool emitNoiseInShadowMode = false;

    float footstepTimer;

    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }

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

        IsCrouching = Input.GetKey(crouchKey);
        IsSprinting = Input.GetKey(sprintKey) && !IsCrouching;

        bool inShadow = shadowCtrl != null && shadowCtrl.IsInShadowMode;
        if (inShadow && !emitNoiseInShadowMode)
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

        float radius = GetCurrentFootstepRadius();
        NoiseKind kind = GetCurrentNoiseKind();

        NoiseSystem.Emit(transform.position, radius, 1f, transform, kind);
        footstepTimer = footstepInterval;
    }

    float GetCurrentFootstepRadius()
    {
        if (IsCrouching)
            return crouchNoiseRadius;

        if (IsSprinting)
            return sprintNoiseRadius;

        return walkNoiseRadius;
    }

    NoiseKind GetCurrentNoiseKind()
    {
        if (IsCrouching) return NoiseKind.Crouch;
        if (IsSprinting) return NoiseKind.Sprint;
        return NoiseKind.Footstep;
    }

    public void EmitObjectNoise(Vector3 position)
    {
        NoiseSystem.Emit(position, objectNoiseRadius, 1f, transform, NoiseKind.Object);
    }
}