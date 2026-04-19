using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    [SerializeField] private UpgradeSO upgradeToGrant;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (upgradeToGrant == null) return;

        if (collision.CompareTag("Player"))
        {
            PlayerUpgradeManager manager = collision.GetComponent<PlayerUpgradeManager>();

            if (manager != null)
            {
                manager.CollectUpgrade(upgradeToGrant);
                Destroy(gameObject);   // Se destruye al recogerlo
            }
        }
    }
}