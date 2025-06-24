using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using AYellowpaper.SerializedCollections;

[System.Serializable]
public struct ExternalForceSettings
{
    public enum ExternalForceApplicationOverTime { Instant, Continuous }

    [Header("Force Settings")]

    [SerializeField][AllowNesting] private Vector3 initialAddedForcePerAxis;
    [SerializeField][AllowNesting] private ExternalForceApplicationOverTime addedForceApplicationOverTime;

    [HideIf("addedForceApplicationOverTime", ExternalForceApplicationOverTime.Instant)]
    public float initialForceDuration;

    [SerializeField][AllowNesting] private AnimationCurve falloffOverDuration;

    [SerializeField][ReadOnly][AllowNesting] private Vector3 _finalForceAddedPerAxis;
    [SerializeField][ReadOnly][AllowNesting] private float _forceElapsedTime;

    public void SetInitialForceVector(Vector3 vector)
    {
        initialAddedForcePerAxis = vector;
    }
    public void SetInitialForceDuration(float duration)
    {
        initialForceDuration = duration;
    }

    private Vector3 ApplyFalloffToForceVector(Vector3 forceVector)
    {
        return forceVector *= falloffOverDuration.Evaluate(GetEvaluationTimeRate());
    }
    public void RunForceOverTime()
    {
        _forceElapsedTime += Time.deltaTime;
    }
    public bool ForceHasEnded()
    {
        return _finalForceAddedPerAxis == Vector3.zero && GetEvaluationTimeRate() >= 1f;
    }

    public Vector3 GetInitialForceVector()
    {
        return initialAddedForcePerAxis;
    }
    public float GetInitialForceDuration()
    {
        return initialForceDuration;
    }
    public ExternalForceApplicationOverTime GetForceApplicationOverTime()
    {
        return addedForceApplicationOverTime;
    }

    private float GetEvaluationTimeRate()
    {
        return addedForceApplicationOverTime == ExternalForceApplicationOverTime.Continuous ?
            (_forceElapsedTime - initialForceDuration) / initialForceDuration :
            _forceElapsedTime / initialForceDuration;
    }
    private Vector3 GetFinalForceVector()
    {
        _finalForceAddedPerAxis = ApplyFalloffToForceVector(initialAddedForcePerAxis);

        if (addedForceApplicationOverTime == ExternalForceApplicationOverTime.Continuous && _forceElapsedTime <= initialForceDuration)
            _finalForceAddedPerAxis = initialAddedForcePerAxis;

        return _finalForceAddedPerAxis;
    }

