using UnityEngine;

public class UpgradePickup : MonoBehaviour
{
    [SerializeField] private UpgradeSO upgradeToGrant;
    [SerializeField] private AudioSource sfxSource;

    private bool collected;
    private UpgradeSO claimed;

    public UpgradeSO Upgrade => upgradeToGrant;

    /// <summary>True si todavia se puede generar un item de este pickup (no lo tenemos ni esta reservado en otro lado).</summary>
    public bool IsAvailableToSpawn()
    {
        return upgradeToGrant == null || UpgradeRegistry.CanSpawn(upgradeToGrant);
    }

    private void Start()
    {
        if (GetComponent<PickupEffect>() == null)
            gameObject.AddComponent<PickupEffect>();

        // Pickup puesto a mano en un prefab (o con el upgrade por defecto): se reserva ahora.
        // Si el item ya lo tenemos o ya esta en otro lado del nivel, este pickup no tiene que existir.
        if (claimed == null && !ClaimCurrent())
            Destroy(gameObject);
    }

    public void SetUpgrade(UpgradeSO upgrade)
    {
        ReleaseClaim();
        upgradeToGrant = upgrade;
        ClaimCurrent();
    }

    private bool ClaimCurrent()
    {
        if (upgradeToGrant == null)
            return true;

        UpgradeSO real = UpgradeRegistry.Resolve(upgradeToGrant);
        if (!UpgradeRegistry.TryReserve(real, this))
            return false;

        claimed = real;
        return true;
    }

    private void ReleaseClaim()
    {
        if (claimed == null)
            return;

        UpgradeRegistry.Release(claimed, this);
        claimed = null;
    }

    private void OnDestroy()
    {
        // Si se destruye sin que lo hayan recogido (cambio de piso, etc.) el item vuelve a estar disponible.
        if (!collected)
            ReleaseClaim();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryCollect(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryCollect(collision);
    }

    private void TryCollect(Collider2D collision)
    {
        if (collected || upgradeToGrant == null) return;

        PlayerUpgradeManager manager = collision.GetComponentInParent<PlayerUpgradeManager>();
        if (manager == null && collision.attachedRigidbody != null)
            manager = collision.attachedRigidbody.GetComponent<PlayerUpgradeManager>();
        if (manager == null) return;

        // Cada item se puede tener una sola vez: si ya lo tiene, este pickup sobra.
        if (manager.HasUpgrade(upgradeToGrant))
        {
            collected = true;
            Destroy(gameObject);
            return;
        }

        if (!manager.CollectUpgrade(upgradeToGrant))
            return;

        collected = true;

        UpgradeSO real = UpgradeRegistry.Resolve(upgradeToGrant);

        AudioManager.PlayUpgrade(real, sfxSource != null ? sfxSource.clip : null);

        AuraHandler aura = manager.GetComponent<AuraHandler>();
        if (aura != null)
            aura.ActivateAura(real.aColor);

        PickupEffect effect = GetComponent<PickupEffect>();
        if (effect != null)
            effect.OnPickup();
        else
            Destroy(gameObject);
    }
}
