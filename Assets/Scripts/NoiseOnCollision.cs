using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NoiseOnCollision : MonoBehaviour
{
    public float noiseRadius = 8f;
    public float minImpactSpeed = 1.0f;
    public float cooldownSeconds = 0.5f;

    float lastNoiseTime = -999f;

    void OnCollisionEnter(Collision collision)
    {
        TryEmitNoise(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        TryEmitNoise(collision);
    }

    void TryEmitNoise(Collision collision)
    {
        if (collision.relativeVelocity.magnitude < minImpactSpeed)
            return;

        if (Time.time - lastNoiseTime < cooldownSeconds)
            return;

        lastNoiseTime = Time.time;

        Vector3 noisePos = transform.position;

        if (collision.contactCount > 0)
            noisePos = collision.GetContact(0).point;

        NoiseSystem.Emit(
            noisePos,
            noiseRadius,
            1f,
            transform,
            NoiseKind.Object
        );
    }
}