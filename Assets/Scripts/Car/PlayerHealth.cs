using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PlayerHealth : IPlayerHealth
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Canvas deathCanvas;

    private Action onDeath;
    private float health;
    private bool isDead;

    public bool IsConfigured => healthFillImage != null && deathCanvas != null;

    public void Initialize(Action playerDied)
    {
        onDeath = playerDied;
        health = maxHealth;
        isDead = false;
        RefreshBar();
    }

    public void ReceiveDamage(float damage)
    {
        if (isDead) return;

        health = Mathf.Clamp(health - Mathf.Max(0f, damage), 0f, maxHealth);
        RefreshBar();

        if (health <= 0f)
        {
            isDead = true;
            deathCanvas.enabled = true;
            onDeath?.Invoke();
        }
    }

    private void RefreshBar()
    {
        healthFillImage.fillAmount = maxHealth > 0f ? health / maxHealth : 0f;
    }
}
