using System.Collections;
using UnityEngine;

public class ElevatorMotionFX : MonoBehaviour
{
    [Header("Shake Target")]
    public Transform shakeTarget;

    [Header("Shake")]
    public float shakeAmount = 0.08f;
    public float shakeFrequency = 35f;

    Vector3 originalLocalPos;

    void Awake()
    {
        if (shakeTarget != null)
            originalLocalPos = shakeTarget.localPosition;
    }

    public IEnumerator Shake(float seconds)
    {
        if (shakeTarget == null)
            yield break;

        float t = 0f;

        while (t < seconds)
        {
            t += Time.deltaTime;

            float x = Mathf.Sin(Time.time * shakeFrequency) * shakeAmount;
            float y = Mathf.Cos(Time.time * shakeFrequency * 0.7f) * shakeAmount;

            shakeTarget.localPosition = originalLocalPos + new Vector3(x, y, 0f);

            yield return null;
        }

        shakeTarget.localPosition = originalLocalPos;
    }
}