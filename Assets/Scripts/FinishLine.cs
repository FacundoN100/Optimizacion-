using System;
using TMPro;
using UnityEngine;

[Serializable]
public sealed class FinishLine : IPointsCounter
{
    [SerializeField] private Canvas VictoryImage;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI finalPointsText;
    [SerializeField] private Collider trigger;

    private Action stopClock;
    private int points;

    public Collider Trigger => trigger;

    public void Initialize(Action onFinished)
    {
        stopClock = onFinished;
        points = 0;
    }

    public void Complete(float timeSpent)
    {
        if (timeText != null) timeText.SetText($"{timeSpent:F2}s");
        if (finalPointsText != null) finalPointsText.SetText("{0}", points);
        int score = (Mathf.Max(0, (int)(100 - timeSpent)) + 1) * (points * 10 + 1);
        if (scoreText != null) scoreText.SetText("{0}", score);
        stopClock?.Invoke();
        if (VictoryImage != null) VictoryImage.enabled = true;
    }

    public void IncreasePoints()
    {
        points += 100;
        if (pointsText != null) pointsText.SetText("Points: {0}", points);
    }
}