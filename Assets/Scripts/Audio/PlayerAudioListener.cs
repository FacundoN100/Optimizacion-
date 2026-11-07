using System;
using UnityEngine;

public sealed class PlayerAudioListener : IDisposable
{
    private readonly AudioListener cameraListener;
    private readonly AudioListener playerListener;
    private readonly Transform cameraTransform;
    private readonly bool cameraListenerEnabled;
    private bool disposed;

    public PlayerAudioListener(Rigidbody body, Camera camera)
    {
        if (body == null || camera == null) return;
        cameraListener = camera.GetComponent<AudioListener>();
        if (cameraListener == null || !cameraListener.enabled) return;
        cameraTransform = camera.transform;
        cameraListenerEnabled = cameraListener.enabled;
        cameraListener.enabled = false;
        var listener = new GameObject("Player Audio Listener");
        listener.transform.SetParent(body.transform, false);
        listener.transform.rotation = cameraTransform.rotation;
        playerListener = listener.AddComponent<AudioListener>();
        playerListener.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
    }

    public void TickRender()
    {
        if (!disposed && playerListener != null && cameraTransform != null)
            playerListener.transform.rotation = cameraTransform.rotation;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (playerListener != null)
        {
            playerListener.enabled = false;
            UnityEngine.Object.Destroy(playerListener.gameObject);
        }
        if (cameraListener != null) cameraListener.enabled = cameraListenerEnabled;
    }
}
