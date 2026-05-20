using UnityEngine;

public class Mole : MonoBehaviour
{
    [SerializeField] private LayerMask player;
    [SerializeField] private PlayerHealth ph;
    [SerializeField] private float yoinkRange = 2f;
    [SerializeField] private bool canStealScrap;
    [SerializeField] private int ammountScrapStolen;
    [SerializeField] private bool canStealKey;

    private void Start()
    {
        Steal();
    }

    private void Steal()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, yoinkRange, player);
        if (hit == null) return;

        PlayerHealth health = hit.GetComponent<PlayerHealth>();
        if (health != null && health.CurrentHealth > 1)
            health.PlayerGetHurt();

        if (canStealScrap)
        {
            PlayerScrap scrap = hit.GetComponent<PlayerScrap>();
            if (scrap != null)
            {
                int amountToSteal = Mathf.Min(ammountScrapStolen, scrap.CurrentScrap);
                scrap.TrySpendScrap(amountToSteal);
            }
        }

        if (canStealKey)
        {
            PlayerKeys keys = hit.GetComponent<PlayerKeys>();
            if (keys != null && keys.CurrentKeys > 0)
                keys.UseKey();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, yoinkRange);
    }
}
