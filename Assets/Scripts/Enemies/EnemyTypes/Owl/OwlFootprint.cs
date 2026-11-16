using System.Collections;
using UnityEngine;

public class OwlFootprint : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float fadeTime = 1f;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        StartCoroutine(FadeAndDie());
    }

    private IEnumerator FadeAndDie()
    {
        // Espera antes de empezar a desvanecer (lifetime - fadeTime visible full).
        float hold = Mathf.Max(0f, lifetime - fadeTime);
        yield return new WaitForSeconds(hold);

        float t = 0f;
        float startAlpha = sr != null ? sr.color.a : 1f;
        float dur = Mathf.Max(0.01f, fadeTime);

        while (t < dur)
        {
            t += Time.deltaTime;
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(startAlpha, 0f, t / dur);
                sr.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}