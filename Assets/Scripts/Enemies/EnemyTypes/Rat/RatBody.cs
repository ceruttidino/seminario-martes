using UnityEngine;

public class RatBody : MonoBehaviour, IDamageable
{
    [SerializeField] private GameObject ratPrefab;

    private float timer;
    private float reducedHealth;

    public void Init(float time, float reducedHp)
    {
        timer = time;
        reducedHealth = reducedHp;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            Regenerate();
            Destroy(gameObject);
        }
    }

    private void Regenerate()
    {
        if (ratPrefab == null)
        {
            Debug.LogError("ratPrefab NO asignado en RatBody");
            return;
        }

        GameObject newRat = Instantiate(ratPrefab, transform.position, Quaternion.identity);

        EnemyHealth health = newRat.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.SetCurrentHealth(reducedHealth);
        }
    }

    public void TakeDamage(float dmg)
    {
        Destroy(gameObject);
    }
}
