using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;

public sealed class EnemyNavigation
{
    private static readonly ProfilerMarker RepathMarker = new ProfilerMarker("PoliceNavigation.Repath");
    private const float RepathInterval = 0.3f;
    private const float CornerDistanceSqr = 0.0625f;
    private readonly NavMeshPath path = new NavMeshPath();
    private readonly Vector3[] corners = new Vector3[128];
    private readonly NavMeshQueryFilter filter;
    private readonly float sampleDistance;
    private float repathTimer;
    private int cornerCount;
    private int cornerIndex;

    public EnemyNavigation(int agentType, float radius, int slot)
    {
        filter = new NavMeshQueryFilter { agentTypeID = agentType, areaMask = 1 };
        sampleDistance = Mathf.Max(2f, radius + 1f);
        repathTimer = slot * 0.025f;
    }

    public void Invalidate()
    {
        repathTimer = 0f;
    }

    public Vector3 Direction(Vector3 position, Vector3 target, float speed, float deltaTime)
    {
        repathTimer -= deltaTime;
        if (repathTimer <= 0f)
            Repath(position, target);

        while (cornerIndex < cornerCount)
        {
            Vector3 direction = corners[cornerIndex] - position;
            direction.y = 0f;
            float distanceSqr = direction.sqrMagnitude;
            if (distanceSqr <= CornerDistanceSqr)
            {
                cornerIndex++;
                continue;
            }
            float distance = Mathf.Sqrt(distanceSqr);
            float step = Mathf.Max(speed * deltaTime, 0.001f);
            return direction / Mathf.Max(distance, step);
        }
        return Vector3.zero;
    }

    private void Repath(Vector3 position, Vector3 target)
    {
        using (RepathMarker.Auto())
        {
            repathTimer = RepathInterval;
            cornerCount = 0;
            cornerIndex = 0;
            if (!NavMesh.SamplePosition(position, out NavMeshHit start, sampleDistance, filter)
                || !NavMesh.SamplePosition(target, out NavMeshHit goal, sampleDistance, filter)
                || !NavMesh.CalculatePath(start.position, goal.position, filter, path)
                || path.status == NavMeshPathStatus.PathInvalid) return;
            cornerCount = path.GetCornersNonAlloc(corners);
            if (cornerCount == corners.Length) cornerCount = 0;
            cornerIndex = cornerCount > 1 ? 1 : 0;
        }
    }
}
