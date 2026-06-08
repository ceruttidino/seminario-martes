using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerAim : MonoBehaviour
{
    [SerializeField] private float inputDeadzone = 0.1f;

    private InputAction aimAction;
    private Vector2 aimInput;
    private Vector2 lastAimDirection = Vector2.down;

    public Vector2 LastAimDirection => lastAimDirection;
    public bool HasAimInput => aimInput != Vector2.zero;

    private void Awake()
    {
        aimAction = GetComponent<PlayerInput>().actions["Aim"];
    }

    private void OnEnable()
    {
        if (aimAction != null)
            aimAction.Enable();
    }

    private void Update()
    {
        ReadAimInput();
    }

    public Vector2 GetAttackDirection()
    {
        ReadAimInput();
        return lastAimDirection;
    }

    public bool IsAimingNow()
    {
        ReadAimInput();
        return aimInput != Vector2.zero;
    }

    private void ReadAimInput()
    {
        Vector2 raw = aimAction != null ? aimAction.ReadValue<Vector2>() : Vector2.zero;

        if (raw.sqrMagnitude < inputDeadzone * inputDeadzone && Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed) raw = Vector2.left;
            else if (Keyboard.current.rightArrowKey.isPressed) raw = Vector2.right;
            else if (Keyboard.current.upArrowKey.isPressed) raw = Vector2.up;
            else if (Keyboard.current.downArrowKey.isPressed) raw = Vector2.down;
        }

        if (raw.sqrMagnitude < inputDeadzone * inputDeadzone)
        {
            aimInput = Vector2.zero;
            return;
        }

        aimInput = ToCardinalDirection(raw);
        lastAimDirection = aimInput;
    }

    private static Vector2 ToCardinalDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0f ? Vector2.right : Vector2.left;

        return direction.y > 0f ? Vector2.up : Vector2.down;
    }
}
