using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class SliderController : MonoBehaviour
{
    private Slider slider;
    private SettingsManager settingsManager;

    void Start()
    {
        slider = GetComponent<Slider>();
        settingsManager = SettingsManager.Instance;
        slider.onValueChanged.AddListener(value => settingsManager.SetMasterVolume(value));
        slider.value = settingsManager.GetMasterVolume();
    }
}
