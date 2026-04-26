using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class PoisonTile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float damageCooldown = 0.8f;

    [Header("Lifetime")]
    [SerializeField] private float minLifetime = 2f;
    [SerializeField] private float maxLifetime = 4f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float fadeOutDuration = 0.4f;

    private float lastDamageTime = -999f;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        float lifetime = Random.Range(minLifetime, maxLifetime);
        StartCoroutine(LifetimeRoutine(lifetime));
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < lastDamageTime + damageCooldown) return;

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            lastDamageTime = Time.time;
        }
    }

    private IEnumerator LifetimeRoutine(float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        yield return FadeOut();
        Destroy(gameObject);
    }

    private IEnumerator FadeOut()
    {
        if (spriteRenderer == null) yield break;

        float timer = 0f;
        Color startColor = spriteRenderer.color;

        while (timer < fadeOutDuration)
        {
            float alpha = Mathf.Lerp(startColor.a, 0f, timer / fadeOutDuration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }
    }
}
