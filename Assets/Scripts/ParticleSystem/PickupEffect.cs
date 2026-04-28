using UnityEngine;

public class PickupEffect : MonoBehaviour
{
    [Header("Flotación")]
    [SerializeField] private float floatAmplitude = 0.18f;
    [SerializeField] private float floatSpeed = 1.8f;

    [Header("Aparición")]
    [SerializeField] private float popDuration = 0.35f;

    private Vector3 originalPosition;
    private Vector3 originalScale;
    private float startTime;
    private bool isBeingCollected = false;

    private void Start()
    {
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

        float floatOffset = Mathf.Sin((Time.time - startTime) * floatSpeed) * floatAmplitude;
        transform.localPosition = originalPosition + new Vector3(0, floatOffset, 0);
    }

    public void OnPickup()
    {
        if (isBeingCollected) return;
        isBeingCollected = true;

        StartCoroutine(PickupAnimation());
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