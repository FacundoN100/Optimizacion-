using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Game/Audio Config")]
public sealed class AudioConfig : ScriptableObject
{
    [InspectorName("Música del menú")] public AudioClip menuMusic;
    [InspectorName("Música de la partida")] public AudioClip gameplayMusic;
    [InspectorName("Motor del auto")] public AudioClip engine;
    [InspectorName("Sirena de policía")] public AudioClip siren;
    [InspectorName("Clic de botones")] public AudioClip buttonClick;
    [SerializeField, HideInInspector] private AudioMixer mixer;

    public AudioMixer Mixer => mixer;
}
