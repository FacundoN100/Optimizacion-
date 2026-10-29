using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class SliderController : IDisposable
{
    [SerializeField] private Slider slider;
    private IAudioSettings audioSettings;

    public bool IsConfigured => slider != null;

    public void Initialize(IAudioSettings settings)
    {
        Dispose();
        if (slider == null || settings == null) return;
        audioSettings = settings;
        slider.SetValueWithoutNotify(audioSettings.GetMasterVolume());
        slider.onValueChanged.AddListener(SetVolume);
    }

    private void SetVolume(float volume)
    {
        if (audioSettings != null) audioSettings.SetMasterVolume(volume);
    }

    public void Dispose()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(SetVolume);
        audioSettings = null;
    }
}
