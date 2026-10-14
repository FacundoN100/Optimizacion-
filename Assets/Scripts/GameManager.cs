using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private List<GameObject> pointsArcs = new List<GameObject>();
    [SerializeField] private int arcsToActivate;
    [SerializeField] private CarController carController;
    [SerializeField] private Clock clock;
    [SerializeField] private ScriptMenu menu;
    [SerializeField] private SkinSelectorUI skinSelector;
    [SerializeField] private GameObject[] modelObjects;

    private CustomUpdateManager updateManager;
    private ApplyModel model;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (carController != null && carController.IsConfigured) carController.Initialize();
        if (clock != null && clock.IsConfigured) clock.Initialize(OnTimeExpired);
        if (menu != null && menu.IsConfigured) menu.Initialize();
        if (skinSelector != null && skinSelector.IsConfigured) skinSelector.Initialize();
        ConfigurePointArcs();
        ConfigureModels();
        if ((carController != null && carController.IsConfigured)
            || (clock != null && clock.IsConfigured))
            updateManager = new CustomUpdateManager(TickFrame, TickPhysics);
    }

    private void ConfigurePointArcs()
    {
        int countToActivate = Mathf.Min(arcsToActivate, pointsArcs.Count);
        List<GameObject> tempList = new List<GameObject>(pointsArcs);
        for (int i = 0; i < countToActivate; i++)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            GameObject arc = tempList[randomIndex];
            if (arc != null) arc.SetActive(false);
            tempList.RemoveAt(randomIndex);
        }
    }

    private void ConfigureModels()
    {
        if (modelObjects == null || modelObjects.Length == 0) return;
        var renderers = new List<Renderer>(modelObjects.Length);
        foreach (GameObject target in modelObjects)
        {
            if (target != null && target.TryGetComponent(out Renderer renderer))
                renderers.Add(renderer);
        }
        model = new ApplyModel(renderers.ToArray());
        model.LoadMaterial();
    }

    private void TickFrame(float deltaTime, float unscaledDeltaTime)
    {
        if (this != null && isActiveAndEnabled) clock?.Tick(deltaTime, unscaledDeltaTime);
    }

    private void TickPhysics(float deltaTime)
    {
        if (this != null && isActiveAndEnabled) carController?.TickPhysics(deltaTime);
    }

    private void OnTimeExpired() => SetCarMovement(false);

    private void OnDestroy()
    {
        updateManager?.Dispose();
        StopAllCoroutines();
        model?.Dispose();
        if (Instance == this) Instance = null;
    }

    public void SetCarMovement(bool enabled) => carController?.SetMovementEnabled(enabled);
    public void StartClock() => clock?.StartCounting();
    public void StopClock() => clock?.Stop();
    public void Break(InputAction.CallbackContext context) => carController?.Break(context);

    public void UpdateCatalog()
    {
        if (model != null) StartCoroutine(model.UpdateCatalog());
    }

    // UnityEvents target this scene component; the behavior lives in plain classes.
    public void Play() => menu?.Play();
    public void BackSkins() => menu?.BackSkins();
    public void StartGame() => menu?.StartGame();
    public void Options() => menu?.Options();
    public void AudioAndVideo() => menu?.AudioAndVideo();
    public void SetMasterVolume(float volume) => menu?.SetMasterVolume(volume);
    public void SetFullscreen(bool fullscreen) => menu?.SetFullscreen(fullscreen);
    public void AudioAndVideoBackOption() => menu?.AudioAndVideoBackOption();
    public void controls() => menu?.controls();
    public void ControlsBackOption() => menu?.ControlsBackOption();
    public void backOptions() => menu?.backOptions();
    public void credits() => menu?.credits();
    public void backCredts() => menu?.backCredts();
    public void Exit() => menu?.Exit();
    public void ExitYes() => menu?.ExitYes();
    public void ExitNot() => menu?.ExitNot();
    public void NextSkin() => skinSelector?.NextSkin();
    public void PreviousSkin() => skinSelector?.PreviousSkin();
}
