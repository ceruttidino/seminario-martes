using UnityEngine;

public static class BossPlayerUtils
{
    public static void DamagePlayer(GameObject playerObject, int amount)
    {
        if (playerObject == null || amount <= 0) return;
        PlayerHealth health = playerObject.GetComponentInParent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(amount);
    }

    public static void ApplySlow(GameObject playerObject, float percent, float duration)
    {
        if (playerObject == null) return;
        PlayerSlowStatus slow = playerObject.GetComponent<PlayerSlowStatus>();
        if (slow == null) slow = playerObject.AddComponent<PlayerSlowStatus>();
        slow.ApplySlow(percent, duration);
    }

    public static void SetPlayerControl(GameObject playerObject, bool enabled)
    {
        if (playerObject == null) return;
        PlayerMovement movement = playerObject.GetComponentInParent<PlayerMovement>();
        if (movement != null) movement.enabled = enabled;
        PlayerDash dash = playerObject.GetComponentInParent<PlayerDash>();
        if (dash != null) dash.enabled = enabled;
    }
}
