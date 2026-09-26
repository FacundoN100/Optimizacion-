using UnityEngine;
using UnityEngine.SceneManagement;

public class ScriptMenu : MonoBehaviour
{
    [SerializeField] private GameObject MenuPanel;
    [SerializeField] private GameObject OptionsPanel;
    [SerializeField] private GameObject AudioAndVideoPanel;
    [SerializeField] private GameObject ControllsPanel;
    [SerializeField] private GameObject CreditsPanel;
    [SerializeField] private GameObject ExitPanel;
    [SerializeField] private GameObject SkinPanel;
    void Awake()
    {
        int anchoObjetivo = 1334;
        int altoObjetivo = 720;

        if (Screen.currentResolution.width >= anchoObjetivo)
        {
            Screen.SetResolution(anchoObjetivo, altoObjetivo, true);
        }
    }
    void Start()
    {
        Application.targetFrameRate = 63;
    }
    public void Play()
    {
        MenuPanel.SetActive(false);
        SkinPanel.SetActive(true);
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
        AudioListener.volume = volume;
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

    public void ExitNot()
    {
        MenuPanel.SetActive(true);
        ExitPanel.SetActive(false);
    }
}
