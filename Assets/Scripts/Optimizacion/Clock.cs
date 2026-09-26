using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Clock : MonoBehaviour
{
    public static Clock Instance { get; private set; }
    [Header("Time")]
    [SerializeField] private float maxTime = 100f;
    private float currentTime;

    [Header("UI")]
    [Tooltip("Image con Type = Filled y Fill Method = Radial")]
    [SerializeField] private Image timeFillImage;

    [Header("Death")]
    [SerializeField] private Canvas deathImage;

    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private CarController carController;
    private float timer;
    private int frameCount;

    private bool canStart = false;
    private bool IsDead = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (currentTime <= 0f) currentTime = maxTime;
        UpdateUIInstant();
        if (deathImage != null) deathImage.enabled = false;
        IsDead = false;
        canStart = false;
    }

    private void Update()
    {
        if (canStart)
        {
            CountTime();
        }
        frameCount++;
        timer += Time.unscaledDeltaTime;
        if (timer >= 0.5f)
        {

            float fps = frameCount / timer;

            fpsText.SetText("{0:0} FPS", fps);

            frameCount = 0;

            timer = 0f;

        }
    }

    public void CountTime()
    {
        if (IsDead) return;
        currentTime -= Time.deltaTime;
        currentTime = Mathf.Clamp(currentTime, 0f, maxTime);
        UpdateUIInstant();

        if (currentTime <= 0f && !IsDead)
        {
            OnDeath();
        }
    }

    private void UpdateUIInstant()
    {
        if (timeFillImage != null)
            timeFillImage.fillAmount = currentTime / maxTime;
    }

    private void OnDeath()
    {
        IsDead = true;
        if (deathImage != null) deathImage.enabled = true;
        carController.ImDead(false);
    }

    public void SelfDestroy()
    {
        this.gameObject.SetActive(false);
    }

    public void NowYouCanCount()
    {
        canStart = true;
    }
}