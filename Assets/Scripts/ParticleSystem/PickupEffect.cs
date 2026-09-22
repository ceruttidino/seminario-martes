using UnityEngine;

public class PickupEffect : MonoBehaviour
{
    [Header("Flotación")]
    [SerializeField] private float floatAmplitude = 0.18f;
    [SerializeField] private float floatSpeed = 1.8f;

    [Header("Aparición")]
    [SerializeField] private float popDuration = 0.35f;

    [Header("Partículas al recoger")]
    [SerializeField] private bool spawnParticlesOnPickup = true;
    [Tooltip("Id del preset configurado en el ParticleEffectsManager (ej: scrap, lockpick).")]
    [SerializeField] private string particlePresetId = "scrap";
    [Tooltip("Offset respecto al centro del pickup donde nace el burst.")]
    [SerializeField] private Vector3 particleOffset = Vector3.zero;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private float startTime;
    private bool isBeingCollected = false;

    private LootPopMover popMover;
    private bool syncedAfterPop = false;

    private void Start()
    {
        popMover = GetComponent<LootPopMover>();

        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        startTime = Time.time;

        transform.localScale = Vector3.zero;
        StartCoroutine(PopInEffect());
    }

    private System.Collections.IEnumerator PopInEffect()
    {
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / popDuration;
            float scale = Mathf.Sin(progress * Mathf.PI * 0.5f) * 1.35f;
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    private void Update()
    {
        if (isBeingCollected) return;

        if (popMover != null && popMover.IsPopping) return;

        if (popMover != null && !syncedAfterPop)
        {
            originalPosition = transform.localPosition;
            startTime = Time.time;
            syncedAfterPop = true;
        }

        float floatOffset = Mathf.Sin((Time.time - startTime) * floatSpeed) * floatAmplitude;
        transform.localPosition = originalPosition + new Vector3(0, floatOffset, 0);
    }

    public void OnPickup()
    {
        OnPickup(null);
    }

    /// <summary>
    /// </summary>
    /// <param name="collector">Transform del que recoge (el jugador). Se usa en los presets con atracción.</param>
    public void OnPickup(Transform collector)
    {
        if (isBeingCollected) return;
        isBeingCollected = true;

        SpawnPickupParticles(collector);
        StartCoroutine(PickupAnimation());
    }

    private void SpawnPickupParticles(Transform collector)
    {
        if (!spawnParticlesOnPickup) return;
        if (string.IsNullOrEmpty(particlePresetId)) return;

        if (ParticleEffectsManager.Instance == null)
        {
            Debug.LogWarning("[PickupEffect] No hay ParticleEffectsManager en la escena.", this);
            return;
        }

        ParticleEffectsManager.Instance.PlayPickup(
            particlePresetId,
            transform.position + particleOffset,
            collector);
    }

    private System.Collections.IEnumerator PickupAnimation()
    {
        float elapsed = 0f;
        float duration = 0.28f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float scale = Mathf.Lerp(1f, 0f, progress * 1.2f);
            transform.localScale = startScale * scale;
            yield return null;
        }

        Destroy(gameObject);
    }
}
