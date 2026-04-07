using UnityEngine;

public class SlimeTile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float damage = 0.5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            IDamageable dmg = other.GetComponent<IDamageable>();

            if (dmg != null)
            {
                dmg.TakeDamage(damage);
            }
        }
    }
}
