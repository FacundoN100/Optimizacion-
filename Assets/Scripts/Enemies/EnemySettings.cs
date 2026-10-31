using System;
using UnityEngine;

[Serializable]
public class EnemySettings
{
    [SerializeField] private float speed = 18f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float slowRadius = 5f;
    [SerializeField] private float maxPredictionTime = 0.5f;
    [SerializeField] private float waypointReachDistance = 3f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float decisionInterval = 0.2f;
    [SerializeField] private LineOfSight lineOfSight;

    public float Speed => speed;
    public float RotationSpeed => rotationSpeed;
    public float SlowRadius => slowRadius;
    public float MaxPredictionTime => maxPredictionTime;
    public float WaypointReachDistance => waypointReachDistance;
    public float Damage => damage;
    public float AttackCooldown => attackCooldown;
    public float DecisionInterval => decisionInterval;
    public LineOfSight LineOfSight => lineOfSight;
}
