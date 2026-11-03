using UnityEngine;

public class RatRegeneration : MonoBehaviour
{
    [SerializeField] private GameObject bodyPrefab;
    [SerializeField] private GameObject ratPrefab;
    [SerializeField] private float regenTime = 3f;
    [SerializeField] private float reducedHealth = 3f;
    [Tooltip("Que parte de la animacion de muerte (0-1) pasa antes de que aparezca el cuerpo.")]
    [SerializeField, Range(0f, 1f)] private float bodyRevealPoint = 0.6f;
    public bool isRegenerating = true;

    private EnemyHealth health;

    private GameObject cachedPrefab;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();

        cachedPrefab = ratPrefab != null ? ratPrefab : gameObject;

        health.OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        if (!isRegenerating || !enabled) return;

        GameObject body = Instantiate(bodyPrefab, transform.position, Quaternion.identity, transform.parent);

        RatBody bodyScript = body.GetComponent<RatBody>();
        bodyScript.Init(regenTime, reducedHealth);

        // Con animacion de muerte (hoja RataDerrota): el cuerpo aparece cuando la rata ya se esta desvaneciendo.
        EnemyDeathAnimation deathAnimation = GetComponent<EnemyDeathAnimation>();
        if (deathAnimation != null && deathAnimation.HasFrames)
            bodyScript.HideVisualFor(deathAnimation.Duration * bodyRevealPoint);
    }
}
