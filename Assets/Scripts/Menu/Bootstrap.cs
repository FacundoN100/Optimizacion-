using UnityEngine;

// Bootstrap / composition root: el punto de arranque del juego.
// Es el ÚNICO Singleton permitido por la consigna. Existe para garantizar que
// haya un solo Bootstrap vivo aunque se vuelva al menú, NO para que otros
// sistemas accedan a cosas a través de él: para eso está el ServiceLocator.
public class Bootstrap : MonoBehaviour
{
    public static Bootstrap Instance { get; private set; }

    private void Awake()
    {
        // Si ya hay un Bootstrap vivo (por ejemplo, al volver al menú),
        // este es un duplicado: se destruye y no registra nada.
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Este objeto sobrevive a los cambios de escena, así los servicios
        // que registra siguen funcionando durante todo el juego.
        DontDestroyOnLoad(gameObject);

        RegisterServices();
    }

    // Acá se crean los servicios y se anotan en el Service Locator.
    // Cada servicio nuevo del juego se agrega en este método.
    private void RegisterServices()
    {
        // Caching (Unidad 02): el AudioSource se busca una sola vez.
        AudioSource musicSource = GetComponent<AudioSource>();

        // Se registra con su INTERFAZ, no con la clase concreta.
        // El <IAudioSettings> es obligatorio: sin él quedaría registrado como
        // AudioSettingsService y Get<IAudioSettings>() no lo encontraría.
        ServiceLocator.Register<IAudioSettings>(new AudioSettingsService(musicSource));
    }
}
