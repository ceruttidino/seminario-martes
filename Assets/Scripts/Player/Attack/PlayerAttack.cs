using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attacks")]
    [SerializeField] private QuickAttack quickAttack;
    [SerializeField] private AreaAttack areaAttack;

    [Header("Input Buffer")] // NUEVO
    [Tooltip("Si apretás el ataque pesado un poquito antes de que termine el cooldown/dash, se ejecuta igual apenas se pueda")]
    [SerializeField] private float areaAttackBufferTime = 0.15f; // NUEVO

    private float areaBufferedUntil = -1f; // NUEVO

    public void OnAreaAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (GamePause.IsGameplayFrozen) return;

        // CAMBIADO: antes se ejecutaba directo y si no se podía, el input se perdía
        areaBufferedUntil = Time.time + areaAttackBufferTime;
        TryConsumeAreaBuffer();
    }

    // NUEVO
    private void Update()
    {
        if (areaBufferedUntil < 0f) return;
        TryConsumeAreaBuffer();
    }

    // NUEVO
    private void TryConsumeAreaBuffer()
    {
        if (areaAttack == null)
        {
            areaBufferedUntil = -1f;
            return;
        }

        if (Time.time > areaBufferedUntil)
        {
            areaBufferedUntil = -1f; // expiró
            return;
        }

        if (!areaAttack.CanExecute()) return;

        areaAttack.Execute();
        areaBufferedUntil = -1f;
    }
}