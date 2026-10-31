using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour, IGameplayControl
{
    [SerializeField] private List<GameObject> pointsArcs = new List<GameObject>();
    [SerializeField] private int arcsToActivate;
    [SerializeField] private CarController carController;
    [SerializeField] private Clock clock;
    [SerializeField] private ScriptMenu menu;
    [SerializeField] private SkinSelectorUI skinSelector;
    [SerializeField] private GameObject[] modelObjects;
    [SerializeField] private FinishLine finishLine;
    [SerializeField] private PointsArc[] collectibleArcs;
    [SerializeField] private CombatSystem combat;

    private CustomUpdateManager updateManager;
    private ApplyModel model;
    private readonly ScenesController scenes = new ScenesController();
    private Dictionary<Collider, PointsArc> arcsByTrigger;

    private void Awake()
    {
        if (carController != null && carController.IsConfigured) carController.Initialize();
        if (clock != null && clock.IsConfigured) clock.Initialize(OnTimeExpired);
        if (menu != null && menu.IsConfigured) menu.Initialize(ServiceLocator.Get<IAudioSettings>());
        if (skinSelector != null && skinSelector.IsConfigured) skinSelector.Initialize();
        ConfigureCollectibles();
        ConfigurePointArcs();
        ConfigureModels();
        if (combat != null && combat.IsConfigured && carController != null && carController.IsConfigured)
            combat.Initialize(carController.Body, OnPlayerDied);
        if ((carController != null && carController.IsConfigured)
            || (clock != null && clock.IsConfigured))
            updateManager = new CustomUpdateManager(TickFrame, TickPhysics);

        
        ServiceLocator.Register<IGameplayControl>(this);
    }

    private void ConfigureCollectibles()
    {
        finishLine?.Initialize(OnRaceFinished);
        if (collectibleArcs == null || collectibleArcs.Length == 0) return;
        arcsByTrigger = new Dictionary<Collider, PointsArc>(collectibleArcs.Length);
        foreach (PointsArc arc in collectibleArcs)
        {
            if (arc == null || arc.Trigger == null) continue;
            arc.Initialize(finishLine);
            arcsByTrigger.Add(arc.Trigger, arc);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || !CompareTag("Player")) return;
        if (arcsByTrigger != null && arcsByTrigger.TryGetValue(other, out PointsArc arc))
            arc.Collect();
        else if (finishLine != null && other == finishLine.Trigger)
            finishLine.Complete(Time.timeSinceLevelLoad);
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
        if (this == null || !isActiveAndEnabled) return;
        carController?.TickPhysics(deltaTime);
        combat?.TickPhysics(deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;
        combat?.HandlePlayerCollision(collision.collider);
    }

    private void OnTimeExpired()
    {
        SetCarMovement(false);
        combat?.Stop();
    }

    private void OnRaceFinished()
    {
        StopClock();
        combat?.Stop();
    }

    private void OnPlayerDied()
    {
        SetCarMovement(false);
        StopClock();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IGameplayControl>(this);
        updateManager?.Dispose();
        StopAllCoroutines();
        model?.Dispose();
        menu?.Dispose();
    }

    public void SetCarMovement(bool enabled) => carController?.SetMovementEnabled(enabled);
    public void StartClock() => clock?.StartCounting();
    public void StopClock() => clock?.Stop();
    public void Break(InputAction.CallbackContext context) => carController?.Break(context);

    public void UpdateCatalog()
    {
        if (model != null) StartCoroutine(model.UpdateCatalog());
    }

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
    public void BackToMenu() => scenes.BackToMenu();
    public void Retry() => scenes.Retry();
}