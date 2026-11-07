using UnityEngine;

public class EnemyController
{
    public enum Mode { Patrol, Pursue, Attack, AfterAttack, Search, Stopped }

    private const float RamSpeedMultiplier = 2f;
    private const float PatrolSpeedMultiplier = 0.5f;
    private const float StuckSpeedFactor = 0.2f;

    private readonly Transform transform;
    private readonly Rigidbody body;
    private readonly Rigidbody playerBody;
    private readonly Transform[] patrolPoints;
    private readonly EnemySettings settings;
    private readonly DecisionNode tree;
    private readonly EnemyContext context;
    private readonly PoliceLights policeLights;
    private readonly VehicleAudio audio;
    private readonly EnemyNavigation navigation;
    private float blockedTime;

    private Mode mode;
    private int waypointIndex;
    private int patrolStep;
    private float decisionTimer;
    private float timeSinceLastAttack;
    private Vector3 lastSeenPosition;

    public Collider Collider { get; }

    public EnemyController(GameObject instance, Rigidbody playerBody, Transform[] patrolPoints,
        EnemySettings settings, DecisionNode tree, EnemyNavigation navigation = null)
    {
        transform = instance.transform;
        body = instance.GetComponent<Rigidbody>();
        Collider = instance.GetComponent<Collider>();
        this.playerBody = playerBody;
        this.patrolPoints = patrolPoints;
        this.settings = settings;
        this.tree = tree;
        this.navigation = navigation;
        policeLights = new PoliceLights(instance.transform, settings.PoliceLightTiming);
        if (ServiceLocator.TryGet<IGameAudio>(out var gameAudio))
            audio = gameAudio.CreateVehicleAudio(body, true, settings.Speed * RamSpeedMultiplier);

        context = new EnemyContext { self = transform, player = playerBody.transform, los = settings.LineOfSight };

        mode = Mode.Patrol;
        patrolStep = Random.value < 0.5f ? 1 : -1;
        ChooseNearestWaypoint();
        timeSinceLastAttack = settings.AttackCooldown;
        decisionTimer = settings.DecisionInterval + Random.Range(0f, settings.DecisionInterval);
    }

    public void SetMode(Mode newMode)
    {
        if (newMode != mode)
        {
            blockedTime = 0f;
            navigation?.Invalidate();
        }
        if (newMode == Mode.Pursue || newMode == Mode.Attack)
        {
            lastSeenPosition = context.player.position;
            context.searchTimeLeft = settings.LineOfSight.MemoryDuration;
        }
        if (newMode == Mode.Patrol && mode != Mode.Patrol) ChooseNearestWaypoint();
        mode = newMode;
    }

    public void TickPhysics(float deltaTime)
    {
        if (mode == Mode.Stopped) return;
        policeLights.Tick(deltaTime);

        if (timeSinceLastAttack < settings.AttackCooldown)
        {
            timeSinceLastAttack += deltaTime;
            Move(Vector3.zero, 0f, deltaTime);
            return;
        }

        Vector3 horizontalVelocity = body.velocity;
        horizontalVelocity.y = 0f;
        float minSpeed = settings.Speed * PatrolSpeedMultiplier * StuckSpeedFactor;
        blockedTime = horizontalVelocity.sqrMagnitude < minSpeed * minSpeed ? blockedTime + deltaTime : 0f;
        decisionTimer -= deltaTime;
        if (decisionTimer <= 0f)
        {
            Mode previousMode = mode;
            tree.Evaluate(this, context);
            if (mode == Mode.Patrol && previousMode == Mode.Patrol && IsStuck()) ReversePatrol();
            if (mode == Mode.Search && previousMode == Mode.Search && IsStuck()) context.searchTimeLeft = 0f;
            decisionTimer = settings.DecisionInterval;
        }

        switch (mode)
        {
            case Mode.Patrol:
                Patrol(deltaTime);
                break;
            case Mode.Pursue:
                Vector3 pursueTarget = SteeringBehaviour.CalculateFuture(transform, context.player, playerBody,
                    settings.MaxPredictionTime, settings.SlowRadius);
                NavigateTo(pursueTarget, settings.Speed, deltaTime);
                lastSeenPosition = context.player.position;
                break;
            case Mode.Attack:
                NavigateTo(context.player.position, settings.Speed * RamSpeedMultiplier, deltaTime);
                lastSeenPosition = context.player.position;
                break;
            case Mode.Search:
                Search(deltaTime);
                break;
            default:
                Move(Vector3.zero, 0f, deltaTime);
                break;
        }
    }

