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

        GameObject newRat = Instantiate(ratPrefab, transform.position, Quaternion.identity, transform.parent);

        EnemyHealth health = newRat.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.SetCurrentHealth(reducedHealth);

            // Subscribe the new rat to the room so enemy death count stays accurate
            RoomInstance room = GetComponentInParent<RoomInstance>();
            if (room != null)
                health.OnDeath += room.HandleEnemyDeath;
        }
    }

    public void TakeDamage(float dmg)
    {
        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null)
            room.HandleEnemyDeath();

        Destroy(gameObject);
    }
}
