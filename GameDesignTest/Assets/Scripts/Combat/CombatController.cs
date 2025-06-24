using NaughtyAttributes;
using System.Timers;
using UnityEditor;
using UnityEditor.Animations.Rigging;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    public delegate void StartFiringCannon();
    public event StartFiringCannon OnStartFiringCannon;


    private PlayerInputManager _inputManager;
    [ReadOnly][SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private EntityController _entityController;

    [SerializeField] private Transform _cannonBarrelPoint;
    [ReadOnly][SerializeField] private bool _isCharging;
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
    private void Awake()
    {
        SetupCombat();
    }
    private void Update()
    {
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
    private void SetupCombat()
    {
        _inputManager = GetComponent<PlayerInputManager>();
        _rigidbody = GetComponent<Rigidbody>();

        _inputManager.OnInputReceived += OnInputReceived;

        _projectileTrajectoryDisplay.SetupTrajectoryDisplay(_cannonBarrelPoint);
        SetupRuntimeProjectileProperties();

        ToggleCharging(false);
    }
    private void SetupRuntimeProjectileProperties()
    {
        _projectileRuntimeProperties = new ProjectileRuntimeProperties();

        _projectileRuntimeProperties.initialPosition = _cannonBarrelPoint.position;
        _projectileRuntimeProperties.initialDirection = _cannonBarrelPoint.forward;
        _projectileRuntimeProperties.initialSpeed = GetShootingForce();

        Rigidbody projectileRigidbody = _entityController.entityData.startingWeapon.weaponProjectile.GetComponent<Rigidbody>();

        _projectileRuntimeProperties.mass = projectileRigidbody.mass;
        _projectileRuntimeProperties.drag = projectileRigidbody.drag;

    }
    private ProjectileRuntimeProperties GetCurrentProjectileProperties()
    {
        ProjectileRuntimeProperties currentProperties = _projectileRuntimeProperties;

        currentProperties.initialPosition = _cannonBarrelPoint.position;
        currentProperties.initialDirection = _cannonBarrelPoint.forward;
        currentProperties.initialSpeed = GetShootingForce();

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
                        ToggleCharging(true);
                    else if (inputType == InputType.Release)
                        ToggleCharging(false);
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
    private void CannonFire()
    {
        if (_weaponOnCooldown)
            return;

        ShootProjectile(GetShootingForce());
    }
 
    private float GetShootingForce()
    {
        return _currentShootingForce = _entityController.entityData.startingWeapon.weaponProjectileBaseForce * (1f+ GetChargeRate() * 2f);
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
    private void ToggleCharging(bool state)
    {
        _isCharging = state;

        if (!state)
            _currentCharge = 0;

        _projectileTrajectoryDisplay.ToggleTrajectoryDisplay(state);
    }
    private void UpdateChargingTime(float charge)
    {
        _currentCharge = charge; 
    }
    private float GetChargeRate()
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
            _weaponMeshRenderer.sharedMaterial.SetColor("_EmissionColor", _weaponMeshRendererBaseColor * _weaponShotCooldownTimer*0.5f);
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

[System.Serializable]
public class ProjectileTracjectoryDisplayController
{
    [ReadOnly][SerializeField] private LineRenderer trajectoryDisplay;
    [SerializeField] private ParticleSystem hitPositionDisplay;
    [SerializeField] private int displayResolution;
    [SerializeField] private float displayUpdateRate;

    public void SetupTrajectoryDisplay(Transform projectileBarrelPoint)
    {
        trajectoryDisplay.SetPosition(0, projectileBarrelPoint.transform.position);
    }

    public void PredictProjectileTrajectory(ProjectileRuntimeProperties properties)
    {
        Vector3 projectileVelocity = properties.GetInitialVelocity();
        Vector3 projectilePosition = properties.initialPosition;

        UpdateTrajectoryDisplay(displayResolution, 0, projectilePosition);

        for (int i = 1; i < displayResolution; i++)
        {
            projectileVelocity = properties.GetUpdatedVelocity(projectileVelocity, displayUpdateRate);
            Vector3 nextProjectilePosition = projectilePosition + projectileVelocity * displayUpdateRate;

            if (Physics.Raycast(projectilePosition, projectileVelocity.normalized, out RaycastHit hit, Vector3.Distance(projectilePosition, nextProjectilePosition) * 1.1f))
            {
                UpdateTrajectoryDisplay(i, i - 1, hit.point);
                UpdateHitPositionDisplay(hit.point, hit.normal);
                break;
            }

            projectilePosition = nextProjectilePosition;
            UpdateTrajectoryDisplay(displayResolution, i, projectilePosition);
            ToggleHitPositionDisplay(false);
        }
    }

    private void UpdateTrajectoryDisplay(int pointCount, int pointIndex, Vector3 pointPosition)
    {
        trajectoryDisplay.positionCount = pointCount;
        trajectoryDisplay.SetPosition(pointIndex, pointPosition);
    }
    private void UpdateHitPositionDisplay(Vector3 hitPoint, Vector3 hitPointNormal)
    {
        ToggleHitPositionDisplay(true);

        hitPositionDisplay.transform.position = trajectoryDisplay.GetPosition(trajectoryDisplay.positionCount - 1);
        hitPositionDisplay.transform.rotation = Quaternion.LookRotation(hitPointNormal, Vector3.up);

        hitPositionDisplay.transform.localScale = Vector3.one*(trajectoryDisplay.GetPosition(0)- trajectoryDisplay.GetPosition(trajectoryDisplay.positionCount - 1)).magnitude/3.5f;
    }
    public void ToggleTrajectoryDisplay(bool state)
    {
        trajectoryDisplay.enabled = state;
        ToggleHitPositionDisplay(state);
    }
    private void ToggleHitPositionDisplay(bool state)
    {
        hitPositionDisplay.gameObject.SetActive(state);
    }
}
