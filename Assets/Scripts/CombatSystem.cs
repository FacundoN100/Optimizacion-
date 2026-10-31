using System;
using UnityEngine;

[Serializable]
public class CombatSystem
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EnemySystem enemies;

    private Action onPlayerDied;
    private bool stopped;

    public bool IsConfigured => playerHealth != null && playerHealth.IsConfigured
                                && enemies != null && enemies.IsConfigured;

    public void Initialize(Rigidbody playerBody, Action playerDied)
    {
        onPlayerDied = playerDied;
        stopped = false;
        playerHealth.Initialize(OnPlayerDied);
        enemies.Initialize(playerBody, playerHealth);
    }

    public void TickPhysics(float deltaTime)
    {
        if (!stopped) enemies.TickPhysics(deltaTime);
    }

    public void HandlePlayerCollision(Collider other)
    {
        if (!stopped) enemies.HandlePlayerCollision(other);
    }

    public void Stop()
    {
        stopped = true;
        enemies.StopAll();
    }

    private void OnPlayerDied()
    {
        Stop();
        onPlayerDied?.Invoke();
    }
}
