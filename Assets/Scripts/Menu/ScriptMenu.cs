using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class ScriptMenu
{
    [SerializeField] private GameObject MenuPanel;
    [SerializeField] private GameObject OptionsPanel;
    [SerializeField] private GameObject AudioAndVideoPanel;
    [SerializeField] private GameObject ControllsPanel;
    [SerializeField] private GameObject CreditsPanel;
    [SerializeField] private GameObject ExitPanel;
    [SerializeField] private GameObject SkinPanel;
    [SerializeField] private SliderController volumeSlider;
    public bool IsConfigured => MenuPanel != null;

    public void Initialize(IAudioSettings audioSettings)
    {
        if (volumeSlider != null && volumeSlider.IsConfigured) volumeSlider.Initialize(audioSettings);
    }
    public void Play()
    {
        SceneManager.LoadScene("Garage");
    }

    public void BackSkins()
    {
        SkinPanel.SetActive(false);
        MenuPanel.SetActive(true);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("SampleScene");
    }
    public void Options()
    {
        OptionsPanel.SetActive(true);
        MenuPanel.SetActive(false);
    }

    public void AudioAndVideo()
    {
        AudioAndVideoPanel.SetActive(true);
        OptionsPanel.SetActive(false);
    }

    public void SetMasterVolume(float volume)
    {
        if (ServiceLocator.TryGet<IAudioSettings>(out var audio)) audio.SetMasterVolume(volume);
    }

    public void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
    }
    public void AudioAndVideoBackOption()
    {
        AudioAndVideoPanel.SetActive(false);
        OptionsPanel.SetActive(true);
    }

    public void controls()
    {
        ControllsPanel.SetActive(true);
        OptionsPanel.SetActive(false);
    }

    public void ControlsBackOption()
    {
        ControllsPanel.SetActive(false);
        OptionsPanel.SetActive(true);
    }


    public void backOptions()
    {

        MenuPanel.SetActive(true);
        OptionsPanel.SetActive(false);
    }
    public void credits()
    {
        MenuPanel.SetActive(false);
        CreditsPanel.SetActive(true);
    }

    public void backCredts()
    {

        MenuPanel.SetActive(true);
        CreditsPanel.SetActive(false);
    }
    public void Exit()
    {
        MenuPanel.SetActive(false);
        ExitPanel.SetActive(true);
    }

    public void ExitYes()
    {
        Application.Quit();
    }

    public void Dispose() => volumeSlider?.Dispose();

    public void ExitNot()
    {
        MenuPanel.SetActive(true);
        ExitPanel.SetActive(false);
    }
}
