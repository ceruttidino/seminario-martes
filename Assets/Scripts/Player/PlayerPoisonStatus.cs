using System.Collections;
using UnityEngine;

// Se agrega dinamicamente al jugador cuando la Poisonous Snake conecta un ataque
// (ver PoisonousSnake.PerformAttack). No requiere configuracion manual en el prefab del Player.
public class PlayerPoisonStatus : MonoBehaviour
{
    private IDamageable damageable;
    private Coroutine poisonRoutine;

    private void Awake()
    {
        damageable = GetComponent<IDamageable>();
    }

    // La duracion total se reparte en 2 tics: uno al 50% del tiempo y otro al finalizar.
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
