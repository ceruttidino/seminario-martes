using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private float defaultDuration = 0.14f;
    [SerializeField] private float defaultMagnitude = 0.11f;

    private Vector3 restLocalPosition;
    private Coroutine shakeRoutine;

    private void Awake()
    {
        restLocalPosition = transform.localPosition;
    }

    public static void Play(float duration = -1f, float magnitude = -1f)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        CameraShake shake = cam.GetComponent<CameraShake>();
        if (shake == null)
            shake = cam.gameObject.AddComponent<CameraShake>();

        float usedDuration = duration > 0f ? duration : shake.defaultDuration;
        float usedMagnitude = magnitude > 0f ? magnitude : shake.defaultMagnitude;
        shake.Shake(usedDuration, usedMagnitude);
    }

    public void Shake(float duration, float magnitude)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 offset = Random.insideUnitCircle * magnitude * damper;
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
