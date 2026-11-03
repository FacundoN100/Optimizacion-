using LightMaster;
using UnityEngine;

public sealed class PoliceLights
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly Color Red = new Color(1f, 0.025f, 0.025f, 1f);
    private static readonly Color Blue = new Color(0.025f, 0.15f, 1f, 1f);
    private readonly Renderer redLens;
    private readonly Renderer blueLens;
    private readonly Light beacon;
    private readonly MaterialPropertyBlock redProperties;
    private readonly MaterialPropertyBlock blueProperties;
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
        Transform rig = car != null ? car.Find("Police Lights") : null;
        if (rig != null)
        {
            Transform red = rig.Find("Red Beacon");
            Transform blue = rig.Find("Blue Beacon");
            if (red != null) redLens = red.GetComponent<Renderer>();
            if (blue != null) blueLens = blue.GetComponent<Renderer>();
            beacon = rig.GetComponentInChildren<Light>(true);
            redProperties = new MaterialPropertyBlock();
            blueProperties = new MaterialPropertyBlock();
        }
        ApplyState(0);
        Tick(0f);
    }

    public void Tick(float deltaTime)
    {
        if (stopped || redLens == null || blueLens == null) return;
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
        if (redLens != null)
        {
            redProperties.SetColor(BaseColor, state == 1 ? Red : new Color(0.07f, 0.002f, 0.002f, 1f));
            redLens.SetPropertyBlock(redProperties);
        }
        if (blueLens != null)
        {
            blueProperties.SetColor(BaseColor, state == 2 ? Blue : new Color(0.002f, 0.01f, 0.07f, 1f));
            blueLens.SetPropertyBlock(blueProperties);
        }
        if (beacon != null)
        {
            beacon.enabled = state != 0;
            if (state != 0) beacon.color = state == 1 ? Red : Blue;
        }
    }
}
