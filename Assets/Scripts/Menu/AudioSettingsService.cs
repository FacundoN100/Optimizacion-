using UnityEngine;

// Adaptador (Arquitectura Hexagonal): la implementación concreta de IAudioSettings.
// Es una clase pura de C# (no hereda de MonoBehaviour): no suma a la cuenta de MonoBehaviours.
public class AudioSettingsService : IAudioSettings
{
    private readonly AudioSource audioSource;

    // Inyección por constructor (Unidad 01, nivel 4): el servicio recibe lo que
    // necesita desde afuera, en vez de buscarlo él mismo.
    public AudioSettingsService(AudioSource source)
    {
        audioSource = source;
    }

    public float GetMasterVolume()
    {
        return audioSource.volume;
    }

    public void SetMasterVolume(float volume)
    {
        audioSource.volume = volume;
    }
}
