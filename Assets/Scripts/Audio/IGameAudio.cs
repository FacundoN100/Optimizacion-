using UnityEngine;

public interface IGameAudio : IAudioSettings
{
    void SetGameplayMusic(bool gameplay);
    void SetPaused(bool paused);
    void PlayButtonClick();
    VehicleAudio CreateVehicleAudio(Rigidbody body, bool police, float maxSpeed);
}
