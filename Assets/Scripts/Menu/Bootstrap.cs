using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    public static Bootstrap Instance { get; private set; }
    private AudioManager audio;
    private CustomUpdateManager updates;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance == null) new GameObject("Bootstrap").AddComponent<Bootstrap>();
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        audio = new AudioManager(gameObject, Resources.Load<AudioConfig>("Audio/AudioConfig"));
        ServiceLocator.Register<IAudioSettings>(audio);
        ServiceLocator.Register<IGameAudio>(audio);
        updates = new CustomUpdateManager((delta, unscaled) => audio.Tick(unscaled), null);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && Instance == this) PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        updates?.Dispose();
        ServiceLocator.Unregister<IGameAudio>(audio);
        ServiceLocator.Unregister<IAudioSettings>(audio);
        audio?.Dispose();
        Instance = null;
    }
}
