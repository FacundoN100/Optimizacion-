using System;
using UnityEngine;
using UnityEngine.UI;


[Serializable]
public class PauseMenu : IDisposable
{
    [SerializeField] private Canvas pauseCanvas;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    private readonly ScenesController scenes = new ScenesController();
    private bool isPaused;

    // true solo si se asignaron todas las referencias en el Inspector.
    public bool IsConfigured => pauseCanvas != null && resumeButton != null
                                && retryButton != null && menuButton != null;

    public void Initialize()
    {
        // El menú arranca oculto. Se apaga el componente Canvas (como hace el tutorial),
        // en vez de desactivar todo el objeto.
        pauseCanvas.enabled = false;

        // Se le dice a cada botón qué método ejecutar al tocarlo, por código.
        resumeButton.onClick.AddListener(Resume);
        retryButton.onClick.AddListener(Retry);
        menuButton.onClick.AddListener(BackToMenu);
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    private void Pause()
    {
        isPaused = true;
        pauseCanvas.enabled = true;
        // PENDIENTE: frenar la lógica del juego cuando esté el Custom Update Manager.
    }

    private void Resume()
    {
        isPaused = false;
        pauseCanvas.enabled = false;
        // PENDIENTE: reanudar la lógica del juego cuando esté el Custom Update Manager.
    }

    // Antes de cambiar de escena se reanuda el juego, para que la próxima
    // partida (o el menú) no arranque pausada.
    private void Retry()
    {
        Resume();
        scenes.Retry();
    }

    private void BackToMenu()
    {
        Resume();
        scenes.BackToMenu();
    }

    // Lo que se suscribe, se desuscribe (Unidad 03: evitar memory leaks).
    public void Dispose()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        if (menuButton != null) menuButton.onClick.RemoveListener(BackToMenu);
    }
}