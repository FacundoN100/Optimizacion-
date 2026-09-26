using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Image image;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Clock clock;
    [SerializeField] private CarController carController;
    private int currentIndex = 0;

    void Start()
    {
        if (sprites.Length > 0 && image != null)
        {
            image.sprite = sprites[currentIndex];
        }
        carController.ImDead(false);
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
            clock.NowYouCanCount();
            carController.ImDead(true);
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
