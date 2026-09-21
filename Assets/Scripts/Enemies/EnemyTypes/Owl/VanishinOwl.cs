using UnityEngine;
using System.Collections;

public class VanishingOwl : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float stalkSpeed = 3.5f;
    [Tooltip("Si esta activo, vuela recto ignorando el esquive de obstaculos de EnemyMovement.")]
    [SerializeField] private bool flyStraight = false;

    public enum OwlStartForm { Invisible, Shadow, Revealed }

    [Header("Spawn")]
    [Tooltip("En que forma aparece el owl al spawnear.")]
    [SerializeField] private OwlStartForm startForm = OwlStartForm.Invisible;
    public OwlStartForm StartForm => startForm;
    [Tooltip("Cuanto se queda visible y quieto al spawnear en forma Revealed, antes de entrar al ciclo.")]
    [SerializeField] private float revealedSpawnDuration = 2f;
    public float RevealedSpawnDuration => revealedSpawnDuration;

    [Header("Distances")]
    [SerializeField] private float revealDistance = 1.6f;
    [SerializeField] private float attackRadius = 1.9f;

    [Header("Timing")]
    [SerializeField] private float windupTime = 0.4f;
    [SerializeField] private float recoverTime = 0.6f;

    [Header("Stalk Cycle")]
    [SerializeField] private float invisibleDuration = 3f;
    [SerializeField] private float shadowChaseDuration = 1.5f;

    [Header("Damage")]
    [Tooltip("2 = un corazon entero (en tu PlayerHealth 1 corazon = 2 puntos).")]
    [SerializeField] private float heartDamage = 2f;

    [Header("Visuals")]
    [Tooltip("El cuerpo normal, visible al revelarse/atacar. SpriteRenderer de la raiz.")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [Tooltip("La shadow form (buho oscuro + ojos). Objeto 'Eyes' en la jerarquia.")]
    [SerializeField] private GameObject shadowFormObject;
    [Tooltip("La sombra proyectada ('Shadow').")]
    [SerializeField] private GameObject projectedShadow;
    [SerializeField] private Animator animator;

    [Header("Refs")]
    [SerializeField] private EnemyMovement movement;

    [Header("Fade")]
    [Tooltip("Cuanto tarda en aparecer (rapido, al atacar/revelarse).")]
    [SerializeField] private float fadeInTime = 0.15f;
    [Tooltip("Cuanto tarda en desvanecerse (gradual, al ocultarse).")]
    [SerializeField] private float fadeOutTime = 0.4f;

    private SpriteRenderer[] bodyGroup;
    private SpriteRenderer[] shadowGroup;
    private SpriteRenderer[] projGroup;

    private Coroutine bodyFade, shadowFade, projFade;

    public float RevealDistance => revealDistance;
    public float AttackRadius => attackRadius;
    public float WindupTime => windupTime;
    public float RecoverTime => recoverTime;
    public float HeartDamage => heartDamage;
    public float InvisibleDuration => invisibleDuration;
    public float ShadowChaseDuration => shadowChaseDuration;

    private void Awake()
    {
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (animator == null) animator = GetComponent<Animator>();
        if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();

        bodyGroup = bodyRenderer != null ? new[] { bodyRenderer } : new SpriteRenderer[0];
        shadowGroup = shadowFormObject != null
            ? shadowFormObject.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];
        projGroup = projectedShadow != null
            ? projectedShadow.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];
    }

    public void MoveTowards(Vector2 target)
    {
        if (movement == null) return;

        if (flyStraight)
        {
            Vector2 dir = (target - (Vector2)transform.position).normalized;
            movement.Move(dir, stalkSpeed, false);
            return;
        }

        movement.MoveTowards(target, stalkSpeed);
    }

    public void Stop()
    {
        if (movement != null) movement.Move(Vector2.zero);
    }

    public void FaceTarget(Vector2 target)
    {
        if (movement == null) return;
        movement.Face(target - (Vector2)transform.position);
    }


    public void TriggerAttack()
    {
        if (animator != null) animator.SetTrigger("Attack");
    }

    public void ResetAttackTrigger()
    {
        if (animator != null) animator.ResetTrigger("Attack");
    }

    public void EnterInvisible()
    {
        if (animator != null) animator.SetBool("IsStealthed", true);
        ResetAttackTrigger();
        FadeTo(bodyGroup, ref bodyFade, 0f, fadeOutTime);
        FadeTo(shadowGroup, ref shadowFade, 0f, fadeOutTime);
        FadeTo(projGroup, ref projFade, 0f, fadeOutTime);
    }

    public void EnterShadow()
    {
        if (animator != null) animator.SetBool("IsStealthed", true);
        FadeTo(bodyGroup, ref bodyFade, 0f, fadeOutTime);
        FadeTo(shadowGroup, ref shadowFade, 1f, fadeInTime);
        FadeTo(projGroup, ref projFade, 1f, fadeInTime);
    }

    public void Reveal()
    {
        if (animator != null) animator.SetBool("IsStealthed", false);
        FadeTo(bodyGroup, ref bodyFade, 1f, fadeInTime);
        FadeTo(shadowGroup, ref shadowFade, 0f, fadeOutTime);
        FadeTo(projGroup, ref projFade, 1f, fadeInTime);
    }

    private void FadeTo(SpriteRenderer[] group, ref Coroutine handle, float target, float duration)
    {
        if (group == null || group.Length == 0) return;
        if (handle != null) StopCoroutine(handle);
        handle = StartCoroutine(FadeRoutine(group, target, duration));
    }

    private IEnumerator FadeRoutine(SpriteRenderer[] group, float target, float duration)
    {
        // Reactiva los renderers para poder fadear.
        foreach (var r in group)
            if (r != null) r.enabled = true;

        float start = group[0] != null ? group[0].color.a : 0f;
        float dur = Mathf.Max(0.01f, duration);
        float t = 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            SetGroupAlpha(group, Mathf.Lerp(start, target, t / dur));
            yield return null;
        }
        SetGroupAlpha(group, target);

        // Si quedo invisible, apaga el dibujado.
        if (target <= 0f)
            foreach (var r in group)
                if (r != null) r.enabled = false;
    }

    private void SetGroupAlpha(SpriteRenderer[] group, float a)
    {
        foreach (var r in group)
        {
            if (r == null) continue;
            Color c = r.color;
            c.a = a;
            r.color = c;
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, revealDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}