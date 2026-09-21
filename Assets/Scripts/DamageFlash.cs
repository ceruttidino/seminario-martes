using System.Collections;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.05f;
    [SerializeField] private int flashCount = 2;

    private Color originalColor;
    private Color persistentTint = Color.white;
    private Coroutine flashRoutine;

    public bool IsFlashing => flashRoutine != null;
    public float TotalFlashDuration => Mathf.Max(0.01f, flashCount) * Mathf.Max(0.01f, flashDuration) * 2f;

    private void Awake()
    {
        if(spriteRenderer  == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        originalColor = spriteRenderer.color;
        persistentTint = originalColor;
    }

    public void SetPersistentTint(Color tint)
    {
        persistentTint = tint;
        if (spriteRenderer != null && flashRoutine == null && loopRoutine == null)
            spriteRenderer.color = persistentTint;
    }

    public void Flash()
    {
        Flash(TotalFlashDuration);
    }

    public void Flash(float totalDuration)
    {
        if (spriteRenderer == null) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(totalDuration));
    }

    public void StartLoopFlash()
    {
        StopLoopFlash();
        loopRoutine = StartCoroutine(LoopFlashRoutine());
    }

    public void StopLoopFlash()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = persistentTint;
    }

    private Coroutine loopRoutine;

    private IEnumerator FlashRoutine(float totalDuration)
    {
        float duration = Mathf.Max(flashDuration * 2f, totalDuration);
        float pulse = Mathf.Max(0.06f, flashDuration);
        float elapsed = 0f;
        bool red = true;

        while (elapsed < duration)
        {
            spriteRenderer.color = red ? flashColor : persistentTint;
            red = !red;
            yield return new WaitForSeconds(pulse);
            elapsed += pulse;
        }

        spriteRenderer.color = persistentTint;
        flashRoutine = null;
    }

    private IEnumerator LoopFlashRoutine()
    {
        while (true)
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            spriteRenderer.color = persistentTint;
            yield return new WaitForSeconds(flashDuration);
        }
    }
}
