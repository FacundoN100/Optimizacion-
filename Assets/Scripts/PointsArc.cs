using System;
using UnityEngine;

[Serializable]
public sealed class PointsArc
{
    [SerializeField] private Collider trigger;
    
    private IPointsCounter pointsCounter;

    public Collider Trigger => trigger;

    public void Initialize(IPointsCounter target) => pointsCounter = target;

    public void Collect()
    {
        if (trigger == null || !trigger.enabled || !trigger.gameObject.activeInHierarchy
            || pointsCounter == null) return;
        pointsCounter.IncreasePoints();
        trigger.gameObject.SetActive(false);
    }
}