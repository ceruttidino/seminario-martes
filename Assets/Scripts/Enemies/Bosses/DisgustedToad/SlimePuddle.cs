using System.Collections;
using UnityEngine;

// NUEVO ARCHIVO
// Prefab: root con CircleCollider2D + SlimePuddle, hijo con el SpriteRenderer (sorting order bajo).
[RequireComponent(typeof(CircleCollider2D))]
public class SlimePuddle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private float defaultRadius = 5f;

    [Header("Efecto")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField][Range(0f, 1f)] private float slowPercent = 0.2f;
    [SerializeField] private float slowDuration = 3f;

    [Header("Vida")]
    [SerializeField] private float lifetime = 7f;
    [SerializeField] private float growDuration = 0.2f;
    [SerializeField] private float fadeDuration = 1f;

    private CircleCollider2D area;
    private Vector3 targetVisualScale = Vector3.one;
    private bool initialized;
    private GameObject playerInside;
    private int playerCollidersInside;
    private float nextDamageTime;

    public static SlimePuddle Spawn(SlimePuddle prefab, Vector3 position, float radius, Transform parent)
    {
        if (prefab == null) return null;
        SlimePuddle puddle = Instantiate(prefab, position, Quaternion.identity, parent);
        puddle.Initialize(radius);
        return puddle;
    }

    private void Awake()
    {
        area = GetComponent<CircleCollider2D>();
        area.isTrigger = true;
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (!initialized) Initialize(defaultRadius);
    }

    public void Initialize(float worldRadius)
    {
        if (initialized) return;
        initialized = true;

        area.radius = worldRadius / Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));

        if (visual != null && visual.sprite != null)
        {
            float currentWidth = visual.sprite.bounds.size.x * Mathf.Abs(visual.transform.lossyScale.x);
            if (currentWidth > 0.0001f)
                targetVisualScale = visual.transform.localScale * (worldRadius * 2f / currentWidth);
        }

        StartCoroutine(Lifecycle());
    }

    private IEnumerator Lifecycle()
    {
        float t = 0f;
        while (t < growDuration)
        {
            if (visual != null)
                visual.transform.localScale = targetVisualScale * Mathf.SmoothStep(0f, 1f, t / growDuration);
            t += Time.deltaTime;
            yield return null;
        }
        if (visual != null) visual.transform.localScale = targetVisualScale;

        yield return new WaitForSeconds(Mathf.Max(0f, lifetime - growDuration - fadeDuration));

        Color baseColor = visual != null ? visual.color : Color.white;
        t = 0f;
        while (t < fadeDuration)
        {
            if (visual != null)
                visual.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(baseColor.a, 0f, t / fadeDuration));
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    // Enter/Exit + Update en vez de OnTriggerStay2D: Stay deja de llamarse si el rigidbody del jugador se duerme
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;
        playerInside = health.gameObject;
        playerCollidersInside++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null) return;
        playerCollidersInside = Mathf.Max(0, playerCollidersInside - 1);
        if (playerCollidersInside == 0) playerInside = null;
    }

    private void Update()
    {
        if (playerInside == null) return;

        BossPlayerUtils.ApplySlow(playerInside, slowPercent, slowDuration);

        if (Time.time >= nextDamageTime)
        {
            BossPlayerUtils.DamagePlayer(playerInside, damage);
            nextDamageTime = Time.time + damageInterval;
        }
    }
}
