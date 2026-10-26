using System;
using UnityEngine;

[Serializable]
public sealed class PointsArc
{
    [SerializeField] private Collider trigger;
    private FinishLine finishLine;

    public Collider Trigger => trigger;

    public void Initialize(FinishLine target) => finishLine = target;

    public void Collect()
    {
        if (trigger == null || !trigger.enabled || !trigger.gameObject.activeInHierarchy
            || finishLine == null) return;
        finishLine.IncreasePoints();
        trigger.gameObject.SetActive(false);
    }
}
