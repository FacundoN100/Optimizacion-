using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Image image;
    [SerializeField] private Canvas canvas;
    [SerializeField] private PauseMenu pauseMenu;

    // Ya no es una referencia directa al GameManager: es una interfaz
    // que se le pide al Service Locator.
    private IGameplayControl gameplay;
    private int currentIndex = 0;

    // Antes era Start(); la consigna solo permite Awake().
    private void Awake()
    {
        gameplay = ServiceLocator.Get<IGameplayControl>();

        if (sprites.Length > 0 && image != null)
        {
            image.sprite = sprites[currentIndex];
        }
        gameplay.SetCarMovement(false);

        if (pauseMenu != null && pauseMenu.IsConfigured) pauseMenu.Initialize();
    }

    // La llama la acción "Pause" del PlayerInput (tecla Escape), igual que el freno.
    public void TogglePause(InputAction.CallbackContext context)
    {
        // El PlayerInput avisa varias veces por cada toque (started, performed, canceled).
        // Solo se reacciona a "performed": si no, pausaría y despausaría en el mismo toque.
        if (!context.performed) return;

        // Mientras se ve el tutorial no se puede pausar.
        if (canvas.enabled) return;

        if (pauseMenu != null && pauseMenu.IsConfigured) pauseMenu.TogglePause();
    }

    public void NextImage()
    {
        if (currentIndex < sprites.Length - 1)
        {
            currentIndex++;
            image.sprite = sprites[currentIndex];
        }
        else
        {
            gameplay.StartClock();
            gameplay.SetCarMovement(true);
            canvas.enabled = false;
        }
    }

    public void PreviousImage()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            image.sprite = sprites[currentIndex];
        }
    }

    private void OnDestroy()
    {
        pauseMenu?.Dispose();
    }
}