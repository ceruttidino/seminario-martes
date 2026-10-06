using System.Collections;
using UnityEngine;

public class SlimePuddle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private float defaultRadius = 5f;

    [Header("Ajuste del área")] // NUEVO
    [Tooltip("Qué parte del ancho del sprite es baba visible (sin bordes transparentes). 1 = el sprite completo.")]
    [SerializeField][Range(0.3f, 1f)] private float spriteContentFill = 1f; // NUEVO
    [Tooltip("Margen a favor del jugador: 0.9 = el daño empieza un 10% adentro del borde.")]
    [SerializeField][Range(0.5f, 1f)] private float hitRadiusMultiplier = 0.9f; // NUEVO

    [Header("Efecto")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField][Range(0f, 1f)] private float slowPercent = 0.2f;
    [SerializeField] private float slowDuration = 3f;

    [Header("Vida")]
    [SerializeField] private float lifetime = 7f;
    [SerializeField] private float growDuration = 0.2f;
    [SerializeField] private float fadeDuration = 1f;

    private float radius;                 
    private float growProgress;           
    private bool isActive;                
    private Vector3 targetVisualScale = Vector3.one;
    private bool initialized;

    private Transform player;            
    private GameObject playerHealthObject; 
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
        if (visual == null)
            visual = GetComponentInChildren<SpriteRenderer>();

        Collider2D oldCollider = GetComponent<Collider2D>();
        if (oldCollider != null)
            oldCollider.enabled = false;
    }

    private void Start()
    {
        if (!initialized)
            Initialize(defaultRadius);
    }

    public void Initialize(float worldRadius)
    {
        if (initialized) return;
        initialized = true;

        radius = worldRadius;

        if (visual != null && visual.sprite != null)
        {
            float currentWidth = visual.sprite.bounds.size.x * Mathf.Abs(visual.transform.lossyScale.x);
            float visibleWidth = currentWidth * spriteContentFill;
            if (visibleWidth > 0.0001f)
                targetVisualScale = visual.transform.localScale * (worldRadius * 2f / visibleWidth);
        }

        isActive = true; 
        FindPlayer();    
        StartCoroutine(Lifecycle());
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;

        player = playerObject.transform;

        PlayerHealth health = playerObject.GetComponentInParent<PlayerHealth>();
        playerHealthObject = health != null ? health.gameObject : playerObject;
    }

    private IEnumerator Lifecycle()
    {
        float t = 0f;
        while (t < growDuration)
        {
            growProgress = Mathf.SmoothStep(0f, 1f, t / growDuration);

            if (visual != null)
                visual.transform.localScale = targetVisualScale * growProgress;

            t += Time.deltaTime;
            yield return null;
        }

        growProgress = 1f; 
        if (visual != null)
            visual.transform.localScale = targetVisualScale;

        yield return new WaitForSeconds(Mathf.Max(0f, lifetime - growDuration - fadeDuration));

        isActive = false; 

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

    private void Update() 
    {
        if (!isActive) return;

        if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float effectiveRadius = radius * growProgress * hitRadiusMultiplier;
        Vector2 offset = (Vector2)player.position - (Vector2)transform.position;
        if (offset.sqrMagnitude > effectiveRadius * effectiveRadius)
            return;

        BossPlayerUtils.ApplySlow(playerHealthObject, slowPercent, slowDuration);

        if (Time.time >= nextDamageTime)
        {
            if (BossPlayerUtils.DamagePlayer(playerHealthObject, damage))
                nextDamageTime = Time.time + damageInterval;
        }
    }

    private void OnDrawGizmosSelected()
    {
        float r = initialized ? radius : defaultRadius;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, r * hitRadiusMultiplier);
    }
}
