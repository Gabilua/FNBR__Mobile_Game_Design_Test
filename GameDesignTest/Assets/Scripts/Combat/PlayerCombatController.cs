using UnityEngine;
using NaughtyAttributes;

public class PlayerCombatController : CombatController
{
    public delegate void StartFiringCannon();
    public event StartFiringCannon OnStartFiringCannon;

    private PlayerInputManager _inputManager;

    [SerializeField] private Transform _cannonBarrelPoint;
    [ReadOnly][SerializeField] private bool _isCharging;
    [ReadOnly][SerializeField] private bool _holdCharge;
    [ReadOnly][SerializeField] private float _currentCharge;
    [ReadOnly][SerializeField] private float _currentShootingForce;
    [ReadOnly][SerializeField] private bool _weaponOnCooldown;
    [ReadOnly][SerializeField] private float _weaponShotCooldownTimer;

    private ProjectileRuntimeProperties _projectileRuntimeProperties;
    [SerializeField] private ProjectileTracjectoryDisplayController _projectileTrajectoryDisplay;
    [SerializeField] private MeshRenderer _weaponMeshRenderer;
    [ColorUsage(true, true)]
    [SerializeField] private Color _weaponMeshRendererBaseColor;

    #region Unity
    private void Update()
    {
        if (_holdCharge)
            return;

        UpdateChargeVisualFeedback();

        if (_weaponOnCooldown)
        {
            if (_weaponShotCooldownTimer > 0f)
                _weaponShotCooldownTimer -= Time.deltaTime;
            else if (_weaponShotCooldownTimer < 0f)
                ToggleCooldown(false);

            return;
        }

        if (_isCharging)
        {
            if (_currentCharge < _entityController.entityData.startingWeapon.weaponTotalChargeTime)
                UpdateChargingTime(_currentCharge + Time.deltaTime);
            else if (_currentCharge > _entityController.entityData.startingWeapon.weaponTotalChargeTime)
                UpdateChargingTime(_entityController.entityData.startingWeapon.weaponTotalChargeTime);
        }
        else
        {
            if (_currentCharge > 0)
                UpdateChargingTime(Mathf.Lerp(_currentCharge, 0, 25f * Time.deltaTime));
        }

        ChangeBarrelAngleAccordingToCharge();
    }
    private void LateUpdate()
    {
        if (_isCharging)
            _projectileTrajectoryDisplay.PredictProjectileTrajectory(GetCurrentProjectileProperties());
    }
    #endregion

    #region Setup
    protected override void SetupCombat()
    {
        base.SetupCombat();

        _inputManager = GetComponent<PlayerInputManager>();

        _inputManager.OnInputReceived += OnInputReceived;

        _projectileTrajectoryDisplay.SetupTrajectoryDisplay(_cannonBarrelPoint);
        SetupRuntimeProjectileProperties();

        ToggleCharging(false);
        UpdateChargeVisualFeedback();
    }
    private void SetupRuntimeProjectileProperties()
    {
        _projectileRuntimeProperties = new ProjectileRuntimeProperties();

        Rigidbody projectileRigidbody = _entityController.entityData.startingWeapon.weaponProjectile.GetComponent<Rigidbody>();

        _projectileRuntimeProperties.mass = projectileRigidbody.mass;
        _projectileRuntimeProperties.drag = projectileRigidbody.drag;

    }
    private ProjectileRuntimeProperties GetCurrentProjectileProperties()
    {
        ProjectileRuntimeProperties currentProperties = _projectileRuntimeProperties;

        currentProperties.initialPosition = _cannonBarrelPoint.position;
        currentProperties.initialDirection = _cannonBarrelPoint.forward;
        currentProperties.addedMovementVector = GetCurrentMovementVector();
        currentProperties.initialSpeed = GetShootingForce();

        currentProperties.explosionRadius = _entityController.entityData.startingWeapon.weaponExplosionRadius;
        currentProperties.explosionDamage = GetShotDamage();

        return currentProperties;
    }
    #endregion

