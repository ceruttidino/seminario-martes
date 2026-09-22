using System.Collections.Generic;
using UnityEngine;

public class ParticleEffectsManager : MonoBehaviour
{
    public static ParticleEffectsManager Instance { get; private set; }

    [System.Serializable]
    public class PickupBurstPreset
    {
        [Tooltip("Identificador que usa el pickup para pedir este burst (ej: scrap, lockpick, heart).")]
        public string id = "scrap";

        [Header("Look")]
        [Tooltip("Opcional. Si queda vacío se usa un círculo generado por código.")]
        public Sprite sprite;
        public Color colorA = Color.white;
        public Color colorB = Color.white;

        [Header("Cantidad")]
        public int minCount = 6;
        public int maxCount = 10;
        [Tooltip("Radio alrededor del pickup donde nacen las partículas.")]
        public float spawnRadius = 0.12f;

        [Header("Movimiento")]
        public float minSpeed = 2f;
        public float maxSpeed = 4.5f;
        [Range(0f, 1f)]
        [Tooltip("0 = se dispersan en todas direcciones, 1 = todas salen hacia arriba.")]
        public float upwardBias = 0.45f;
        public float gravity = 9f;
        public float drag = 2.5f;
        public float maxRotationSpeed = 220f;

        [Header("Vida y tamaño")]
        public float minLifetime = 0.35f;
        public float maxLifetime = 0.65f;
        public float minSize = 0.12f;
        public float maxSize = 0.22f;
        [Range(0f, 1f)] public float endScaleMultiplier = 0.15f;
        [Range(0f, 1f)] public float fadeStart = 0.45f;

        [Header("Atracción hacia el jugador")]
        [Tooltip("Si está activo, las partículas vuelan hacia quien recogió el item en vez de caer.")]
        public bool attractToCollector = false;
        public float attractForce = 35f;
    }

    [Header("Presets")]
    [SerializeField]
    private List<PickupBurstPreset> presets = new List<PickupBurstPreset>
    {
        new PickupBurstPreset
        {
            id = "scrap",
            colorA = new Color(0.78f, 0.78f, 0.82f, 1f),
            colorB = new Color(1f, 0.72f, 0.32f, 1f),
            minCount = 7, maxCount = 11,
            minSpeed = 2f, maxSpeed = 4.5f,
            gravity = 10f
        },
        new PickupBurstPreset
        {
            id = "lockpick",
            colorA = new Color(1f, 0.85f, 0.35f, 1f),
            colorB = new Color(1f, 0.97f, 0.75f, 1f),
            minCount = 9, maxCount = 14,
            minSpeed = 1.6f, maxSpeed = 3.6f,
            gravity = 3f,
            minLifetime = 0.45f, maxLifetime = 0.8f,
            attractToCollector = true
        }
    };

    [Header("Pool")]
    [SerializeField] private int initialPoolSize = 60;
    [SerializeField] private bool expandPoolIfNeeded = true;

    [Header("Render")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 50;

    private readonly Queue<PickupParticle> pool = new Queue<PickupParticle>();
    private readonly Dictionary<string, PickupBurstPreset> presetLookup = new Dictionary<string, PickupBurstPreset>();
    private Sprite fallbackSprite;
    private Transform poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildLookup();
        BuildPool();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildLookup()
    {
        presetLookup.Clear();
        foreach (PickupBurstPreset preset in presets)
        {
            if (preset == null || string.IsNullOrEmpty(preset.id)) continue;
            presetLookup[preset.id.ToLowerInvariant()] = preset;
        }
    }

    private void BuildPool()
    {
        poolRoot = new GameObject("PickupParticles").transform;
        poolRoot.SetParent(transform);

        for (int i = 0; i < initialPoolSize; i++)
            pool.Enqueue(CreateParticle());
    }

    private PickupParticle CreateParticle()
    {
        GameObject go = new GameObject("PickupParticle");
        go.transform.SetParent(poolRoot);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        PickupParticle particle = go.AddComponent<PickupParticle>();
        go.SetActive(false);
        return particle;
    }

    /// <summary>
    /// Dispara un burst de partículas en una posición del mundo.
    /// </summary>
    /// <param name="presetId">Id del preset (scrap, lockpick, etc).</param>
    /// <param name="position">Posición en world space.</param>
    /// <param name="collector">Opcional: transform del jugador, para presets con atracción.</param>
    public void PlayPickup(string presetId, Vector3 position, Transform collector = null)
    {
        if (string.IsNullOrEmpty(presetId)) return;

        if (!presetLookup.TryGetValue(presetId.ToLowerInvariant(), out PickupBurstPreset preset))
        {
            Debug.LogWarning($"[ParticleEffectsManager] No existe el preset '{presetId}'.", this);
            return;
        }

        PlayPickup(preset, position, collector);
    }

    public void PlayPickup(PickupBurstPreset preset, Vector3 position, Transform collector = null)
    {
        if (preset == null) return;

        int count = Random.Range(preset.minCount, preset.maxCount + 1);
        Transform target = preset.attractToCollector ? collector : null;

        for (int i = 0; i < count; i++)
            SpawnOne(preset, position, target);
    }

    private void SpawnOne(PickupBurstPreset preset, Vector3 position, Transform target)
    {
        PickupParticle particle = GetFromPool();
        if (particle == null) return;

        // Posición inicial dentro de un pequeño radio
        Vector2 offset = Random.insideUnitCircle * preset.spawnRadius;
        particle.transform.position = position + (Vector3)offset;

        // Dirección: círculo completo, sesgado hacia arriba según upwardBias
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        if (randomDir == Vector2.zero) randomDir = Vector2.up;
        Vector2 dir = Vector2.Lerp(randomDir, Vector2.up, preset.upwardBias).normalized;
        float speed = Random.Range(preset.minSpeed, preset.maxSpeed);

        SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();
        sr.sprite = preset.sprite != null ? preset.sprite : GetFallbackSprite();
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        PickupParticle.Settings settings = new PickupParticle.Settings
        {
            velocity = dir * speed,
            gravity = preset.gravity,
            drag = preset.drag,
            rotationSpeed = Random.Range(-preset.maxRotationSpeed, preset.maxRotationSpeed),
            lifetime = Random.Range(preset.minLifetime, preset.maxLifetime),
            startScale = Random.Range(preset.minSize, preset.maxSize),
            endScaleMultiplier = preset.endScaleMultiplier,
            color = Color.Lerp(preset.colorA, preset.colorB, Random.value),
            fadeStart = preset.fadeStart,
            attractTarget = target,
            attractForce = preset.attractForce
        };

        particle.gameObject.SetActive(true);
        particle.Play(settings, () => ReturnToPool(particle));
    }

    private PickupParticle GetFromPool()
    {
        if (pool.Count > 0) return pool.Dequeue();
        if (!expandPoolIfNeeded) return null;
        return CreateParticle();
    }

    private void ReturnToPool(PickupParticle particle)
    {
        if (particle == null) return;
        particle.gameObject.SetActive(false);
        particle.transform.SetParent(poolRoot);
        pool.Enqueue(particle);
    }

    /// <summary>
    /// Sprite circular generado por código, para no depender de un asset.
    /// </summary>
    private Sprite GetFallbackSprite()
    {
        if (fallbackSprite != null) return fallbackSprite;

        const int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2(size * 0.5f - 0.5f, size * 0.5f - 0.5f);
        float radius = size * 0.5f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        fallbackSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return fallbackSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) BuildLookup();
    }
#endif
}

