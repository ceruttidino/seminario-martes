using System.Collections;
using UnityEngine;

public enum StealTrapMode
{
    /// <summary>Alerta a todos los guardias, pero no lastima.</summary>
    Alarm,
    /// <summary>Cuenta como golpe directo: el jugador pierde el challenge.</summary>
    Hit
}

/// <summary>
/// Trampa del challenge "Steal the Item". Collider2D en trigger, colocada en el
/// camino hacia el item.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StealChallengeTrap : MonoBehaviour
{
    [SerializeField] private StealTrapMode mode = StealTrapMode.Alarm;
    [Tooltip("Segundos hasta que la trampa se vuelve a armar. Ignorado si es oneShot.")]
    [SerializeField] private float rearmTime = 1.5f;
    [SerializeField] private bool oneShot = false;

    [Header("Visual (opcional)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite armedSprite;
    [SerializeField] private Sprite triggeredSprite;

    private StealTheItemChallenge controller;
    private Collider2D bodyCollider;
    private bool armed;
    private bool used;

    private void Awake()
    {
        bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider != null)
            bodyCollider.isTrigger = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Prepare(StealTheItemChallenge owner)
    {
        controller = owner;
        used = false;
        armed = true;
        ApplyVisual(true);
    }

    public void Deactivate()
    {
        armed = false;
        ApplyVisual(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!armed || used)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (GamePause.IsGameplayFrozen)
            return;

        if (controller == null)
            return;

        armed = false;
        ApplyVisual(false);

        if (mode == StealTrapMode.Hit)
        {
            used = true;
            controller.NotifyPlayerHit(other.gameObject);
            return;
        }

        controller.AlertAllGuards(transform.position);

        if (oneShot)
        {
            used = true;
            return;
        }

        StartCoroutine(RearmRoutine());
    }

    private IEnumerator RearmRoutine()
    {
        yield return new WaitForSeconds(rearmTime);

        if (used)
            yield break;

        armed = true;
        ApplyVisual(true);
    }

    private void ApplyVisual(bool isArmed)
    {
        if (spriteRenderer == null)
            return;

        Sprite target = isArmed ? armedSprite : triggeredSprite;

        if (target != null)
            spriteRenderer.sprite = target;
    }
}

