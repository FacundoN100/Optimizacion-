using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class EnemySystem : IDisposable
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private NavMeshData navigationData;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private EnemySettings settings;

    private List<EnemyController> enemies;
    private Dictionary<Collider, EnemyController> enemiesByCollider;
    private IPlayerHealth playerHealth;
    private NavMeshDataInstance navigationInstance;

    public bool IsConfigured => enemyPrefab != null && spawnPoints != null && spawnPoints.Length > 0
                                && patrolPoints != null && patrolPoints.Length > 0;

    public void Initialize(Rigidbody playerBody, IPlayerHealth health)
    {
        if (navigationInstance.valid) navigationInstance.Remove();
        if (navigationData != null) navigationInstance = NavMesh.AddNavMeshData(navigationData);
        playerHealth = health;
        settings.LineOfSight.Initialize();
        DecisionNode tree = EnemyDecisionTree.Create();
        Transform[] route = CleanRoute();
        if (route.Length == 0) return;

        enemies = new List<EnemyController>(spawnPoints.Length);
        enemiesByCollider = new Dictionary<Collider, EnemyController>(spawnPoints.Length);

        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;
            GameObject instance = GameObject.Instantiate(enemyPrefab, point.position, point.rotation);
            var collider = instance.GetComponent<Collider>();
            Vector3 extents = collider.bounds.extents;
            float radius = Mathf.Sqrt(extents.x * extents.x + extents.z * extents.z);
            EnemyNavigation navigation = navigationData != null
                ? new EnemyNavigation(NavMesh.GetSettingsByIndex(0).agentTypeID, radius, enemies.Count) : null;
            EnemyController enemy = new EnemyController(instance, playerBody, route, settings, tree, navigation);
            enemies.Add(enemy);
            if (enemy.Collider != null) enemiesByCollider.Add(enemy.Collider, enemy);
        }
    }

    private Transform[] CleanRoute()
    {
        List<Transform> route = new List<Transform>(patrolPoints.Length);
        foreach (Transform point in patrolPoints)
        {
            if (point != null) route.Add(point);
        }
        return route.ToArray();
    }

    public void TickPhysics(float deltaTime)
    {
        if (enemies == null) return;
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].TickPhysics(deltaTime);
        }
    }

    public void HandlePlayerCollision(Collider other)
    {
        if (enemiesByCollider == null) return;
        if (enemiesByCollider.TryGetValue(other, out EnemyController enemy) && enemy.TryAttack())
            playerHealth.ReceiveDamage(settings.Damage);
    }

    public void Dispose()
    {
        StopAll();
        if (navigationInstance.valid) navigationInstance.Remove();
        navigationInstance = default;
    }

    public void StopAll()
    {
        if (enemies == null) return;
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].Stop();
        }
    }
}
