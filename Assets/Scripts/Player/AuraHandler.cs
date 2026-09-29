using System.Collections;
using UnityEngine;

public class AuraHandler : MonoBehaviour
{
    [SerializeField] private GameObject aura;
    [SerializeField] private float auraDuration = 1f;

    private SpriteRenderer auraRenderer;

    private void Awake()
    {
        if (aura != null)
            auraRenderer = aura.GetComponent<SpriteRenderer>();
    }

    public void ActivateAura(Color colorin)
    {
        if (auraRenderer == null)
            return;

        auraRenderer.color = new Color(colorin.r, colorin.g, colorin.b, 1f);
        StartCoroutine(AuraFade(colorin, auraDuration));
    }

    private IEnumerator AuraFade(Color color, float duration)
    {
        Color startColor = color;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startColor.a, 0f, elapsedTime / duration);
            auraRenderer.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            yield return null;
        }
    }
}
