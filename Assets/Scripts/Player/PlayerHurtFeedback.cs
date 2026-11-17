using System.Collections;
using UnityEngine;

// CAMBIADO (archivo completo)
// Feedback cuando un enemigo le pega al player. Va en el root del player. PlayerHealth lo llama desde TakeDamage.
//
// Antes: un shake chico + el DamageFlash parpadeando en rojo + este script prendiendo y apagando el sprite,
// todo al mismo tiempo durante 1.5 s. Las dos titilaciones se pisaban y el rojo (que MULTIPLICA el color del
// sprite) en un personaje oscuro se veía casi igual que el sprite normal: no se entendía qué había pasado.
//
// Ahora el golpe tiene 3 momentos bien separados:
//  1) IMPACTO (~0.15 s): hit stop + el mapache se pone de un color SÓLIDO (silueta) + viñeta roja en los bordes
//     de la pantalla + shake fuerte con empujón de cámara + knockback.
//  2) INVULNERABILIDAD: un solo parpadeo, semitransparente, durante los iFrames ("ahora no te pueden pegar").
//  3) Vuelve a la normalidad.
public class PlayerHurtFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Capas de enemigos (y de proyectiles enemigos): se usan para saber desde dónde vino el golpe")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Impacto")]
    [Tooltip("Más largo que cuando pegás vos: recibir daño tiene que sentirse grave")]
    [SerializeField] private float hitStopDuration = 0.12f; // CAMBIADO: antes 0.1
    [Tooltip("0.55 casi no se notaba (el shake usa trauma²). 0.8 se lee claro sin marear.")]
    [SerializeField] private float shakeTrauma = 0.8f;      // CAMBIADO: antes 0.55
    [SerializeField] private float cameraKick = 0.25f;      // CAMBIADO: antes 0.15

    [Header("Golpe mortal")]
    [SerializeField] private float lethalHitStopDuration = 0.15f;
    [SerializeField] private float lethalShakeTrauma = 1f;

    [Header("Flash de impacto (silueta de color sólido)")] // NUEVO
    [Tooltip("Color de la silueta. Al ser sólido (no multiplicado) se lee aunque el sprite sea oscuro.")]
    [SerializeField] private Color flashColor = new Color(1f, 0.22f, 0.22f, 1f); // NUEVO
    [Tooltip("Cuánto dura la silueta, en tiempo REAL (así cubre el hit stop y un poquito más)")]
    [SerializeField] private float flashTime = 0.15f; // NUEVO
    [Tooltip("Material con shader 'GUI/Text Shader'. Usá el MISMO que tenés en el HitReaction de los enemigos. Vacío = lo crea solo (en un build puede fallar).")]
    [SerializeField] private Material flashMaterial; // NUEVO

    [Header("Viñeta roja en los bordes de la pantalla")] // NUEVO
    [SerializeField] private bool vignetteEnabled = true; // NUEVO
    [SerializeField] private Color vignetteColor = new Color(0.8f, 0.03f, 0.03f, 1f); // NUEVO
    [SerializeField, Range(0f, 1f)] private float vignetteAlpha = 0.6f; // NUEVO
    [SerializeField] private float vignetteDuration = 0.45f; // NUEVO
    [SerializeField, Range(0f, 1f)] private float lethalVignetteAlpha = 0.85f; // NUEVO
    [SerializeField] private float lethalVignetteDuration = 1.2f; // NUEVO

    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 0.6f;
    [SerializeField] private float knockbackDuration = 0.15f;
    [Tooltip("Radio donde se busca al enemigo/proyectil más cercano para calcular el empuje")]
    [SerializeField] private float sourceSearchRadius = 2.5f;

    [Header("Parpadeo (invulnerabilidad)")]
    [SerializeField] private bool blinkEnabled = true;
    [SerializeField] private float blinkInterval = 0.1f; // CAMBIADO: antes 0.08 (más lento = se lee como "estado", no como ruido)
    [Tooltip("Opacidad en la parte 'apagada' del parpadeo. Antes el sprite desaparecía del todo.")]
    [SerializeField, Range(0f, 1f)] private float blinkAlpha = 0.3f; // NUEVO
    // ELIMINADO: blinkDelay. Ahora el parpadeo arranca solo cuando termina la silueta de impacto.

    [Header("Sonido (opcional)")]
    [Tooltip("Dejalo vacío: PlayerHealth ya reproduce GameSfx.PlayerHit")]
    [SerializeField] private AudioSource hurtAudioSource;
    [SerializeField] private AudioClip[] hurtClips;

    private Coroutine feedbackRoutine; // CAMBIADO: antes blinkRoutine

    // NUEVO: estado guardado mientras dura la silueta, para dejar el sprite como estaba
    private bool isFlashing;
    private Material savedMaterial;
    private Color savedColor;

    private void Awake()
    {
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // NUEVO: mismo fallback que HitReaction
        if (flashMaterial == null)
        {
            Shader shader = Shader.Find("GUI/Text Shader");
            if (shader != null) flashMaterial = new Material(shader);
        }
    }

    private void OnDisable()
    {
        StopFeedback(); // CAMBIADO: deja el sprite visible, opaco y con su material
    }

    /// <summary>Lo llama PlayerHealth. 'invulnerabilityDuration' = sus iFrames, así el parpadeo dura lo mismo.</summary>
    public void Play(float invulnerabilityDuration, bool lethal)
    {
        CombatFeedback fx = CombatFeedback.Instance;
        Vector2 direction = GetKnockbackDirection();

        StopFeedback();

        if (lethal)
        {
            // Muerte: congelamiento y shake al máximo, silueta + viñeta fuerte, sin empuje ni parpadeo
            HitStopManager.Play(lethalHitStopDuration); // CAMBIADO: directo, no depende de que exista CombatFeedback
            if (fx != null)
                fx.AddTrauma(lethalShakeTrauma);

            if (vignetteEnabled) // NUEVO
                ScreenVignetteFlash.Play(vignetteColor, lethalVignetteAlpha, lethalVignetteDuration);

            if (isActiveAndEnabled) // NUEVO
                feedbackRoutine = StartCoroutine(FeedbackRoutine(0f, false));

            PlaySound();
            return;
        }

        HitStopManager.Play(hitStopDuration); // CAMBIADO: directo, no depende de que exista CombatFeedback
        if (fx != null)
        {
            fx.AddTrauma(shakeTrauma);
            fx.Kick(direction, cameraKick);
        }

        if (vignetteEnabled) // NUEVO
            ScreenVignetteFlash.Play(vignetteColor, vignetteAlpha, vignetteDuration);

        if (playerMovement != null && playerMovement.enabled && direction != Vector2.zero && knockbackDistance > 0f)
            playerMovement.ApplyKnockback(direction, knockbackDistance, knockbackDuration);

        PlaySound();

        if (isActiveAndEnabled) // CAMBIADO: la silueta y el parpadeo van en una sola corrutina, uno después del otro
            feedbackRoutine = StartCoroutine(FeedbackRoutine(invulnerabilityDuration, blinkEnabled));
    }

    // NUEVO: 1) silueta sólida  2) parpadeo semitransparente hasta que terminan los iFrames
    private IEnumerator FeedbackRoutine(float invulnerabilityDuration, bool blink)
    {
        BeginSolidFlash();

        // Tiempo real: el hit stop frena el tiempo del juego y la silueta tiene que verse igual
        float t = 0f;
        while (t < flashTime)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        EndSolidFlash();

        if (blink && spriteRenderer != null)
        {
            // Tiempo de juego: los iFrames de PlayerHealth también usan Time.time
            float elapsed = flashTime;
            bool dim = true;
            while (elapsed < invulnerabilityDuration)
            {
                SetAlpha(dim ? blinkAlpha : 1f);
                dim = !dim;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            SetAlpha(1f);
        }

        feedbackRoutine = null;
    }

    // NUEVO
    private void BeginSolidFlash()
    {
        if (spriteRenderer == null || flashMaterial == null || isFlashing) return;

        savedMaterial = spriteRenderer.sharedMaterial;
        savedColor = spriteRenderer.color;
        isFlashing = true;

        spriteRenderer.sharedMaterial = flashMaterial;
        spriteRenderer.color = flashColor;
    }

    // NUEVO
    private void EndSolidFlash()
    {
        if (!isFlashing || spriteRenderer == null) return;

        spriteRenderer.sharedMaterial = savedMaterial;
        spriteRenderer.color = savedColor;
        isFlashing = false;
    }

    // NUEVO: cambia solo la opacidad (así respeta el tinte verde del veneno, si lo hay)
    private void SetAlpha(float alpha)
    {
        if (spriteRenderer == null) return;

        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }

    // CAMBIADO: antes StopBlink
    private void StopFeedback()
    {
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = null;

        EndSolidFlash();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            SetAlpha(1f);
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
}