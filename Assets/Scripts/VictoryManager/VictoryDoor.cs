using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VictoryDoor : MonoBehaviour
{
    [Header("Configuraciùn")]
    [SerializeField] private bool destroyOnTouch = true;
    [SerializeField] private float appearDuration = 0.85f;

    private bool hasTriggered = false;
    private bool isInteractable = false;
    private Collider2D doorCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
        if (doorCollider != null)
        {
            doorCollider.isTrigger = true;
            doorCollider.enabled = false;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 0f;
            spriteRenderer.color = color;
        }

        StartCoroutine(AppearRoutine());
    }

    private IEnumerator AppearRoutine()
    {
        Vector3 targetScale = transform.localScale;
        transform.localScale = targetScale * 0.85f;

        float timer = 0f;
        while (timer < appearDuration)
        {
            float t = timer / appearDuration;
            float eased = t * t * (3f - 2f * t);

            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = eased;
                spriteRenderer.color = color;
            }

            transform.localScale = Vector3.Lerp(targetScale * 0.85f, targetScale, eased);

            timer += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        transform.localScale = targetScale;

        if (doorCollider != null)
            doorCollider.enabled = true;

        isInteractable = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInteractable || hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            VictoryManager victoryManager = FindFirstObjectByType<VictoryManager>();

            if (victoryManager != null)
            {
                victoryManager.TriggerVictory();
            }
            else
            {
                Debug.LogError("No se encontrù VictoryManager en la escena");
            }

            if (destroyOnTouch)
                Destroy(gameObject);
        }
    }
}
