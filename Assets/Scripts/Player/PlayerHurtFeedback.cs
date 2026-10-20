using System.Collections;
using UnityEngine;

// NUEVO (archivo completo)
// Feedback cuando un enemigo le pega al player: hit stop, shake fuerte, knockback,
// parpadeo durante la invulnerabilidad y (opcional) sonido. Va en el root del player.
// PlayerHealth lo llama solo desde TakeDamage.
public class PlayerHurtFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Capas de enemigos (y de proyectiles enemigos): se usan para saber desde dónde vino el golpe")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Impacto")]
    [Tooltip("Más largo que cuando pegás vos: recibir daño tiene que sentirse grave")]
    [SerializeField] private float hitStopDuration = 0.1f;
    [SerializeField] private float shakeTrauma = 0.55f;
    [SerializeField] private float cameraKick = 0.15f;

    [Header("Golpe mortal")]
    [SerializeField] private float lethalHitStopDuration = 0.15f;
    [SerializeField] private float lethalShakeTrauma = 1f;

    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 0.6f;
    [SerializeField] private float knockbackDuration = 0.15f;
    [Tooltip("Radio donde se busca al enemigo/proyectil más cercano para calcular el empuje")]
    [SerializeField] private float sourceSearchRadius = 2.5f;

    [Header("Parpadeo")]
    [Tooltip("Apagalo si el parpadeo junto con tu DamageFlash rojo te parece demasiado")]
    [SerializeField] private bool blinkEnabled = true;
    [SerializeField] private float blinkInterval = 0.08f;
    [Tooltip("Espera antes de parpadear, para que se vea primero el flash rojo")]
    [SerializeField] private float blinkDelay = 0.1f;

    [Header("Sonido (opcional)")]
    [Tooltip("Dejalo vacío: PlayerHealth ya reproduce GameSfx.PlayerHit")]
    [SerializeField] private AudioSource hurtAudioSource;
    [SerializeField] private AudioClip[] hurtClips;

    private Coroutine blinkRoutine;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnDisable()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        blinkRoutine = null;
    }

    /// <summary>Lo llama PlayerHealth. 'invulnerabilityDuration' = sus iFrames, así el parpadeo dura lo mismo.</summary>
    public void Play(float invulnerabilityDuration, bool lethal)
    {
        CombatFeedback fx = CombatFeedback.Instance;
        Vector2 direction = GetKnockbackDirection();

        if (lethal)
        {
            // Muerte: congelamiento y shake al máximo, sin empuje ni parpadeo (los controles se desactivan)
            if (fx != null)
            {
                fx.HitStop(lethalHitStopDuration);
                fx.AddTrauma(lethalShakeTrauma);
            }

            StopBlink();
            PlaySound();
            return;
        }

        if (fx != null)
        {
            fx.HitStop(hitStopDuration);
            fx.AddTrauma(shakeTrauma);
            fx.Kick(direction, cameraKick);
        }

        if (playerMovement != null && playerMovement.enabled && direction != Vector2.zero && knockbackDistance > 0f)
            playerMovement.ApplyKnockback(direction, knockbackDistance, knockbackDuration);

        PlaySound();

        if (blinkEnabled && spriteRenderer != null && invulnerabilityDuration > 0f)
        {
            StopBlink();
            blinkRoutine = StartCoroutine(BlinkRoutine(invulnerabilityDuration));
        }
    }

    private Vector2 GetKnockbackDirection()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, sourceSearchRadius, enemyLayer);
        Collider2D nearest = null;
        float best = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            float d = ((Vector2)hit.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = hit;
            }
        }

        if (nearest == null) return Vector2.zero;
        return (Vector2)transform.position - (Vector2)nearest.transform.position;
    }

    private void PlaySound()
    {
        if (hurtAudioSource == null || hurtClips == null || hurtClips.Length == 0) return;

        hurtAudioSource.pitch = Random.Range(0.95f, 1.05f);
        hurtAudioSource.PlayOneShot(hurtClips[Random.Range(0, hurtClips.Length)]);
    }

    private void StopBlink()
    {
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = null;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }

    private IEnumerator BlinkRoutine(float duration)
    {
        spriteRenderer.enabled = true;
        yield return new WaitForSeconds(blinkDelay);

        float elapsed = blinkDelay;
        while (elapsed < duration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        spriteRenderer.enabled = true;
        blinkRoutine = null;
    }
}