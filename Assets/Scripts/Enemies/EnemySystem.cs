using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemySystem
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private EnemySettings settings;

    private List<EnemyController> enemies;
    private Dictionary<Collider, EnemyController> enemiesByCollider;
    private IPlayerHealth playerHealth;

    public bool IsConfigured => enemyPrefab != null && spawnPoints != null && spawnPoints.Length > 0
                                && patrolPoints != null && patrolPoints.Length > 0;

    public void Initialize(Rigidbody playerBody, IPlayerHealth health)
    {
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
            EnemyController enemy = new EnemyController(instance, playerBody, route, settings, tree);
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

    public void StopAll()
    {
        if (enemies == null) return;
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].Stop();
        }
    }
}