    public bool TryAttack()
    {
        if (mode == Mode.Stopped || timeSinceLastAttack < settings.AttackCooldown) return false;

        timeSinceLastAttack = 0f;
        mode = Mode.AfterAttack;
        decisionTimer = 0f;
        return true;
    }

    public void Stop()
    {
        mode = Mode.Stopped;
        policeLights.Stop();
        audio?.SetRunning(false);
        if (body != null) body.velocity = Vector3.zero;
    }

    private void Patrol(float deltaTime)
    {
        Vector3 target = patrolPoints[waypointIndex].position;
        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;
        float reachDistance = settings.WaypointReachDistance;
        if (toTarget.sqrMagnitude <= reachDistance * reachDistance)
        {
            NextWaypoint();
            target = patrolPoints[waypointIndex].position;
        }

        NavigateTo(target, settings.Speed * PatrolSpeedMultiplier, deltaTime);
    }

    private void Search(float deltaTime)
    {
        context.searchTimeLeft -= deltaTime;
        Vector3 toTarget = lastSeenPosition - transform.position;
        toTarget.y = 0f;
        float reachDistance = settings.WaypointReachDistance;
        if (toTarget.sqrMagnitude <= reachDistance * reachDistance)
        {
            context.searchTimeLeft = 0f;
            Move(Vector3.zero, 0f, deltaTime);
            return;
        }

        NavigateTo(lastSeenPosition, settings.Speed, deltaTime);
    }

    private void NextWaypoint()
    {
        if (patrolPoints.Length < 2) return;

        int next = waypointIndex + patrolStep;
        if (next < 0 || next >= patrolPoints.Length)
        {
            patrolStep = -patrolStep;
            next = waypointIndex + patrolStep;
        }
        waypointIndex = next;
        navigation?.Invalidate();
    }

    private void ReversePatrol()
    {
        patrolStep = -patrolStep;
        blockedTime = 0f;
        NextWaypoint();
    }

    private void ChooseNearestWaypoint()
    {
        Vector3 position = Collider.bounds.center;
        int nearestVisible = -1;
        int nearest = 0;
        float nearestVisibleSqr = float.MaxValue;
        float nearestSqr = float.MaxValue;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Vector3 point = patrolPoints[i].position;
            float sqr = (point - position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = i;
            }
            if (sqr < nearestVisibleSqr && settings.LineOfSight.IsClearPath(position, point))
            {
                nearestVisibleSqr = sqr;
                nearestVisible = i;
            }
        }

        waypointIndex = nearestVisible >= 0 ? nearestVisible : nearest;
    }

    private bool IsStuck()
    {
        return blockedTime >= 1.2f;
    }

    private void NavigateTo(Vector3 target, float speed, float deltaTime)
    {
        Vector3 direction = navigation != null
            ? navigation.Direction(Collider.bounds.center, target, speed, deltaTime)
            : SteeringBehaviour.Seek(transform, target);
        Move(direction, speed, deltaTime);
    }

    private void Move(Vector3 direction, float speed, float deltaTime)
    {
        Vector3 velocity = direction * speed;
        velocity.y = body.velocity.y;
        body.velocity = velocity;

        if (direction == Vector3.zero) return;
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, angle, 0f);
        body.MoveRotation(Quaternion.Lerp(body.rotation, targetRotation, settings.RotationSpeed * deltaTime));
    }
}
