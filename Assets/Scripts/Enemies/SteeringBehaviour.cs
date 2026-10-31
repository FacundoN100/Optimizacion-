using UnityEngine;

public static class SteeringBehaviour
{
    public static Vector3 Seek(Transform self, Vector3 target)
    {
        Vector3 dir = target - self.position;
        dir.y = 0f;
        return dir.normalized;
    }

    public static Vector3 Flee(Transform self, Vector3 target)
    {
        Vector3 dir = self.position - target;
        dir.y = 0f;
        return dir.normalized;
    }

    public static Vector3 Arrive(Transform self, Vector3 target, float slowRadious)
    {
        Vector3 dir = target - self.position;
        dir.y = 0f;
        float sqrDistance = dir.sqrMagnitude;

        if (sqrDistance < 0.000001f) return Vector3.zero;

        float distance = Mathf.Sqrt(sqrDistance);
        float speedFactor = Mathf.Clamp01(distance / slowRadious);
        return (dir / distance) * speedFactor;
    }

    public static Vector3 Pursue(Transform self, Transform target, Rigidbody targetRb, float maxPredictionTime, float slowRadious)
    {
        Vector3 futurePos = CalculateFuture(self, target, targetRb, maxPredictionTime, slowRadious);
        return Seek(self, futurePos);
    }

    public static Vector3 Evade(Transform self, Transform target, Rigidbody targetRb, float maxPredictionTime, float slowRadious)
    {
        Vector3 futurePos = CalculateFuture(self, target, targetRb, maxPredictionTime, slowRadious);
        return Flee(self, futurePos);
    }

    public static Vector3 Wander(Vector3 currentDirection, float maxAngleChange)
    {
        float randomAngle = Random.Range(-maxAngleChange, maxAngleChange);
        Quaternion rotation = Quaternion.Euler(0f, randomAngle, 0f);
        Vector3 newDirection = rotation * currentDirection;
        newDirection.y = 0f;
        return newDirection.normalized;
    }

    public static Vector3 CalculateFuture(Transform self, Transform target, Rigidbody targetRb, float maxPredictionTime, float slowRadious)
    {
        if (targetRb == null) return target.position;

        Vector3 toTarget = target.position - self.position;
        toTarget.y = 0f;
        float sqrDistance = toTarget.sqrMagnitude;

        float predictionTime = Mathf.Clamp(Mathf.Sqrt(sqrDistance) / slowRadious, 0f, maxPredictionTime);
        return target.position + targetRb.velocity * predictionTime;
    }
}
