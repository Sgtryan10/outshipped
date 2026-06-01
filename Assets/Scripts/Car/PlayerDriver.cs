using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CarController))]
public class PlayerDriver : MonoBehaviour
{
    [Header("Optional Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference brakeAction;

    [Header("Fallback Controls")]
    [SerializeField] private bool useDirectInputFallback = true;

    private CarController car;

    protected virtual void Awake()
    {
        car = GetComponent<CarController>();
    }

    protected virtual void OnEnable()
    {
        moveAction?.action.Enable();
        brakeAction?.action.Enable();
    }

    protected virtual void OnDisable()
    {
        moveAction?.action.Disable();
        brakeAction?.action.Disable();

        if (car)
            car.SetInputs(0f, 0f, false);
    }

    protected virtual void Update()
    {
        if (!car) return;

        Vector2 move = moveAction ? moveAction.action.ReadValue<Vector2>() : ReadFallbackMove();
        bool brake = brakeAction ? brakeAction.action.IsPressed() : ReadFallbackBrake();
        car.SetInputs(move.y, move.x, brake);
    }

    Vector2 ReadFallbackMove()
    {
        if (!useDirectInputFallback)
            return Vector2.zero;

        Vector2 move = Vector2.zero;

        if (Keyboard.current != null)
        {
            move.x = ReadAxis(
                Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed,
                Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed);
            move.y = ReadAxis(
                Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed,
                Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed);
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > move.sqrMagnitude)
                move = stick;
        }

        return Vector2.ClampMagnitude(move, 1f);
    }

    bool ReadFallbackBrake()
    {
        if (!useDirectInputFallback)
            return false;

        bool keyboardBrake = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        bool gamepadBrake = Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;
        return keyboardBrake || gamepadBrake;
    }

    static float ReadAxis(bool negative, bool positive)
    {
        return (positive ? 1f : 0f) - (negative ? 1f : 0f);
    }
}
