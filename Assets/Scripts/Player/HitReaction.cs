using System.Collections;
using UnityEngine;

// NUEVO (archivo completo)
// Va en el ROOT de cada enemigo (y opcionalmente en tachos/objetos rompibles).
// Hace: knockback con caída suave, stun corto, flash blanco, squash y trigger de animación de "hit".
//
// EnemyMovement y EnemyBehaviour ya chequean IsStunned: mientras dura, el enemigo
// no se mueve ni corre su máquina de estados (no ataca).
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)] // su FixedUpdate corre después del movimiento del enemigo
public class HitReaction : MonoBehaviour
{
    [Header("Knockback")]
    [SerializeField] private Rigidbody2D rb;
    [Tooltip("0 = recibe todo el knockback, 1 = inmune (ideal ~0.8-1 para bosses)")]
    [SerializeField, Range(0f, 1f)] private float knockbackResistance = 0f;
    [SerializeField] private float knockbackDuration = 0.15f;
    [Tooltip("Solo para Rigidbody2D Kinematic: capas que frenan el knockback (paredes)")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Stun")]
    [Tooltip("Tiempo que la IA no se mueve/ataca después del golpe. 0 para bosses si no querés interrumpirlos.")]
    [SerializeField] private float stunDuration = 0.2f;

    [Header("Hit Flash")]
    [Tooltip("Sprites que se ponen blancos. Vacío = todos los hijos (dejá afuera sombras si las tenés)")]
    [SerializeField] private SpriteRenderer[] renderersToFlash;
    [Tooltip("Material con shader 'GUI/Text Shader' (pinta el sprite de un color sólido)")]
    [SerializeField] private Material flashMaterial;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.08f;

    [Header("Squash")]
    [SerializeField] private SquashStretch squash;
    [SerializeField] private Vector2 hitSquashScale = new Vector2(1.3f, 0.75f);

    [Header("Animation (opcional)")]
    [SerializeField] private Animator animator;
    [Tooltip("Nombre de un trigger 'Hit' en el Animator del enemigo. Vacío = no se usa.")]
    [SerializeField] private string hitTriggerParam = "";

    public bool IsStunned => Time.time < stunEndTime;
    public bool IsInKnockback => knockbackTimer > 0f;

    /// <summary>Por si otro script quiere reaccionar al golpe (sonido propio, cambiar de estado, etc.)</summary>
    public event System.Action<Vector2> Hit;

    private SpriteRenderer[] renderers;
    private Material[] savedMaterials;
    private Color[] savedColors;
    private bool isFlashing;
    private Coroutine flashRoutine;

    private Vector2 knockbackVelocity;
    private float knockbackTimer;
    private float stunEndTime = -999f;

    private static readonly RaycastHit2D[] castHits = new RaycastHit2D[4];
    private const float Skin = 0.02f;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (squash == null) squash = GetComponentInChildren<SquashStretch>();

        renderers = (renderersToFlash != null && renderersToFlash.Length > 0)
            ? renderersToFlash
            : GetComponentsInChildren<SpriteRenderer>(true);
        savedMaterials = new Material[renderers.Length];
        savedColors = new Color[renderers.Length];

        // Fallback: OJO, en un build el shader puede no incluirse si no hay un material que lo use.
        // Lo correcto es crear el material en el proyecto y asignarlo.
        if (flashMaterial == null)
        {
            Shader shader = Shader.Find("GUI/Text Shader");
            if (shader != null) flashMaterial = new Material(shader);
        }
    }

    private void OnDisable()
    {
        // Si el enemigo se desactiva (pool, muerte) en medio del flash, dejarlo como estaba
        if (isFlashing) RestoreRenderers();
        flashRoutine = null;
        knockbackTimer = 0f;
    }

    public void ReceiveHit(Vector2 direction, float force)
    {
        if (direction.sqrMagnitude < 0.0001f)
            direction = Random.insideUnitCircle;
        direction.Normalize();

        float finalForce = force * (1f - knockbackResistance);
        bool hasKnockback = finalForce > 0.01f && rb != null;

        if (hasKnockback)
        {
            knockbackVelocity = direction * finalForce;
            knockbackTimer = knockbackDuration;
        }

        float stun = hasKnockback ? Mathf.Max(stunDuration, knockbackDuration) : stunDuration;
        stunEndTime = Mathf.Max(stunEndTime, Time.time + stun);

        Flash();

        if (squash != null)
            squash.Punch(hitSquashScale);

        if (animator != null && !string.IsNullOrEmpty(hitTriggerParam))
            animator.SetTrigger(hitTriggerParam);

        Hit?.Invoke(direction);
    }

    private void FixedUpdate()
    {
        if (knockbackTimer <= 0f || rb == null) return;

        knockbackTimer -= Time.fixedDeltaTime;

        // Arranca fuerte y frena rápido (curva cuadrática): se siente como un "golpe", no como un empujón
        float t = 1f - Mathf.Clamp01(knockbackTimer / knockbackDuration);
        float strength = (1f - t) * (1f - t);
        Vector2 velocity = knockbackVelocity * strength;

        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            // Unity 6. Si usás Unity 2022 o anterior, cambiar linearVelocity por velocity
            rb.linearVelocity = knockbackTimer > 0f ? velocity : Vector2.zero;
        }
        else
        {
            MoveKinematic(velocity * Time.fixedDeltaTime);
        }
    }

    private void MoveKinematic(Vector2 delta)
    {
        float distance = delta.magnitude;
        if (distance < 0.0001f) return;

        Vector2 dir = delta / distance;

        if (obstacleMask.value != 0)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(obstacleMask);
            filter.useTriggers = false;

            int count = rb.Cast(dir, filter, castHits, distance + Skin);
            if (count > 0)
            {
                // Choca contra la pared: avanza hasta el borde y corta el knockback
                distance = Mathf.Max(0f, castHits[0].distance - Skin);
                knockbackTimer = 0f;
            }
        }

        rb.MovePosition(rb.position + dir * distance);
    }

    // ---------------- FLASH ----------------

    private void Flash()
    {
        if (flashMaterial == null || renderers.Length == 0) return;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (!isFlashing)
        {
            // Guardamos el estado actual (no el de Awake) por si otro script cambió color/material
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                savedMaterials[i] = renderers[i].sharedMaterial;
                savedColors[i] = renderers[i].color;
            }
        }

        isFlashing = true;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].sharedMaterial = flashMaterial;
            renderers[i].color = flashColor;
        }

        // Tiempo escalado a propósito: durante el hit stop el enemigo queda blanco y congelado
        yield return new WaitForSeconds(flashDuration);

        RestoreRenderers();
        flashRoutine = null;
    }

    private void RestoreRenderers()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].sharedMaterial = savedMaterials[i];
            renderers[i].color = savedColors[i];
        }

        isFlashing = false;
    }
}