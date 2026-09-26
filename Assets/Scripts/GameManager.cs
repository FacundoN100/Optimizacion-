using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] private List<GameObject> pointsArcs = new List<GameObject>();
    [SerializeField] private int arcsToActivate;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private void Start()
    {
        int countToActivate = Mathf.Min(arcsToActivate, pointsArcs.Count);
        List<GameObject> tempList = new List<GameObject>(pointsArcs);

        for (int i = 0; i < countToActivate; i++)
        {
            int randomIndex = Random.Range(0, tempList.Count);
            GameObject arc = tempList[randomIndex];
            if (arc != null)
                arc.SetActive(false);
            tempList.RemoveAt(randomIndex); 
        }
    }
}
