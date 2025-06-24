using NaughtyAttributes;
using System.Timers;
using UnityEditor;
using UnityEditor.Animations.Rigging;
using UnityEngine;

public class CombatController : MonoBehaviour
{
    public delegate void Damage();
    public event Damage OnDamage;

    public delegate void Death();
    public event Death OnDeath;

    [ReadOnly][SerializeField] protected Rigidbody _rigidbody;
    [SerializeField] protected EntityController _entityController;

    [ReadOnly][SerializeField] protected float _currentHealth;
    [ReadOnly][SerializeField] protected bool _isDead;


    #region Unity
    private void Awake()
    {
        SetupCombat();
    }
    #endregion

    #region Setup
    protected virtual void SetupCombat()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _currentHealth = _entityController.entityData.maxHealth;
    }  
    #endregion  

    #region Damage
    public void ReceiveDamage(float damageValue)
    {
        if (_isDead)
            return;

        _currentHealth = Mathf.Clamp(_currentHealth - damageValue, 0f, _currentHealth);

        if (_currentHealth == 0f)
            Die();
        else
            OnDamage?.Invoke();
    }
    private void Die()
    {
        _isDead = true;

        OnDeath?.Invoke();

        Destroy(gameObject, Random.Range(3f, 6f));
    }

    public bool IsDead()
    {
        return _isDead;
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
