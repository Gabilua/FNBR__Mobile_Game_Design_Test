using NaughtyAttributes;
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

    [SerializeField] private Joystick _lStick;
    [SerializeField] private Joystick _rStick;
    [ReadOnly][SerializeField] private bool _lStickPressed;
    [ReadOnly][SerializeField] private bool _rStickPressed;

    [ReadOnly][SerializeField] private bool _chargeStickReceivingInput;
    [ReadOnly][SerializeField] private bool _chargeStickBeingHeld;

    [SerializeField] private float _chargeStickHoldTreshold;
    [ReadOnly][SerializeField] private float _chargeStickInputDuration;

    private PlayerInput _input;

    private void OnEnable()
    {
        _input = GetComponent<PlayerInput>();

        if (_input != null)
            _input.onActionTriggered += InputActionTriggered;

        _lStick.OnStickMoved += OnLStickMoved;
        _rStick.OnStickMoved += OnRStickMoved;

        _lStick.OnStickReleased += OnLStickReleased;
        _rStick.OnStickReleased += OnRStickReleased;
    }
    private void Update()
    {
        if (_lStickPressed)
            OnInputReceived?.Invoke(ButtonType.LStick, InputType.Hold, _lStick.GetStickInput());

        if (_rStickPressed)
            OnInputReceived?.Invoke(ButtonType.RStick, InputType.Hold, _rStick.GetStickInput());

        if (_chargeStickReceivingInput)
        {
            _chargeStickInputDuration += Time.deltaTime;

            if (_chargeStickInputDuration >= _chargeStickHoldTreshold && !_chargeStickBeingHeld)
                _chargeStickBeingHeld = true;

            if (_chargeStickBeingHeld)
                OnInputReceived?.Invoke(ButtonType.LTrigger, InputType.Hold);
        }
    }

    #region Digital Input
    private void OnLStickMoved()
    {
        _lStickPressed = true;
    }
    private void OnRStickMoved()
    {
        _rStickPressed = true;
    }

    private void OnLStickReleased()
    {
        _lStickPressed = false;

        OnInputReceived?.Invoke(ButtonType.LStick, InputType.Release, _lStick.GetStickInput());
    }
    private void OnRStickReleased()
    {
        _rStickPressed = false;

        OnInputReceived?.Invoke(ButtonType.RStick, InputType.Release, _rStick.GetStickInput());
    }

    public void OnChargeButtonPressed()
    {
        _chargeStickReceivingInput = true;
    }
    public void OnChargeButtonReleased()
    {
        _chargeStickReceivingInput = false;
        _chargeStickInputDuration = 0;

        if (_chargeStickBeingHeld)
        {
            _chargeStickBeingHeld = false;

            OnInputReceived?.Invoke(ButtonType.LTrigger, InputType.Release);
        }
        else
            OnInputReceived?.Invoke(ButtonType.LTrigger, InputType.Press);
    }
    #endregion

    #region Physical Input
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
    #endregion
}
