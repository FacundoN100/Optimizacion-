using System;
using UnityEngine;

[Serializable]
public class LineOfSight
{
    [SerializeField] private float distance = 50f;
    [SerializeField] private float angle = 90f;
    [SerializeField] private float attackDistance = 5f;
    [SerializeField] private LayerMask obstacles;

    private float distanceSqr;
    private float attackDistanceSqr;

    public void Initialize()
    {
        distanceSqr = distance * distance;
        attackDistanceSqr = attackDistance * attackDistance;
    }

    public bool CanBeSeen(Transform self, Transform target)
    {
        return IsInRange(self, target) && IsInAngle(self, target) && IsClearPath(self.position, target.position);
    }

    public bool CanAttack(Transform self, Transform target)
    {
        return CanBeSeen(self, target) && IsInAttackRange(self, target);
    }

    public bool IsClearPath(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        return !Physics.Raycast(from, dir, dir.magnitude, obstacles, QueryTriggerInteraction.Ignore);
    }

    private bool IsInRange(Transform self, Transform target)
    {
        return (self.position - target.position).sqrMagnitude <= distanceSqr;
    }

    private bool IsInAngle(Transform self, Transform target)
    {
        Vector3 dir = (target.position - self.position).normalized;
        return Vector3.Angle(self.forward, dir) <= angle * 0.5f;
    }

    private bool IsInAttackRange(Transform self, Transform target)
    {
        return (self.position - target.position).sqrMagnitude <= attackDistanceSqr;
    }
}
