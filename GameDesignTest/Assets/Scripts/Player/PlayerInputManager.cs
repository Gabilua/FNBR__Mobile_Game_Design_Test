using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

    public enum GameplayMode { BaseGameplayInput, UIInput}
public enum InputType {  Press, Release, Hold }
public enum ButtonType { 
    North, East, South, West,
    LShoulder, RShoulder, LTrigger, RTrigger,
    LStick, LStickButton, RStick, RStickButton,
    DPad,
    Select,
    Start
}
   
public class PlayerInputManager : MonoBehaviour
{
    public delegate void InputReceived(ButtonType button, InputType inputType, Vector2? inputAxis = null);
    public event InputReceived OnInputReceived;

    private PlayerInput _input;

    private void OnEnable()
    {
        _input = GetComponent<PlayerInput>();
        _input.onActionTriggered += InputActionTriggered;
    }

    private void InputActionTriggered(InputAction.CallbackContext value)
    {
        if (value.started && value.action.type != InputActionType.Value)
            return;

        OnInputReceived?.Invoke(GetButtonType(value), GetInputType(value), value.action.type == InputActionType.Value ? value.ReadValue<Vector2>() : Vector2.zero);
    }
    private InputType GetInputType(InputAction.CallbackContext value)
    {
        InputType inputType = InputType.Press;

        if (value.performed)
        {
            if (value.interaction is HoldInteraction)
                inputType = InputType.Hold;
            else if (value.interaction is PressInteraction)
                inputType = InputType.Press;
        }
        if (value.canceled)
            inputType = InputType.Release;

        return inputType;
    }
    private ButtonType GetButtonType(InputAction.CallbackContext value)
    {
        ButtonType buttonType = (ButtonType)System.Enum.Parse(typeof(ButtonType), value.action.name);

        return buttonType;
    }
}
