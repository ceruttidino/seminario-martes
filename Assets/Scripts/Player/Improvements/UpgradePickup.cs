using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    [SerializeField] private UpgradeSO upgradeToGrant;
    [SerializeField] private AudioSource sfxSource;


    private void Start()
    {
        if (GetComponent<PickupEffect>() == null)
            gameObject.AddComponent<PickupEffect>();
    }

    public void SetUpgrade(UpgradeSO upgrade)
    {
        upgradeToGrant = upgrade;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {


        if (upgradeToGrant == null) return;
        if (!collision.CompareTag("Player")) return;

        PlayerUpgradeManager manager = collision.GetComponent<PlayerUpgradeManager>();
        if (manager != null)
        {

            if (sfxSource != null)
            {
                sfxSource.Play();
            }

            manager.CollectUpgrade(upgradeToGrant);

            PickupEffect effect = GetComponent<PickupEffect>();
            if (effect != null)
                effect.OnPickup();
            else
                Destroy(gameObject);
        }
    }
}