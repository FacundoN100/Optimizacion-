using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen;

public sealed class AudioManager : IGameAudio, IDisposable
{
    private const string VolumeKey = "Audio.MasterVolume";
    private static readonly ProfilerMarker TickMarker = new ProfilerMarker("GameAudio.Tick");
    private readonly AudioConfig config;
    private readonly AudioSource musicA;
    private readonly AudioSource musicB;
    private readonly AudioSource buttons;
    private readonly AudioMixerGroup output;
    private readonly List<Button> boundButtons = new List<Button>(24);
    private readonly UnityAction buttonClick;
    private readonly List<VehicleAudio> vehicles = new List<VehicleAudio>(8);
    private AudioSource musicTarget;
    private Rigidbody player;
    private float masterVolume;
    private float budgetTimer;
    private bool volumeDirty = true;
    private bool paused;

    public AudioManager(GameObject owner, AudioConfig config)
    {
        this.config = config;
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.8f));
        AudioListener.volume = 1f;
        if (config != null && config.Mixer != null)
            foreach (AudioMixerGroup group in config.Mixer.FindMatchingGroups("Master"))
                if (group.name == "Master") { output = group; break; }
        musicA = CreateMusicSource(owner);
        musicB = CreateMusicSource(owner);
        buttons = owner.AddComponent<AudioSource>();
        buttons.playOnAwake = false;
        buttons.ignoreListenerPause = true;
        buttons.priority = 16;
        buttons.outputAudioMixerGroup = output;
        buttonClick = PlayButtonClick;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private AudioSource CreateMusicSource(GameObject owner)
    {
        var source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
        source.spatialBlend = 0f;
        source.priority = 0;
        source.ignoreListenerPause = true;
        source.outputAudioMixerGroup = output;
        return source;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        if (config != null)
        {
            foreach (AudioSource source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (source != musicA && source != musicB
                    && (source.clip == config.menuMusic || source.clip == config.gameplayMusic)) source.Stop();
        }
        BindButtons();
        SetPaused(false);
        SetGameplayMusic(scene.name != "MainMenu");
    }

    public void SetGameplayMusic(bool gameplay)
    {
        if (config == null) return;
        AudioClip clip = gameplay ? config.gameplayMusic : config.menuMusic;
        if (clip == null || (musicTarget != null && musicTarget.clip == clip)) return;
        musicTarget = musicTarget == musicA ? musicB : musicA;
        musicTarget.Stop();
        musicTarget.clip = clip;
        musicTarget.volume = 0f;
        musicTarget.Play();
    }

    public float GetMasterVolume() => masterVolume;

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, masterVolume);
        volumeDirty = true;
    }

    public void SetPaused(bool value) => paused = value;

    public void PlayButtonClick()
    {
        if (buttons != null && config != null && config.buttonClick != null)
            buttons.PlayOneShot(config.buttonClick, 0.7f);
    }

    private void BindButtons()
    {
        UnbindButtons();
        foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.GetComponent<OnScreenButton>() != null) continue;
            button.onClick.AddListener(buttonClick);
            boundButtons.Add(button);
        }
    }

    private void UnbindButtons()
    {
        foreach (Button button in boundButtons)
            if (button != null) button.onClick.RemoveListener(buttonClick);
        boundButtons.Clear();
    }

    public VehicleAudio CreateVehicleAudio(Rigidbody body, bool police, float maxSpeed)
    {
        if (config == null || body == null) return null;
        var audio = new VehicleAudio(body, config, police, maxSpeed, output);
        vehicles.Add(audio);
        if (!police) player = body;
        budgetTimer = 0f;
        return audio;
    }

    public void Tick(float unscaledDeltaTime)
    {
        using var sample = TickMarker.Auto();
        bool pauseEffects = paused || Time.timeScale <= 0f;
        if (AudioListener.pause != pauseEffects) AudioListener.pause = pauseEffects;
        if (volumeDirty)
        {
            float decibels = masterVolume <= 0f ? -80f : Mathf.Log10(masterVolume) * 20f;
            if (config != null && config.Mixer != null) config.Mixer.SetFloat("MasterVolume", decibels);
            else AudioListener.volume = masterVolume;
            volumeDirty = false;
        }
        FadeMusic(musicA, unscaledDeltaTime);
        FadeMusic(musicB, unscaledDeltaTime);
        for (int i = vehicles.Count - 1; i >= 0; i--)
        {
            if (vehicles[i].IsAlive) continue;
            vehicles[i].Dispose();
            vehicles.RemoveAt(i);
        }
        budgetTimer -= unscaledDeltaTime;
        if (budgetTimer <= 0f)
        {
            UpdatePoliceBudget();
            budgetTimer = 0.25f;
        }
        for (int i = 0; i < vehicles.Count; i++) vehicles[i].Tick(unscaledDeltaTime);
    }

    private void FadeMusic(AudioSource source, float deltaTime)
    {
        if (source == null) return;
        source.volume = Mathf.MoveTowards(source.volume, source == musicTarget ? 0.4f : 0f, deltaTime / 0.75f);
        if (source != musicTarget && source.volume == 0f && source.isPlaying) source.Stop();
    }

    private void UpdatePoliceBudget()
    {
        for (int i = 0; i < vehicles.Count; i++)
        {
            VehicleAudio audio = vehicles[i];
            if (!audio.IsPolice) continue;
            if (!audio.IsRunning)
            {
                audio.SetAudible(false);
                continue;
            }
            float distance = player != null ? (audio.Body.position - player.position).sqrMagnitude : float.MaxValue;
            int nearer = 0;
            for (int j = 0; j < vehicles.Count; j++)
            {
                if (j == i || !vehicles[j].IsPolice || !vehicles[j].IsRunning || player == null) continue;
                float other = (vehicles[j].Body.position - player.position).sqrMagnitude;
                if (other < distance || (other == distance && j < i)) nearer++;
            }
            audio.SetAudible(distance < 80f * 80f && nearer < 3);
        }
    }

    public void Dispose()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindButtons();
        if (buttons != null) buttons.Stop();
        foreach (VehicleAudio audio in vehicles) audio.Dispose();
        vehicles.Clear();
        if (musicA != null) musicA.Stop();
        if (musicB != null) musicB.Stop();
        AudioListener.pause = false;
        PlayerPrefs.Save();
    }
}
