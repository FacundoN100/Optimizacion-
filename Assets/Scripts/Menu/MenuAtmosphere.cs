using System;
using UnityEngine;

/// <summary>Presentacion del menu: reutiliza las balizas del gameplay sin otro MonoBehaviour.</summary>
[Serializable]
public sealed class MenuAtmosphere : IDisposable
{
    [SerializeField] private Transform[] policeCars;
    private PoliceLights[] lights;

    public bool IsConfigured => policeCars != null && policeCars.Length > 0;

    public void Initialize()
    {
        if (!IsConfigured) return;
        lights = new PoliceLights[policeCars.Length];
        for (int i = 0; i < policeCars.Length; i++)
        {
            lights[i] = new PoliceLights(policeCars[i], null);
            lights[i].Tick(i * 0.17f);
        }
    }

    public void Tick(float deltaTime)
    {
        if (lights == null) return;
        for (int i = 0; i < lights.Length; i++) lights[i].Tick(deltaTime);
    }

    public void Dispose()
    {
        if (lights == null) return;
        for (int i = 0; i < lights.Length; i++) lights[i].Stop();
        lights = null;
    }
}
