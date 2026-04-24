using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NoiseOnCollision : MonoBehaviour
{
    // 충돌했을 때 소리가 나야 하는 오브젝트
    public float noiseRadius = 5f;
    public float minImpactSpeed = 1.0f;
    public float cooldownSeconds = 0.5f;

    float lastNoiseTime = -999f;

    void OnCollisionEnter(Collision collision)
    {
        TryEmitNoise(collision.relativeVelocity.magnitude);
    }

    void OnCollisionStay(Collision collision)
    {
        TryEmitNoise(collision.relativeVelocity.magnitude);
    }

    void TryEmitNoise(float impactSpeed)
    {
        if (impactSpeed < minImpactSpeed)
            return;

        if (Time.time - lastNoiseTime < cooldownSeconds)
            return;

        lastNoiseTime = Time.time;
        NoiseSystem.Emit(transform.position, noiseRadius, 1f, transform, NoiseKind.Object);
    }
}