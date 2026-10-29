using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Image image;
    [SerializeField] private Canvas canvas;

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

}