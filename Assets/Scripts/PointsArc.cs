using UnityEngine;

public class PointsArc : MonoBehaviour
{
    [SerializeField] private FinishLine finishLine;
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            finishLine.IncreasePoints();
            this.gameObject.SetActive(false);
        }
    }
}
