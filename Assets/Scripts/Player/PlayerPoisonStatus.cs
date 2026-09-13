using System.Collections;
using UnityEngine;

public class PlayerPoisonStatus : MonoBehaviour
{
    private IDamageable damageable;
    private Coroutine poisonRoutine;

    private void Awake()
    {
        damageable = GetComponent<IDamageable>();
    }

    public void ApplyPoison(float duration, float tickDamage)
    {
        if (poisonRoutine != null)
            StopCoroutine(poisonRoutine);

        poisonRoutine = StartCoroutine(PoisonRoutine(duration, tickDamage));
    }

    private IEnumerator PoisonRoutine(float duration, float tickDamage)
    {
        float halfDuration = duration * 0.5f;

        yield return new WaitForSeconds(halfDuration);
        damageable?.TakeDamage(tickDamage);

        yield return new WaitForSeconds(duration - halfDuration);
        damageable?.TakeDamage(tickDamage);

        poisonRoutine = null;
    }
}
