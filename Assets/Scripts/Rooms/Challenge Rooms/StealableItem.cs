using UnityEngine;

/// <summary>
/// El item que el jugador tiene que robar. Va adentro de la room de challenge,
/// con un Collider2D en trigger.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StealableItem : MonoBehaviour
{
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite takenSprite;
    [Tooltip("Si no hay takenSprite, esconde el objeto al ser robado.")]
    [SerializeField] private bool hideWhenTaken = true;
    [Tooltip("Opcional: partículas o brillo que marca el objetivo.")]
    [SerializeField] private GameObject highlight;

    private StealTheItemChallenge controller;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private bool taken;
    private bool armed;

    public bool Taken => taken;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        bodyCollider = GetComponent<Collider2D>();
        if (bodyCollider != null)
            bodyCollider.isTrigger = true;
    }

    public void Prepare(StealTheItemChallenge owner)
    {
        controller = owner;
        taken = false;
        armed = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;

            if (idleSprite != null)
                spriteRenderer.sprite = idleSprite;
        }

        if (bodyCollider != null)
            bodyCollider.enabled = true;

        if (highlight != null)
            highlight.SetActive(true);
    }

    public void MarkStolen()
    {
        taken = true;
        armed = false;

        if (bodyCollider != null)
            bodyCollider.enabled = false;

        if (highlight != null)
            highlight.SetActive(false);

        if (spriteRenderer == null)
            return;

        if (takenSprite != null)
        {
            spriteRenderer.sprite = takenSprite;
            return;
        }

        if (hideWhenTaken)
            spriteRenderer.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!armed || taken)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (GamePause.IsGameplayFrozen)
            return;

        armed = false;

        if (controller != null)
            controller.NotifyItemStolen();
        else
            MarkStolen();
    }
}

