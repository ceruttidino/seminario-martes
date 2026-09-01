using System.Collections;
using UnityEngine;

public class HitShake : MonoBehaviour
{
    [SerializeField] private float duration = 0.12f;
    [SerializeField] private float magnitude = 0.05f;

    private Vector3 restLocalPosition;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        restLocalPosition = transform.localPosition;
    }

    public void Play()
    {
        Play(duration, magnitude);
    }

    public void Play(float shakeDuration, float shakeMagnitude)
    {
        if (!isActiveAndEnabled) return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(shakeDuration, shakeMagnitude));
    }

    private IEnumerator ShakeRoutine(float shakeDuration, float shakeMagnitude)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / shakeDuration);
            Vector2 offset = Random.insideUnitCircle * shakeMagnitude * damper;
            transform.localPosition = restLocalPosition + (Vector3)offset;
            yield return null;
        }

        transform.localPosition = restLocalPosition;
        shakeRoutine = null;
    }

    private void OnDisable()
    {
        transform.localPosition = restLocalPosition;
        shakeRoutine = null;
    }
}
