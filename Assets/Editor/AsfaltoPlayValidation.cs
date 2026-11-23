using System;
using System.IO;
using System.Text;
using TMPro;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only observation of manual Play tests. It never loads or edits scenes.
[InitializeOnLoad]
public static class AsfaltoPlayValidation
{
    private const string LogPath = "Temp/AsfaltoPlayValidation.txt";
    private const int SampleCount = 600;
    private static readonly long[] FrameTimes = new long[SampleCount];
    private static readonly float[] FrameSeconds = new float[SampleCount];
    private static ProfilerRecorder mainThread;
    private static int collected;
    private static int warmup;
    private static int lastFrame = -1;
    private static string sceneName;
    private static bool pending;
    private static double snapshotAt;
    private static AudioSource observedMusic;
    private static Canvas tutorialOverlay;
    private static int lastSourceId;
    private static int lastClipId;
    private static int lastSamples;
    private static string previousScene;
    private static int previousSourceId;
    private static int previousClipId;
    private static int previousSamples;

    static AsfaltoPlayValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += StopRecorder;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(LogPath, "Manual editor Play validation | " + DateTime.Now.ToString("O") + "\n");
            ResetObservation();
        }
        else if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Also covers Play with scene reload disabled.
            if (!pending) ScheduleSnapshot(SceneManager.GetActiveScene());
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Append("[END] Play stopped.\n");
            ResetObservation();
        }
    }

    private static void ResetObservation()
    {
        StopRecorder();
        pending = false;
        observedMusic = null;
        tutorialOverlay = null;
        sceneName = previousScene = null;
        lastSourceId = lastClipId = lastSamples = 0;
        previousSourceId = previousClipId = previousSamples = 0;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!EditorApplication.isPlaying || mode == LoadSceneMode.Additive) return;
        ScheduleSnapshot(scene);
    }

    private static void ScheduleSnapshot(Scene scene)
    {
        StopRecorder();
        previousScene = sceneName;
        previousSourceId = lastSourceId;
        previousClipId = lastClipId;
        previousSamples = lastSamples;
        sceneName = scene.name;
        pending = true;
        snapshotAt = EditorApplication.timeSinceStartup + 0.5;
        observedMusic = null;
        tutorialOverlay = null;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
        if (pending && EditorApplication.timeSinceStartup >= snapshotAt)
        {
            pending = false;
            Snapshot();
        }

        // Cached source and fixed sample buffer: no searches or allocations per frame.
        if (observedMusic != null && observedMusic.clip != null)
        {
            lastSourceId = observedMusic.GetInstanceID();
            lastClipId = observedMusic.clip.GetInstanceID();
            lastSamples = observedMusic.timeSamples;
        }
        if (!mainThread.Valid || collected >= SampleCount || lastFrame == Time.frameCount) return;
        if (tutorialOverlay != null && tutorialOverlay.enabled) return;
        lastFrame = Time.frameCount;
        if (warmup-- > 0) return;
        long value = mainThread.LastValue;
        if (value <= 0) return;
        float frameSeconds = Time.unscaledDeltaTime;
        if (frameSeconds <= 0f) return;
        FrameSeconds[collected] = frameSeconds;
        FrameTimes[collected++] = value;
        if (collected == SampleCount) WriteProfilerResult();
    }

    private static void Snapshot()
    {
        var result = new StringBuilder(1500);
        result.Append("\n[SCENE] ").Append(sceneName).Append(" | frame ").Append(Time.frameCount).Append('\n');
        result.Append("Selection prefs: ").Append(CarModelChoice.ReadSelection()).Append(" / ")
            .Append(PlayerPrefs.GetString(CarModelChoice.MaterialPreference, "(unset)")).Append('\n');
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.sharedMesh) continue;
                string assetPath = AssetDatabase.GetAssetPath(filter.sharedMesh);
                bool player = assetPath == "Assets/Models/PlayerCar.fbx";
                if (!player && assetPath != "Assets/Models/Police/PoliceCar.fbx") continue;
                int lights = 0;
                foreach (var lamp in filter.GetComponentsInChildren<Light>())
                    if (lamp.enabled) lights++;
                int effects = 0;
                foreach (var surface in filter.GetComponentsInChildren<MeshRenderer>())
                    if (surface.enabled && surface.sharedMaterial != null &&
                        surface.sharedMaterial.shader.name == "Asfalto Rebelde/Police glass and spill") effects++;
                result.Append(player ? "Player=" : "Patrol=").Append(filter.name).Append(" effectRenderers=").Append(effects)
                    .Append(" realtimeLights=").Append(lights).Append('\n');
            }
        }
        AudioConfig config = Resources.Load<AudioConfig>("Audio/AudioConfig");
        Bootstrap bootstrap = Bootstrap.Instance;
        if (bootstrap != null)
        {
            foreach (AudioSource source in bootstrap.GetComponents<AudioSource>())
            {
                result.Append("Audio sourceId=").Append(source.GetInstanceID()).Append(" clip=")
                    .Append(source.clip != null ? source.clip.name : "none").Append(" playing=")
                    .Append(source.isPlaying).Append(" timeSamples=").Append(source.clip != null ? source.timeSamples : 0)
                    .Append(" volume=").Append(source.volume.ToString("F3")).Append('\n');
                if (source.isPlaying && config != null && source.clip == (IsMenu(sceneName) ? config.menuMusic : config.gameplayMusic))
                    observedMusic = source;
            }
        }
        if (IsMenu(previousScene) && IsMenu(sceneName) && observedMusic != null)
        {
            result.Append("[CONTINUITY] ").Append(previousScene).Append(" -> ").Append(sceneName)
                .Append(" sameSource=").Append(previousSourceId == observedMusic.GetInstanceID())
                .Append(" sameClip=").Append(previousClipId == observedMusic.clip.GetInstanceID())
                .Append(" samplesBefore=").Append(previousSamples)
                .Append(" samplesAfter=").Append(observedMusic.timeSamples).Append('\n');
        }
        foreach (GameManager manager in UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None))
        {
            using (var serialized = new SerializedObject(manager))
            {
                SerializedProperty models = serialized.FindProperty("garage.models");
                if (models != null && models.arraySize > 0)
                {
                    int visible = 0;
                    for (int i = 0; i < models.arraySize; i++)
                    {
                        var model = models.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                        if (model == null) continue;
                        if (model.activeInHierarchy) visible++;
                        result.Append("Garage model ").Append(i).Append(": ").Append(model.name)
                            .Append(" active=").Append(model.activeInHierarchy).Append('\n');
                    }
                    result.Append("Garage visible models=").Append(visible).Append('/').Append(models.arraySize).Append('\n');
                    var title = serialized.FindProperty("garage.modelName")?.objectReferenceValue as TMP_Text;
                    if (title != null) result.Append("Garage selected title=").Append(title.text).Append('\n');
                }
                var player = serialized.FindProperty("carController.body")?.objectReferenceValue as Rigidbody;
                if (player != null)
                {
                    result.Append("Player position=").Append(player.position)
                        .Append(" camera=").Append(Camera.main != null ? Camera.main.transform.position : Vector3.zero).Append('\n');
                    foreach (RaycastHit hit in Physics.RaycastAll(player.position + Vector3.up * 8, Vector3.down, 25))
                        result.Append("Ground probe=").Append(hit.collider.name).Append(" top=").Append(hit.point.y).Append('\n');
                    foreach (Renderer renderer in player.GetComponentsInChildren<Renderer>())
                    {
                        if (renderer is ParticleSystemRenderer) continue;
                        result.Append("Player renderer=").Append(renderer.name).Append(" material=")
                            .Append(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "none");
                        if (Camera.main != null)
                            result.Append(" viewport=").Append(Camera.main.WorldToViewportPoint(renderer.bounds.center))
                                .Append(" bounds=").Append(renderer.bounds).Append(" enabled=").Append(renderer.enabled);
                        result.Append('\n');
                    }
                }
            }
        }
        Append(result.ToString());
        TutorialManager tutorial = UnityEngine.Object.FindFirstObjectByType<TutorialManager>();
        if (tutorial != null)
        {
            using (var serialized = new SerializedObject(tutorial))
                tutorialOverlay = serialized.FindProperty("canvas").objectReferenceValue as Canvas;
        }
        collected = 0;
        warmup = 60;
        lastFrame = Time.frameCount;
        mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
        if (!mainThread.Valid) Append("[PROFILER] Main Thread recorder unavailable.\n");
    }

    private static bool IsMenu(string name) => name == "MainMenu" || name == "Garage";

    private static void WriteProfilerResult()
    {
        StopRecorder();
        long total = 0;
        for (int i = 0; i < SampleCount; i++) total += FrameTimes[i];
        Array.Sort(FrameTimes);
        double meanMs = total / (double)SampleCount / 1000000.0;
        double p95Ms = FrameTimes[(int)(SampleCount * 0.95) - 1] / 1000000.0;
        double elapsed = 0;
        int withinBudget = 0;
        foreach (float seconds in FrameSeconds)
        {
            elapsed += seconds;
            if (seconds <= 1.0 / 60.0) withinBudget++;
        }
        Array.Sort(FrameSeconds);
        Append("[PROFILER] " + sceneName + " | " + QualitySettings.names[QualitySettings.GetQualityLevel()]
            + " | Game view " + Screen.width + "x" + Screen.height
            + " | editor Main Thread | " + SampleCount + " frames after 60-frame warmup | meanMs="
            + meanMs.ToString("F3") + " p95Ms=" + p95Ms.ToString("F3")
            + " maxMs=" + (FrameTimes[SampleCount - 1] / 1000000.0).ToString("F3")
            + " | Game view FPS=" + (SampleCount / elapsed).ToString("F1")
            + " | Game view p95Ms=" + (FrameSeconds[(int)(SampleCount * 0.95) - 1] * 1000.0).ToString("F3")
            + " | frames <= 16.667 ms=" + withinBudget + "/" + SampleCount + ". Editor overhead included.\n");
        if (sceneName == "SampleScene") LogPlayerView();
    }

    private static void LogPlayerView()
    {
        Camera camera = Camera.main;
        GameObject player = GameObject.FindWithTag("Player");
        if (camera == null || player == null || !player.TryGetComponent(out MeshRenderer body)) return;
        Vector3 toBody = body.bounds.center - camera.transform.position;
        var result = new StringBuilder();
        result.Append("Player in race: world=").Append(body.bounds.center)
            .Append(" viewport=").Append(camera.WorldToViewportPoint(body.bounds.center))
            .Append(" visible=").Append(body.isVisible).Append('\n');
        foreach (RaycastHit hit in Physics.RaycastAll(camera.transform.position, toBody.normalized, toBody.magnitude + 4f))
            result.Append("Sight line: ").Append(hit.collider.name).Append(" hit=").Append(hit.point).Append('\n');
        Append(result.ToString());
    }

    private static void StopRecorder()
    {
        if (mainThread.Valid) mainThread.Dispose();
        mainThread = default;
    }

    private static void Append(string text)
    {
        Directory.CreateDirectory("Temp");
        File.AppendAllText(LogPath, text);
    }
}
