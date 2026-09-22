using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PickupParticle : MonoBehaviour
{
    public struct Settings
    {
        public Vector2 velocity;
        public float gravity;
        public float drag;
        public float rotationSpeed;
        public float lifetime;
        public float startScale;
        public float endScaleMultiplier;
        public Color color;
        public float fadeStart;          // 0-1: a partir de qué % de vida empieza a desvanecerse
        public Transform attractTarget;  // si no es null, la partícula vuela hacia el que la recogió
        public float attractForce;
    }

    private SpriteRenderer sr;
    private Settings settings;
    private Vector2 velocity;
    private float timer;
    private Action onReturn;
    private bool running;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Play(Settings config, Action returnCallback)
    {
        settings = config;
        onReturn = returnCallback;
        velocity = config.velocity;
        timer = 0f;
        running = true;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.color = config.color;
        transform.localScale = Vector3.one * config.startScale;
        transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
    }

    private void Update()
    {
        if (!running) return;

        float dt = Time.deltaTime;
        timer += dt;

        if (timer >= settings.lifetime)
        {
            running = false;
            onReturn?.Invoke();
            return;
        }

        float t = timer / settings.lifetime;

        // Movimiento
        if (settings.attractTarget != null)
        {
            Vector2 dir = ((Vector2)settings.attractTarget.position - (Vector2)transform.position).normalized;
            velocity += dir * settings.attractForce * dt;
        }
        else
        {
            velocity.y -= settings.gravity * dt;
        }

        velocity *= Mathf.Clamp01(1f - settings.drag * dt);
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0f, 0f, settings.rotationSpeed * dt);

        // Escala: se va achicando hasta desaparecer
        float scale = Mathf.Lerp(settings.startScale, settings.startScale * settings.endScaleMultiplier, t);
        transform.localScale = Vector3.one * scale;

        // Alpha
        float alpha = 1f;
        if (t > settings.fadeStart)
            alpha = 1f - Mathf.InverseLerp(settings.fadeStart, 1f, t);

        Color c = settings.color;
        sr.color = new Color(c.r, c.g, c.b, c.a * alpha);
    }

    public void StopImmediate()
    {
        running = false;
    }
}

