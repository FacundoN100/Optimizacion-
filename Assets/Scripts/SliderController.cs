using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class SliderController : IDisposable
{
    [SerializeField] private Slider slider;
    private SettingsManager settingsManager;

    public void Initialize(SettingsManager settings)
    {
        Dispose();
        if (slider == null || settings == null) return;
        settingsManager = settings;
        slider.SetValueWithoutNotify(settingsManager.GetMasterVolume());
        slider.onValueChanged.AddListener(SetVolume);
    }

    private void SetVolume(float volume)
    {
        if (settingsManager != null) settingsManager.SetMasterVolume(volume);
    }

    public void Dispose()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(SetVolume);
        settingsManager = null;
    }
}
