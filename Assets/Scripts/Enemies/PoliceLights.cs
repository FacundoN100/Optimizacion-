using LightMaster;
using UnityEngine;

public sealed class PoliceLights
{
    private static readonly int Pulse = Shader.PropertyToID("_Pulse");
    public const string RigName = "Police Lights";
    public const string GlassName = "Cristales";
    public const string SpillName = "Reflejo en el asfalto";
    private readonly Renderer glass;
    private readonly Renderer spill;
    private readonly MaterialPropertyBlock properties;
    private readonly float onTime;
    private readonly float beatLength;
    private float elapsed;
    private int currentState = -1;
    private bool stopped;

    public PoliceLights(Transform car, TimerLightVariables timing)
    {
        onTime = Mathf.Max(0.02f, timing != null ? timing.TimerOnLength : 0.09f);
        beatLength = onTime + Mathf.Max(0.02f, timing != null ? timing.TimerOffLength : 0.06f);
        elapsed = -Mathf.Max(0f, timing != null ? timing.StartDelay : 0f);
        Transform rig = car != null ? car.Find(RigName) : null;
        if (rig != null)
        {
            glass = rig.Find(GlassName)?.GetComponent<Renderer>();
            spill = rig.Find(SpillName)?.GetComponent<Renderer>();
            properties = new MaterialPropertyBlock();
        }
        ApplyState(0);
        Tick(0f);
    }

    public void Tick(float deltaTime)
    {
        if (stopped || glass == null) return;
        elapsed += Mathf.Max(0f, deltaTime);
        if (elapsed < 0f) return;
        float cycleTime = Mathf.Repeat(elapsed, beatLength * 4f);
        int beat = Mathf.Min(3, Mathf.FloorToInt(cycleTime / beatLength));
        bool lit = cycleTime - beat * beatLength < onTime;
        ApplyState(lit ? (beat < 2 ? 1 : 2) : 0);
    }

    public void Stop()
    {
        stopped = true;
        ApplyState(0);
    }

    private void ApplyState(int state)
    {
        if (state == currentState) return;
        currentState = state;
        if (properties == null) return;
        properties.SetVector(Pulse, new Vector4(state == 1 ? 1f : 0f, state == 2 ? 1f : 0f, 0f, 0f));
        if (glass != null) glass.SetPropertyBlock(properties);
        if (spill != null) spill.SetPropertyBlock(properties);
    }
}
