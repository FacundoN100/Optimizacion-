using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class Clock
{
    [SerializeField] private float maxTime = 100f;
    [SerializeField] private Image timeFillImage;
    [SerializeField] private Canvas deathImage;
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private GameObject owner;

    private Action onTimeExpired;
    private float currentTime;
    private float timer;
    private int frameCount;
    private bool canStart;
    private bool isDead;
    private bool stopped;

    public bool IsConfigured => owner != null;

    public void Initialize(Action timeExpired)
    {
        onTimeExpired = timeExpired;
        currentTime = maxTime;
        timer = 0f;
        frameCount = 0;
        canStart = false;
        isDead = false;
        stopped = false;
        RefreshTime();
        if (deathImage != null) deathImage.enabled = false;
    }

    public void Tick(float deltaTime, float unscaledDeltaTime)
    {
        if (stopped || owner == null || !owner.activeInHierarchy) return;
        if (canStart && !isDead)
        {
            currentTime = Mathf.Clamp(currentTime - deltaTime, 0f, maxTime);
            RefreshTime();
            if (currentTime <= 0f)
            {
                isDead = true;
                if (deathImage != null) deathImage.enabled = true;
                onTimeExpired?.Invoke();
            }
        }
        frameCount++;
        timer += unscaledDeltaTime;
        if (timer >= 0.5f)
        {
            if (fpsText != null) fpsText.SetText("{0:0} FPS", frameCount / timer);
            frameCount = 0;
            timer = 0f;
        }
    }

    private void RefreshTime()
    {
        if (timeFillImage != null)
            timeFillImage.fillAmount = maxTime > 0f ? currentTime / maxTime : 0f;
    }

    public void StartCounting() => canStart = true;

    public void Stop()
    {
        stopped = true;
        if (owner != null) owner.SetActive(false);
    }
}
