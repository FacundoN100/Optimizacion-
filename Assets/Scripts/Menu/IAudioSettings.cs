// Puerto (Arquitectura Hexagonal): define QUÉ se puede hacer con el volumen,
// nunca CÓMO se hace. Quien lo use depende de esta interfaz, no de una clase concreta.
public interface IAudioSettings
{
    float GetMasterVolume();
    void SetMasterVolume(float volume);
}
