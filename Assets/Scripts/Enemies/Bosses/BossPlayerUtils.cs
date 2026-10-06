using UnityEngine;

public static class BossPlayerUtils
{
    private const float BossDamageCooldown = 0.75f;
    private static float nextBossDamageTime;

    public static bool DamagePlayer(GameObject playerObject, int amount) // devuelve si pegó
    {
        if (playerObject == null || amount <= 0) return false;

        // si quedó un valor viejo (por ej. con Domain Reload desactivado), se resetea
        if (nextBossDamageTime - Time.time > BossDamageCooldown)
            nextBossDamageTime = 0f;

        if (Time.time < nextBossDamageTime) return false; // evita que se apilen golpes

        PlayerHealth health = playerObject.GetComponentInParent<PlayerHealth>();
        if (health == null) return false;

        health.TakeDamage(amount);
        nextBossDamageTime = Time.time + BossDamageCooldown;
        return true;
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
