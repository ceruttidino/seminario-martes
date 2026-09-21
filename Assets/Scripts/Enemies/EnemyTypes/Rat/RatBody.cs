using System.Collections;
using UnityEngine;

public class RatBody : MonoBehaviour, IDamageable
{
    [SerializeField] private GameObject ratPrefab;

    private float timer;
    private float reducedHealth;
    private bool regenerating;
    private HitShake hitShake;

    private void Awake()
    {
        hitShake = GetComponent<HitShake>();
        if (hitShake == null)
            hitShake = gameObject.AddComponent<HitShake>();
    }

    public void Init(float time, float reducedHp)
    {
        timer = time;
        reducedHealth = reducedHp;
    }

    private void Update()
    {
        if (regenerating) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
            StartCoroutine(RegenerateRoutine());
    }

    private IEnumerator RegenerateRoutine()
    {
        regenerating = true;

        if (hitShake != null)
            hitShake.Play(0.22f, 0.08f);

        yield return new WaitForSeconds(0.22f);

        Regenerate();
        Destroy(gameObject);
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

            RoomInstance room = GetComponentInParent<RoomInstance>();
            if (room != null)
                health.OnDeath += room.HandleEnemyDeath;
        }

        RegeneratingRat reviveScript = newRat.GetComponent<RegeneratingRat>();
        reviveScript?.BeginRevive();
    }

    public void TakeDamage(float dmg)
    {
        AudioManager.PlayEnemyHurt(EnemyType.Rat);

        RoomInstance room = GetComponentInParent<RoomInstance>();
        if (room != null)
            room.HandleEnemyDeath();

        BloodPool.Spawn(transform.position, GetComponentInParent<RoomInstance>()?.transform);
        Destroy(gameObject);
    }
}
