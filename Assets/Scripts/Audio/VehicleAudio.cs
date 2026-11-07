using System;
using UnityEngine;
using UnityEngine.Audio;

public sealed class VehicleAudio : IDisposable
{
    private readonly AudioSource engine;
    private readonly AudioSource siren;
    private readonly float maxSpeed;
    private float sirenPosition;
    private bool running = true;
    private bool audible;
    private bool disposed;

    public Rigidbody Body { get; }
    public bool IsPolice { get; }
    public bool IsAlive => !disposed && Body != null;
    public bool IsRunning => running;

    public VehicleAudio(Rigidbody body, AudioConfig config, bool police, float maxSpeed, AudioMixerGroup output)
    {
        Body = body;
        IsPolice = police;
        this.maxSpeed = Mathf.Max(1f, maxSpeed);
        audible = !police;
        engine = CreateSource(config.engine, output, police ? 80 : 32);
        if (police)
        {
            siren = CreateSource(config.siren, output, 24);
            siren.dopplerLevel = 0.3f;
            if (config.siren != null)
                sirenPosition = Mathf.Repeat(body.GetInstanceID() * 0.381966f, 1f) * config.siren.length;
        }
    }

    private AudioSource CreateSource(AudioClip clip, AudioMixerGroup output, int priority)
    {
        var source = Body.gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.spatialBlend = IsPolice ? 1f : 0f;
        source.priority = priority;
        source.dopplerLevel = IsPolice ? 1f : 0f;
        source.velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
        source.minDistance = 12f;
        source.maxDistance = 80f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.outputAudioMixerGroup = output;
        return source;
    }

    public void SetAudible(bool value) => audible = value;

    public void SetRunning(bool value)
    {
        running = value;
        if (!value)
        {
            if (engine != null) engine.Stop();
            if (siren != null)
            {
                if (siren.isPlaying) sirenPosition = siren.time;
                siren.Stop();
            }
        }
    }

    public void Tick(float deltaTime)
    {
        if (!IsAlive || engine == null) return;
        bool active = running && audible && Body.gameObject.activeInHierarchy;
        float speed = Vector3.ProjectOnPlane(Body.velocity, Vector3.up).magnitude;
        float throttle = Mathf.Clamp01(speed / maxSpeed);
        engine.pitch = Mathf.Lerp(engine.pitch, Mathf.Lerp(0.8f, 2.1f, throttle), 1f - Mathf.Exp(-6f * deltaTime));
        SetVolume(engine, active ? Mathf.Lerp(0.1f, 0.3f, throttle) : 0f, deltaTime);
        if (siren != null)
        {
            if (!AudioListener.pause && siren.clip != null && siren.clip.length > 0f)
                sirenPosition = siren.isPlaying ? siren.time
                    : Mathf.Repeat(sirenPosition + (running ? deltaTime : 0f), siren.clip.length);
            SetVolume(siren, active ? 0.55f : 0f, deltaTime);
        }
    }

    private void SetVolume(AudioSource source, float target, float deltaTime)
    {
        source.volume = Mathf.MoveTowards(source.volume, target, deltaTime * 2f);
        if (target > 0f && source.clip != null && !source.isPlaying && !AudioListener.pause)
        {
            source.Play();
            if (source == siren) source.time = sirenPosition;
        }
        else if (target == 0f && source.volume == 0f && source.isPlaying)
        {
            if (source == siren) sirenPosition = source.time;
            source.Stop();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (engine != null) UnityEngine.Object.Destroy(engine);
        if (siren != null) UnityEngine.Object.Destroy(siren);
    }
}
