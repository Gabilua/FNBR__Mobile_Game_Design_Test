using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;

    [SerializeField] private int _minFirstSpawnEnemies = 1;
    [SerializeField] private int _maxEnemies;
    [SerializeField] private Vector2 _respawnInterval = new Vector2(5f, 10f);

    [ReadOnly][SerializeField] private List<CombatController> _spawnedEnemies = new List<CombatController>();

    private void Awake()
    {
        FirstSpawn();
    }

    private void FirstSpawn()
    {
        int firstSpawnNumber = Random.Range(_minFirstSpawnEnemies, _maxEnemies);

        for (int i = 0; i < firstSpawnNumber; i++)
            SpawnEnemy();

        Invoke("RespawnEnemies", Random.Range(_respawnInterval.x, _respawnInterval.y));
    }
    private void RespawnEnemies()
    {
        int availableEnemySpots = _maxEnemies - _spawnedEnemies.Count;

        if (availableEnemySpots == 0)
            return;

        for (int i = 0; i < availableEnemySpots; i++)
            SpawnEnemy();

        Invoke("RespawnEnemies", Random.Range(_respawnInterval.x, _respawnInterval.y));
    }
    private void SpawnEnemy()
    {
        CombatController enemy = Instantiate(_enemyPrefab, GetRandomPointOnNavmesh(), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)).GetComponent< CombatController>();
        enemy.transform.SetParent(transform);
        _spawnedEnemies.Add(enemy);

        enemy.OnDeath += EnemyDied;
    }
    private void EnemyDied(CombatController deadEntity)
    {
        deadEntity.OnDeath -= EnemyDied;

        _spawnedEnemies.Remove(deadEntity);
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
}
