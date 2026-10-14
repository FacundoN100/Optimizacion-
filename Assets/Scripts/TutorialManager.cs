using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Image image;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameManager gameManager;
    private int currentIndex = 0;

    void Start()
    {
        if (sprites.Length > 0 && image != null)
        {
            image.sprite = sprites[currentIndex];
        }
        gameManager.SetCarMovement(false);
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
            gameManager.StartClock();
            gameManager.SetCarMovement(true);
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
