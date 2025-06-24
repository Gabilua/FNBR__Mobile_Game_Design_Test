using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCMovementController : MovementController
{
    private CombatController _combatController;

    [ReadOnly][SerializeField] private NavMeshAgent _navmeshAgent;
    [ReadOnly][SerializeField] private Vector3 _currentWanderPoint;

    [ReadOnly][SerializeField] private float wanderDelayTimer;

    public override void SetupMovement()
    {
        base.SetupMovement();

        _combatController = GetComponent<CombatController>();
        _combatController.OnDamage += Damage;

        _navmeshAgent = GetComponent<NavMeshAgent>();
        _navmeshAgent.speed = _entityController.entityData.maxMovementSpeed;

        wanderDelayTimer = Random.Range(0f, 4f);
    }

    private void PickWanderPoint()
    {
        _navmeshAgent.isStopped = false;

        _currentWanderPoint = GetRandomPointOnNavmesh();
        _navmeshAgent.SetDestination(_currentWanderPoint);
        _isMoving = true;
    }
    private void StopWandering()
    {
        _isMoving = false;
        _navmeshAgent.isStopped = true;
        _currentWanderPoint = Vector3.zero;

        wanderDelayTimer = Random.Range(1f, 3f);
    }
    private void Damage()
    {
        StopWandering();
    }
    protected override void Update()
    {
        if(_combatController.IsDead())
        {
            if (IsMoving())
                StopWandering();

            return;
        }

        if (IsMoving())
        {
            if (_navmeshAgent.remainingDistance <= 0.5f)
                StopWandering();
        }
        else
        {
            if (wanderDelayTimer > 0f)
                wanderDelayTimer -= Time.deltaTime;
            if (wanderDelayTimer < 0f)
                wanderDelayTimer = 0f;
            else if (wanderDelayTimer == 0f)
                PickWanderPoint();
        }

        UpdateMovementState();
    }
    private Vector3 GetRandomPointOnNavmesh()
    {
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();

        int maxIndices = navMeshData.indices.Length - 3;

        // pick the first indice of a random triangle in the nav mesh
        int firstVertexSelected = UnityEngine.Random.Range(0, maxIndices);
        int secondVertexSelected = UnityEngine.Random.Range(0, maxIndices);

        // spawn on verticies
        Vector3 point = navMeshData.vertices[navMeshData.indices[firstVertexSelected]];

        Vector3 firstVertexPosition = navMeshData.vertices[navMeshData.indices[firstVertexSelected]];
        Vector3 secondVertexPosition = navMeshData.vertices[navMeshData.indices[secondVertexSelected]];

        // eliminate points that share a similar X or Z position to stop spawining in square grid line formations
        if ((int)firstVertexPosition.x == (int)secondVertexPosition.x || (int)firstVertexPosition.z == (int)secondVertexPosition.z)
        {
            point = GetRandomPointOnNavmesh(); // re-roll a position - I'm not happy with this recursion it could be better
        }
        else
        {
            // select a random point on it
            point = Vector3.Lerp(firstVertexPosition, secondVertexPosition, UnityEngine.Random.Range(0.05f, 0.95f));
        }

        return point;
    }
    protected override void UpdateMovementState()
    {
        SendMovementStateUpdateEvent(_isMoving);
    }
}
