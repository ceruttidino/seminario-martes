using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attacks")]
    [SerializeField] private QuickAttack quickAttack;
    [SerializeField] private AreaAttack areaAttack;

    public void OnQuickAttack(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }
        quickAttack.Execute();
    }

    public void OnAreaAttack(InputAction.CallbackContext context) 
    {
        if (!context.performed)
        {
            return;
        }
        areaAttack.Execute();
    }
}
