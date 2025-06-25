using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using System.Linq;

public class AnimationManager : MonoBehaviour
{
    private EntityController _entityController;
    private MovementController _movementController;
    private CombatController _combatController;
    private Animator _animator;

    #region Setup

    private void Awake()
    {
        SetupEntityAnimations();
    }
    public virtual void SetupEntityAnimations()
    {
        _entityController = GetComponent<EntityController>();

        _movementController = GetComponent<MovementController>();
        _combatController = GetComponent<CombatController>();
        _animator = gameObject.GetComponent<Animator>();

        SubscribeToEvents();
    }

    protected virtual void SubscribeToEvents()
    {       
        _movementController.OnMovingStateChange += OnMovement;
        _movementController.OnMovementSpeedChange += MovementSpeedChanged;
        _movementController.OnGroundedStateChange += GroundedStateChange;
        _movementController.OnStrafeDirectionChange += OnStrafeDirectionChange;
        _movementController.OnJumpStateChange += OnJump;

        _combatController.OnDamage += Damage;
        _combatController.OnDeath += Death;

        if (_combatController is PlayerCombatController)
        {
            PlayerCombatController playerCombatController = _combatController as PlayerCombatController;

            playerCombatController.OnStartFiringCannon += CannonFired;
        }
    }

    #endregion

    #region Movement Animations
    private void MovementSpeedChanged(float speed)
    {
        _animator.SetFloat("MovementSpeed", speed);
    }
    private void OnJump(bool state)
    {
        if (state)
            _animator.SetTrigger("Jump");
    }
    private void OnMovement(bool isMoving)
    {
        _animator.SetBool("IsMoving", isMoving);
    }
    private void GroundedStateChange(bool isGrounded)
    {
        _animator.SetBool("IsGrounded", isGrounded);
    }
    private void OnStrafeDirectionChange(Vector2 dir)
    {
        _animator.SetFloat("StrafeDirectionX", dir.x);
        _animator.SetFloat("StrafeDirectionY", dir.y);
    }
    
    #endregion

    #region Combat Animations
    private void CannonFired()
    {
        _animator.SetTrigger("Fire");
    }
    private void Death(CombatController deadEntity)
    {
        _animator.SetTrigger("Death");
    }
    private void Damage()
    {
        _animator.SetTrigger("Damage");
    }
    #endregion
}
