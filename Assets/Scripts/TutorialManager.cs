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

    
    private IGameplayControl gameplay;
    private int currentIndex = 0;

    
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

    
    public void TogglePause(InputAction.CallbackContext context)
    {
       
        if (!context.performed) return;

        
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