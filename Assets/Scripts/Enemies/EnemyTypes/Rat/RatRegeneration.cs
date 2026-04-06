using UnityEngine;

public class RatRegeneration : MonoBehaviour
{
    [SerializeField] private GameObject bodyPrefab;
    [SerializeField] private GameObject ratPrefab;
    [SerializeField] private float regenTime = 3f;
    [SerializeField] private float reducedHealth = 3f;

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
        GameObject body = Instantiate(bodyPrefab, transform.position, Quaternion.identity);

        RatBody bodyScript = body.GetComponent<RatBody>();
        bodyScript.Init(regenTime, reducedHealth);
    }
}
