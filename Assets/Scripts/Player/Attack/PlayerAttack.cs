using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attacks")]
    [SerializeField] private QuickAttack quickAttack;
    [SerializeField] private AreaAttack areaAttack;

    public void OnAreaAttack(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (areaAttack != null) areaAttack.Execute();
    }
}
