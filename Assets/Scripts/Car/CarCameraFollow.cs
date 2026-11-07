using System;
using UnityEngine;

public sealed class CarCameraFollow : IDisposable
{
    private readonly Rigidbody body;
    private readonly Transform cameraTransform;
    private readonly Vector3 localPosition;
    private readonly Quaternion localRotation;
    private readonly Vector3 positionOffset;
    private readonly Quaternion rotationOffset;
    private Quaternion previousRotation;
    private Quaternion currentRotation;

    public CarCameraFollow(Rigidbody target, Camera camera)
    {
        body = target;
        if (camera == null || camera.transform.parent != body.transform) return;
        cameraTransform = camera.transform;
        localPosition = cameraTransform.localPosition;
        localRotation = cameraTransform.localRotation;
        Quaternion inverse = Quaternion.Inverse(body.rotation);
        positionOffset = inverse * (cameraTransform.position - body.position);
        rotationOffset = inverse * cameraTransform.rotation;
        previousRotation = currentRotation = body.rotation;
    }

    public void CapturePhysicsRotation()
    {
        if (body == null || cameraTransform == null) return;
        previousRotation = currentRotation;
        currentRotation = body.rotation;
    }

    public void TickRender()
    {
        if (body == null || cameraTransform == null) return;
        float blend = Mathf.Clamp01((float)((Time.timeAsDouble - Time.fixedTimeAsDouble) / Time.fixedDeltaTime));
        Quaternion rotation = Quaternion.Slerp(previousRotation, currentRotation, blend);
        cameraTransform.SetPositionAndRotation(body.transform.position + rotation * positionOffset,
            rotation * rotationOffset);
    }

    public void Dispose()
    {
        if (cameraTransform == null) return;
        cameraTransform.localPosition = localPosition;
        cameraTransform.localRotation = localRotation;
    }
}
