using System;
using UnityEngine;

public class PlayerPoisonStatus : MonoBehaviour
{
    private const int RoomsPerTick = 3;
    private static readonly Color PoisonTint = new Color(0.35f, 0.92f, 0.38f, 1f);

    private int roomsSinceTick;
    private ParticleSystem poisonParticles;
    private DamageFlash damageFlash;
    private SpriteRenderer spriteRenderer;

    public bool IsPoisoned { get; private set; }
    public event Action<bool> PoisonChanged;

    private void Awake()
    {
        damageFlash = GetComponent<DamageFlash>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void ApplyPoison()
    {
        if (IsPoisoned)
            return;

        IsPoisoned = true;
        roomsSinceTick = 0;
        ApplyVisuals(true);
        PoisonChanged?.Invoke(true);
    }

    public void Cure()
    {
        if (!IsPoisoned)
            return;

        IsPoisoned = false;
        roomsSinceTick = 0;
        ApplyVisuals(false);
        PoisonChanged?.Invoke(false);
    }

    public void NotifyRoomEntered()
    {
        if (!IsPoisoned)
            return;

        CameraShake.Play(0.2f, 0.09f);
        roomsSinceTick++;
        if (roomsSinceTick < RoomsPerTick)
            return;

        roomsSinceTick = 0;
        GetComponent<PlayerHealth>()?.TakePoisonTick();
    }

    private void ApplyVisuals(bool poisoned)
    {
        if (damageFlash != null)
            damageFlash.SetPersistentTint(poisoned ? PoisonTint : Color.white);
        else if (spriteRenderer != null)
            spriteRenderer.color = poisoned ? PoisonTint : Color.white;

        EnsureParticles();
        if (poisonParticles == null)
            return;

        if (poisoned)
        {
            if (!poisonParticles.isPlaying)
                poisonParticles.Play();
        }
        else
        {
            poisonParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void EnsureParticles()
    {
        if (poisonParticles != null)
            return;

        var go = new GameObject("PoisonParticles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        poisonParticles = go.AddComponent<ParticleSystem>();
        poisonParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = poisonParticles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = 0.55f;
        main.startSpeed = 0.35f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.25f, 0.95f, 0.28f, 0.85f);
        main.gravityModifier = -0.15f;
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = poisonParticles.emission;
        emission.rateOverTime = 18f;

        var shape = poisonParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;

        var colorOver = poisonParticles.colorOverLifetime;
        colorOver.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.45f, 1f, 0.4f), 0f),
                new GradientColorKey(new Color(0.1f, 0.55f, 0.15f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOver.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 8;
    }
}
