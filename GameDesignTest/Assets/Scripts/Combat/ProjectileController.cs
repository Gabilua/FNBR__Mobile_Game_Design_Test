using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Unity.VisualScripting;

public class ProjectileController : MonoBehaviour
{
    public delegate void ProjectileHit(Vector3 hitPosition, Vector3 hitNormal);
    public event ProjectileHit OnProjectileHit;

    [SerializeField] private LayerMask _collisionMask;
    private Rigidbody _rigibody;
    private CapsuleCollider _collider;
    private ProjectileRuntimeProperties _projectileProperties;
    private EntityController _shooter;

    #region Setup
    public void SetupProjectile(ProjectileRuntimeProperties projectileBaseProperties, EntityController entity)
    {
        _rigibody = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();

        _shooter = entity;

       _projectileProperties = projectileBaseProperties;
        ShootProjectile();
    }
    #endregion

    #region Unity
    void Update()
    {
        ProjectileAlignment();
        DetectSurfaceCollision();
    }
    #endregion

    #region Movement
    private void ShootProjectile()
    {
        _rigibody.AddForce(_projectileProperties.initialSpeed *( _projectileProperties.initialDirection + (_projectileProperties.addedMovementVector * 0.25f)), ForceMode.Impulse);
    }
    private void ProjectileAlignment()
    {
        if (_rigibody.velocity != Vector3.zero)
            transform.forward = _rigibody.velocity;
    }
    #endregion

    #region Collision
    private void DetectSurfaceCollision()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _collider.height*2f, _collisionMask))
        {
            EntityController entityHit = hit.collider.GetComponent<EntityController>();

            if (entityHit != null && entityHit == _shooter)
                return;

            SurfaceCollision(hit);
        }
    }
    private void SurfaceCollision(RaycastHit hit)
    {
        OnProjectileHit?.Invoke(hit.point, hit.normal);
        Destroy(gameObject);
    }
    #endregion
}
public struct ProjectileRuntimeProperties
{
    public Vector3 initialPosition;
    public Vector3 initialDirection;
    public Vector3 addedMovementVector;
    public float initialSpeed;
    public Vector3 currentDirection;

    public float mass;
    public float drag;

    public Vector3 GetInitialVelocity()
    {
        return initialSpeed / mass * initialDirection;
    }
    public Vector3 GetUpdatedVelocity(Vector3 currentVelocity, float increment)
    {
        currentVelocity += Physics.gravity * increment;
        currentVelocity *= Mathf.Clamp01(1f - drag * increment);

        return currentVelocity;
    }
}

