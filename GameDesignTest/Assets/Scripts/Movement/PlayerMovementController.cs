using UnityEngine;
using System;
using System.Linq;
using static PlayerInputManager;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MovementController
{
    private PlayerInputManager _inputManager;
 //   private PlayerUIManager _playerUIManager;
    protected GameObject _lockedOnTargetMarker;

    #region Setup
    public override void SetupMovement()
    {
        _inputManager = GetComponent<PlayerInputManager>();
        //_playerUIManager = playerEntityController.EntityUIManager();

        base.SetupMovement(); 
    }
    protected override void SubscribeToEvents()
    {
        base.SubscribeToEvents();

        _inputManager.OnInputReceived += OnInputReceived;

      //  _combatController.OnCombatTargetChanged += LockOnTarget;
        //_playerUIManager.OnGameplayModeChanged += GameplayModeChanged;
    }

    private void OnInputReceived(ButtonType button, InputType inputType, Vector2? inputAxis)
    {
        switch (button)
        {
            case ButtonType.LStick:
                SetMovementDirection((Vector2)inputAxis);
                break;
            case ButtonType.RStick:
                SetAimingDirection((Vector2)inputAxis);
                break;
            case ButtonType.South:
                if (inputType == InputType.Press)
                    JumpInputPerformed();
                break;
        }
    }
    #endregion


    protected void JumpInputPerformed()
    {
        if (!isGrounded)
            return;

       JumpUp();
    }
    protected virtual void LockOnTarget(EntityController target)
    {
        if(target == null)
        {
            ReleaseTargetLockOn();
                return;
        }

        if (_lockedOnTargetMarker == null)
            _lockedOnTargetMarker = new GameObject("TargetLock Marker");

        _lockedOnTargetMarker.transform.position = target.transform.position;
        _lockedOnTargetMarker.transform.SetParent(target.transform);

        ToggleStrafeState(true);
    }
    protected virtual void ReleaseTargetLockOn()
    {
        Destroy(_lockedOnTargetMarker);

        ToggleStrafeState(false);
    }
    public Vector3 GetLockedOnTargetDirection()
    {
        if (_lockedOnTargetMarker == null)
            return Vector3.zero;

        return GetLeveledDirectionOfPoint(_lockedOnTargetMarker.transform.position);
    }
}