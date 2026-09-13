using System.Collections;
using UnityEditor.UIElements;
using UnityEngine;

public class AuraHandler : MonoBehaviour
{
    [SerializeField] private GameObject aura;

    [SerializeField] private float AuraDuration;

    SpriteRenderer auraRenderer;

    [SerializeField] float auraDuration = 1;

    private void Awake()
    {
        auraRenderer = aura.GetComponent<SpriteRenderer>();
    }

    public void ActivateAura(Color colorin)
    {
        auraRenderer.color = new Color(colorin.r, colorin.g, colorin.b, 1);

        StartCoroutine(AuraFade(colorin, auraDuration));
        
    }

    IEnumerator AuraFade(Color color, float duration)
    {
        Color startColor = color;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startColor.a, 0, elapsedTime / duration);
            auraRenderer.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            yield return null;
        }
    }

}
