using TMPro;
using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [SerializeField] private Canvas VictoryImage;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI finalPointsText;
    [SerializeField] private Clock clock;
    private float timeSpend;
    private int score = 0;
    private int points = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            timeSpend = Time.timeSinceLevelLoad;
            timeText.SetText($"{timeSpend:F2}s");
            finalPointsText.SetText("{0}", points);
            score = (Mathf.Max(0, (int)(100 - timeSpend)) + 1) * ((int)(points * 10) + 1);
            scoreText.SetText("{0}", score);
            clock.SelfDestroy();
            VictoryImage.enabled = true;
        }
    }

    public void IncreasePoints()
    {
        points += 100;
        pointsText.text = "Points: " + points;
    }
}