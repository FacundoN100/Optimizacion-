using UnityEngine;


public class AudioSettingsService : IAudioSettings
{
    private readonly AudioSource audioSource;

   
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
