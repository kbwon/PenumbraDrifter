using System;
using UnityEngine;

public enum NoiseKind
{
    Footstep,
    Sprint,
    Crouch,
    Object,
    Custom
}

public struct GameNoise
{
    public Vector3 position;
    public float radius;
    public float strength;
    public Transform source;
    public NoiseKind kind;

    public GameNoise(Vector3 position, float radius, float strength, Transform source, NoiseKind kind)
    {
        this.position = position;
        this.radius = radius;
        this.strength = strength;
        this.source = source;
        this.kind = kind;
    }
}

public static class NoiseSystem
{
    public static event Action<GameNoise> OnNoise;

    public static void Emit(Vector3 position, float radius, float strength = 1f, Transform source = null, NoiseKind kind = NoiseKind.Custom)
    {
        if (radius <= 0f) return;
        if (strength <= 0f) return;

        OnNoise?.Invoke(new GameNoise(position, radius, strength, source, kind));
    }
}