    #region Events
    private void OnInputReceived(ButtonType button, InputType inputType, Vector2? inputAxis)
    {
        switch (button)
        {
            case ButtonType.RTrigger:
                if (inputType == InputType.Press)
                    CannonFire();
                break;
            case ButtonType.LTrigger:
                {
                    if (_weaponOnCooldown)
                        return;

                    if (inputType == InputType.Press)
                        OnToggleChargeButtonClicked();
                    else if (inputType == InputType.Hold)
                        OnToggleChargeButtonHeld();
                    else if (inputType == InputType.Release)
                        OnToggleChargeButtonHoldRelease();
                }
                break;
        }
    }
    public void OnWeaponAnimationFire()
    {
        CannonFire();
    }
    #endregion

    #region Firing
    public void CannonFire()
    {
        if (_weaponOnCooldown)
            return;

        ShootProjectile(GetShootingForce());
    }
    private float GetShotDamage()
    {
        return _entityController.entityData.startingWeapon.weaponExplosionBaseDamage * (GetChargeRate()+1f);
    }
    private float GetShootingForce()
    {
        return _currentShootingForce = _entityController.entityData.startingWeapon.weaponProjectileBaseForce * (1f + GetChargeRate() * 2f);
    }
    private void ShootProjectile(float shootingForce)
    {
        ProjectileController newProjectile = Instantiate(_entityController.entityData.startingWeapon.weaponProjectile, _cannonBarrelPoint.position, _cannonBarrelPoint.rotation).GetComponent<ProjectileController>();
        newProjectile.SetupProjectile(GetCurrentProjectileProperties(), _entityController);

        newProjectile.OnProjectileHit += ProjectileHit;

        SpawnVFX(_entityController.entityData.startingWeapon.weaponFireVFX, _cannonBarrelPoint.position, _cannonBarrelPoint.rotation);

        ToggleCooldown(true);
        ToggleCharging(false);
    }
    private void ProjectileHit(Vector3 hitPosition, Vector3 hitNormal)
    {
        SpawnVFX(_entityController.entityData.startingWeapon.weaponHitVFX, hitPosition, Quaternion.LookRotation(hitNormal));
    }

    #endregion

    #region Charging

    public void OnToggleChargeButtonHeld()
    {
        if (!_isCharging)
            ToggleCharging(true);

        if(_holdCharge)
            _holdCharge = false;
    }
    public void OnToggleChargeButtonHoldRelease()
    {
        _holdCharge = true;
    }
    public void OnToggleChargeButtonClicked()
    {
        if (_isCharging)
            ToggleCharging(false);
    }
    private void ToggleCharging(bool state)
    {
        _isCharging = state;

        if (!state)
            _currentCharge = 0;

        _projectileTrajectoryDisplay.ToggleTrajectoryDisplay(state);

        _holdCharge = false;
    }
    private void UpdateChargingTime(float charge)
    {
        _currentCharge = charge;
    }
    public float GetChargeRate()
    {
        return (_currentCharge / _entityController.entityData.startingWeapon.weaponTotalChargeTime);
    }
    private void ChangeBarrelAngleAccordingToCharge()
    {
        _cannonBarrelPoint.localRotation = Quaternion.Euler(GetBarrelAngle(), 90f, _cannonBarrelPoint.localRotation.z);
    }
    private float GetBarrelAngle()
    {
        return GetChargeRate() * -15f;
    }
    private void ToggleCooldown(bool state)
    {
        _weaponOnCooldown = state;

        _weaponShotCooldownTimer = state == true ? _entityController.entityData.startingWeapon.weaponShotCooldown : 0f;
    }
    public float GetCooldownRate()
    {
        return _weaponShotCooldownTimer / _entityController.entityData.startingWeapon.weaponShotCooldown;
    }
    #endregion

    #region Visuals
    private void SpawnVFX(GameObject vfx, Vector3 position, Quaternion rotation)
    {
        if (vfx == null)
            return;

        Instantiate(vfx, position, rotation);
    }
    private void UpdateChargeVisualFeedback()
    {
        if (_weaponOnCooldown)
            _weaponMeshRenderer.sharedMaterial.SetColor("_EmissionColor", _weaponMeshRendererBaseColor * _weaponShotCooldownTimer * 0.5f);
        else
            _weaponMeshRenderer.sharedMaterial.SetColor("_EmissionColor", _weaponMeshRendererBaseColor * GetChargeRate() * 1.5f);
    }
    #endregion

    #region Movement

    private Vector3 GetCurrentMovementVector()
    {
        return _rigidbody.velocity;
    }

    #endregion
}
