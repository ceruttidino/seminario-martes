using UnityEngine;

public class PoisonWarning : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Blink")]
    [SerializeField] private float minAlpha = 0.15f;
    [SerializeField] private float maxAlpha = 0.65f;
    [SerializeField] private float blinksPerSecond = 3f;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.6f;
    [SerializeField] private float endScale = 1f;

    private float duration = 0.8f;
    private float timer;
    private Color baseColor = Color.white;
    private Vector3 targetScale;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;

        targetScale = transform.localScale;
    }

    public void Play(float warningDuration)
    {
        duration = Mathf.Max(0.01f, warningDuration);
        timer = 0f;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        transform.localScale = targetScale * Mathf.Lerp(startScale, endScale, t);

        if (spriteRenderer != null)
        {
            float blink = Mathf.PingPong(timer * blinksPerSecond * 2f, 1f);
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, blink);
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        if (timer >= duration)
            Destroy(gameObject);
    }
}