    public Vector3 GetAddedForceVector()
    {
        return GetFinalForceVector();
    }
}
public enum ChosenAxis { X, Y, Z }
[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour
{
    #region Events
    public delegate void GroundedStateChange(bool state);
    public event GroundedStateChange OnGroundedStateChange;

    public delegate void StrafeDirectionChange(Vector2 dir);
    public event StrafeDirectionChange OnStrafeDirectionChange;

    public delegate void MovementInputIntensity(float input);
    public event MovementInputIntensity OnMovementInputIntensityChange;

    public delegate void MovementSpeedChange(float currentSpeed);
    public event MovementSpeedChange OnMovementSpeedChange;

    public delegate void MovingStateChange(bool state);
    public event MovingStateChange OnMovingStateChange;

    public delegate void MovementFromEffect(ExternalForceSettings effectMovementSettings);
    public event MovementFromEffect OnMovementFromEffect;

    public delegate void JumpStateChange(bool state);
    public event JumpStateChange OnJumpStateChange;

    public delegate void RootMotionMovementToggle(bool toggleState);
    public event RootMotionMovementToggle OnRootMotionMovementToggle;
    #endregion

    protected EntityController _entityController;
  //  protected CombatController _combatController;
    protected CharacterController _controller;   
    protected Collider _entityCollider;

    #region Movement Members
    [Header("Movement Attributes")]
    [ReadOnly][SerializeField] protected float _turnSpeedLimitation = 1;
    [ReadOnly][SerializeField] protected float _currentMovementSpeed;
    [ReadOnly][SerializeField] protected float _movementSpeedLimitation = 1;

    [ReadOnly][SerializeField] protected bool _autoTrackCurrentTarget;
    #endregion

    #region Ground Check Members
    [Header("Ground Check")]
    [SerializeField] protected float _groundLevel;
    [SerializeField] protected LayerMask _groundLayer;
    [SerializeField] protected LayerMask _obstacleLayerMask;
    [SerializeField] public float _gravityMultiplier;
    #endregion

    #region General Movement Members
    [Header("Movement Info")]
    [ReadOnly][SerializeField] public bool isGrounded;
    [ReadOnly][SerializeField] public bool wasGroundedLastFrame;
    [ReadOnly][SerializeField] protected bool _canBasicMove = true;
    [ReadOnly][SerializeField] protected bool _canAim = true;
    [ReadOnly][SerializeField] protected bool _isMoving;
    [ReadOnly][SerializeField] protected bool _isAiming;
    [ReadOnly][SerializeField] protected bool _isJumping;
    [ReadOnly][SerializeField] protected bool _isStrafing;

    [ReadOnly][SerializeField] protected Vector3 _gravityForce;
    [ReadOnly][SerializeField] protected List<ExternalForceSettings> _addedForces = new List<ExternalForceSettings>();
    [ReadOnly][SerializeField] protected Vector3 _resultingForces;

    protected List<ExternalForceSettings> _endedExternalForces = new List<ExternalForceSettings>();
    protected Transform _transform;

    [ReadOnly][SerializeField] protected Vector3 _move;
    [ReadOnly][SerializeField] protected Vector2 _movementInput;
    [ReadOnly][SerializeField] protected Vector2 _aimingInput;

    [SerializeField] protected ExternalForceSettings _baseJumpForce;

    [SerializeField] protected ExternalForceSettings _baseInstantForce;
    #endregion

    #region Setup

    private void Awake()
    {
        SetupMovement();
    }
    public virtual void SetupMovement()
    {
        _entityController = GetComponentInChildren<EntityController>();

        _entityCollider = GetComponent<Collider>();
        _controller = GetComponent<CharacterController>();
        _transform = transform;

        SubscribeToEvents();
    }
    protected virtual void SubscribeToEvents()
    {
        //_interactionManager.OnEntityDied += OnDeath;
      //  _interactionManager.OnEntityRevived += OnRevive;
    }

    #endregion

    #region Unity
    protected virtual void Update()
    {
        UpdadeGroundedState();

        ApplyMovementAcceleration();

        Move();
        UpdateMovementState();
        Aim();

        ApplyFauxGravity();
        UpdateAddedForces();
    }

    private void LateUpdate()
    {
        wasGroundedLastFrame = isGrounded;
    }
    #endregion

    #region Movement Methods

    #region Aiming
    public void TemporaryTurnSpeedLimitation(bool state, float specialTurnLimitation = 1)
    {
        _autoTrackCurrentTarget = state;

        if (state)
            _turnSpeedLimitation = specialTurnLimitation;
        else
            _turnSpeedLimitation = 1f;
    }
    public void InstantAim(Vector3 direction)
    {
        SetAimingDirection(GetVector2FromVector3(direction));
        Aim(true);
    }
    protected virtual void Aim(bool instantly = false)
    {
        if (!_canAim)
           return;

        if (GetAimingDirection() == Vector3.zero)
            return;

        _transform.Rotate(Vector3.up, _aimingInput.x * Time.deltaTime);
    }

    #endregion

    #region Moving
    public void JumpUp()
    {        
        if (!isGrounded)
            return;

        Vector3 targetJumpPosition = new Vector3(transform.position.x, transform.position.y+ _entityController.entityData.jumpHeight, transform.position.z);

        ClearThisJump(targetJumpPosition);
    }
    public void ClearThisJump(Vector3 targetPosition, bool alsoFaceIt = false)
    {
        SetJumpingState(true);

        if (alsoFaceIt)
        {
            SetAimingDirection(GetLeveledDirectionOfPoint(targetPosition));
            Aim(true);
        }

        ExternalForceSettings jumpForce = _baseJumpForce;
        Vector3 distancesToTarget = new Vector3(0f, GetDistanceToPointOnAxis(targetPosition, ChosenAxis.Y), GetDistanceToPointOnAxis(targetPosition, ChosenAxis.Z));
        Vector3 forceNeeded = new Vector3(0f, GetForceNeededForVerticalJump(distancesToTarget.y), GetForceNeededForHorizontalJump(distancesToTarget.z));

        jumpForce.SetInitialForceVector(forceNeeded);

        ApplyExternalForce(jumpForce);
    }
    protected virtual void Move()
    {
        Vector3 _addedForceTotal = GetAddedForcesVector();

        if (_canBasicMove)
            _move = GetVector3FromVector2(_movementInput * _currentMovementSpeed) + _addedForceTotal;
        else
            _move = _addedForceTotal;

        _move *= Time.deltaTime;

        _controller.Move(_move);

        OnMovementSpeedChange?.Invoke(Mathf.Clamp(_movementInput.magnitude, 0, _movementSpeedLimitation) * (_currentMovementSpeed/GetMaxMovementSpeed()));

        OnStrafeDirectionChange?.Invoke(GetStrafeDirection()); 
    }
    protected virtual void UpdateMovementState()
    {
        if ((_movementInput.x != 0 || _movementInput.y != 0) && _canBasicMove)
        {
            if(!_isMoving)
                SendMovementStateUpdateEvent(true);

            _isMoving = true;
        }
        else
        {
            if (_isMoving)
                SendMovementStateUpdateEvent(false);

            _isMoving = false;     
        }
    }
    protected void SendMovementStateUpdateEvent(bool state)
    {
        OnMovingStateChange?.Invoke(state);
    }
    protected virtual void SetJumpingState(bool state)
    {
        if (state == _isJumping)
            return;

        _isJumping = state;

        OnJumpStateChange?.Invoke(_isJumping);
    }

    #endregion

    #endregion

    #region Faux Physics
    public void ApplyExternalForce(Vector3 forceDirection)
    {
        ExternalForceSettings _basicInstantForce = _baseInstantForce;
        _basicInstantForce.SetInitialForceVector(forceDirection);

        ApplyExternalForce(_basicInstantForce);
    }
    public void ApplyExternalForce(ExternalForceSettings externalForce)
    {
        if (_addedForces.Contains(externalForce))
            return;

        _addedForces.Add(externalForce);
    }
    protected virtual void UpdateAddedForces()
    {
        for (int i = _addedForces.Count - 1; i >= 0; i--)
        {
            ExternalForceSettings iterationSettings = _addedForces[i];

            iterationSettings.RunForceOverTime();

            _addedForces[i] = iterationSettings;

            if (_addedForces[i].ForceHasEnded())
            {
                _addedForces.Remove(_addedForces[i]);
                continue;
            }
        }
    }
    protected void ApplyFauxGravity()
    {
        if (_controller.isGrounded && _gravityForce.y < 0f)
            _gravityForce.y = -1f;
        else
            _gravityForce.y += Physics.gravity.y * 3f * Time.deltaTime;
    }
    protected Vector3 GetAddedForcesVector()
    {
        Vector3 resultingForce = Vector3.zero;

        foreach (var addedForce in _addedForces)
            resultingForce += transform.InverseTransformDirection(addedForce.GetAddedForceVector());

        resultingForce += _gravityForce;

        return _resultingForces = resultingForce;
    }
    protected void ApplyMovementAcceleration()
    {
        if (_canBasicMove)
        {
            if (_currentMovementSpeed != GetMaxMovementSpeed())
                _currentMovementSpeed = Mathf.Lerp(_currentMovementSpeed, GetMaxMovementSpeed(), 20 * Time.deltaTime);
        }
        else
        {
            if (_currentMovementSpeed == 0f)
                return;

            if (_currentMovementSpeed < 0.1f)
                _currentMovementSpeed = 0;
            else
                _currentMovementSpeed = Mathf.Lerp(_currentMovementSpeed, 0, 20 * Time.deltaTime);
        }
    }

    #endregion

    #region Auxiliaries
    public void ToggleCharacterController(bool state)
    {
        _controller.enabled = state;
    }
    public virtual void TeleportToPoint(Transform point)
    {
        TeleportToPoint(point.position, point.rotation);
    }
    public virtual void TeleportToPoint(Vector3 position, Quaternion rotation)
    {
        Run.After(0.01f, () => ToggleCharacterController(true));
        ToggleCharacterController(false);

        transform.position = position;
        transform.rotation = rotation;

        SetAimingDirection(GetVector2FromVector3(rotation.eulerAngles));
    }
    protected virtual void UpdadeGroundedState()
    {
        isGrounded = _controller.isGrounded;

        if (isGrounded)
        {
            if (_isJumping && wasGroundedLastFrame != isGrounded)
                FinishedJumping();
        }     

        OnGroundedStateChange?.Invoke(isGrounded);        
    }

    protected virtual void FinishedJumping()
    {
        SetJumpingState(false);
    }

    #region Events
    private void OnDeath()
    {
        ToggleCharacterController(false);
        ChangeEntityCapacity(false);
    }
    private void OnRevive()
    {
        ToggleCharacterController(true);
        ChangeEntityCapacity(true);
    }
    protected virtual void ChangeEntityCapacity(bool state)
    {
        _canBasicMove = state;
        _canAim = state;
        _movementSpeedLimitation = state == true ? 1f : 0f;
    }
    #endregion

    #region Info Getters
    public bool IsMoving()
    {
        return _isMoving;
    }
    public bool CanMove()
    {
        return _canBasicMove;
    }
    public bool IsStrafing()
    {
        return _isStrafing;
    }
    public float GetMaxMovementSpeed()
    {
        return _entityController.entityData.maxMovementSpeed * _movementSpeedLimitation;
    }
    protected virtual float GetHorizontalCameraSpeed()
    {
        return _entityController.entityData.maxTurnSpeed * _turnSpeedLimitation;
    }
    protected virtual float GetMaxVerticalCameraSpeed()
    {
        return _entityController.entityData.maxVerticalCameraSpeed * _turnSpeedLimitation;
    }

    public float GetDistanceToPointOnAxis(Vector3 point, ChosenAxis axis)
    {
        Vector3 isolatedTargetPos = Vector3.zero;
        Vector3 isolatedOriginPos = Vector3.zero;

        switch (axis)
        {
            case ChosenAxis.X:
                {
                    isolatedTargetPos.x = point.x;
                    isolatedOriginPos.x = GetGroundedPosition().x;
                }
                break;
            case ChosenAxis.Y:
                {
                    isolatedTargetPos.y = point.y;
                    isolatedOriginPos.y = GetGroundedPosition().y;
                }
                break;
            case ChosenAxis.Z:
                {
                    isolatedTargetPos.z = point.z;
                    isolatedOriginPos.z = GetGroundedPosition().z;
                }
                break;
        }

        float distance = (isolatedTargetPos- isolatedOriginPos).magnitude;

        if (axis == ChosenAxis.Y && point.y < GetGroundedPosition().y)
            distance *= -1f;

        return distance;
    }
    public float GetLeveledDistanceToPointOnAxis(Vector3 point, ChosenAxis axis)
    {
        float distance = 0;

        switch (axis)
        {
            case ChosenAxis.X:
                distance = Mathf.Abs(GetGroundedPosition().x - point.x);
                break;
            case ChosenAxis.Y:
                distance = Mathf.Abs(GetGroundedPosition().y - point.y);
                break;
            case ChosenAxis.Z:
                distance = Mathf.Abs(GetGroundedPosition().z - point.z);
                break;
        }

        return distance;
    }
    public Vector3 GetGroundedPosition()
    {
        return transform.position;
    }
    public float GetLeveledDistanceToPoint(Vector3 point)
    {
        return (transform.position - GetLeveledPosition(point)).magnitude;
    }
    public float GetDistanceToPoint(Vector3 point)
    {
        return Mathf.Abs((transform.position - point).magnitude);
    }
    public Vector3 GetDirectionOfPoint(Vector3 point)
    {
        return (point - transform.position).normalized;
    }
    public Vector3 GetLeveledPosition(Vector3 point)
    {
        return new Vector3(point.x, transform.position.y, point.z);
    }
    public Vector3 GetLeveledDirectionOfPoint(Vector3 point)
    {
        return (GetLeveledPosition(point) - transform.position).normalized;
    }
    public float GetForceNeededForVerticalJump(float jumpHeight)
    {
        if (jumpHeight < 0)
            return 2f;

        return _entityController.entityData.verticalJumpForceCurve.Evaluate(jumpHeight);
    }
    public float GetForceNeededForHorizontalJump(float jumpDistance)
    {
        return _entityController.entityData.horizontalJumpForceCurve.Evaluate(jumpDistance);
    }
    public Vector3 GetGeneralMovementDirection()
    {
        return new Vector3(_move.x, _move.y, _move.z).normalized;
    }
    public Vector3 GetInputBasedMovementDirection()
    {
        return new Vector3(_movementInput.x, 0, _movementInput.y).normalized;
    }
    public Vector2 GetStrafeDirection()
    {
        return GetVector2FromVector3(GetInputBasedMovementDirection());
    }
    public Vector3 GetAimingDirection()
    {
        return new Vector3(_aimingInput.x, 0, _aimingInput.y).normalized;
    }
    public virtual Vector3 GetMovementVector()
    {
        return _move;
    }

    public Vector2 GetVector2FromVector3(Vector3 direction)
    {
        return new Vector2(direction.x, direction.z);
    }
    public Vector3 GetVector3FromVector2(Vector2 direction)
    {
        return new Vector3(direction.x, 0, direction.y);
    }

    public bool IsDirectionObstructed(Vector3 dir)
    {
        bool result = false;

        RaycastHit hit;
        Physics.Raycast(transform.position, dir, out hit, 3f, _obstacleLayerMask);
        result = hit.collider != null && hit.collider != _entityCollider;

        return result;
    }
    public Vector3 GetDirectionOfCurrentTarget()
    {
        // if (!_combatController.HasCombatTarget())
        //    return Vector3.zero;

        // return GetDirectionOfPoint(_combatController.GetCurrentCombatTarget().transform.position).normalized;
        return Vector3.zero;
    }
    public EntityController GetNearestEntity(List<EntityController> entities)
    {
        List<GameObject> entityObjects = new List<GameObject>();

        foreach (var entity in entities)
            entityObjects.Add(entity.gameObject);

        return GetNearestObject(entityObjects).GetComponent<EntityController>();
    }
    public GameObject GetNearestObject(List<GameObject> objects)
    {
        float _shortestDistance = Mathf.Infinity;
        GameObject currentNearestObject = null;

        foreach (GameObject currentObject in objects)
        {
            float distanceToObject = (transform.position - currentObject.transform.position).magnitude;

            if (distanceToObject < _shortestDistance)
            {
                _shortestDistance = distanceToObject;
                currentNearestObject = currentObject;
            }
        }

        return currentNearestObject;
    }

    #endregion

    #region Input Setting
    public virtual void SetMovementDirection(Vector2 input)
    {
        _movementInput = input;

        OnMovementInputIntensityChange?.Invoke(input.magnitude);
    }
    protected virtual void SetMovementInputIntensity(float intensity)
    {
        OnMovementInputIntensityChange?.Invoke(intensity);
    }
    public virtual void SetAimingDirection(Vector2 input)
    {
        _aimingInput = input;

        if ((_aimingInput.x != 0 || _aimingInput.y != 0))
            _isAiming = true;
        else
            _isAiming = false;
    }

    #endregion

    #endregion

    #region Target Lock-On
    protected virtual void ToggleStrafeState(bool state)
    {
        _isStrafing = state; 
    }

    #endregion
}
