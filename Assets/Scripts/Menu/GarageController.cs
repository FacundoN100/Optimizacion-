using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class GarageController
{
    [SerializeField] private GameObject[] models;
    [SerializeField] private TMP_Text modelName;
    [SerializeField] private TMP_Text stats;
    [SerializeField] private TMP_Text number;

    // All three bodies reuse the existing player handling and health values.
    private const string Details = "VELOCIDAD MAX.  200     SALUD  100\nACELERACION  300\nCONDUCCION ORIGINAL";
    private int selected;
    private int ModelCount => Mathf.Min(models?.Length ?? 0, CarModelChoice.Count);
    public bool IsConfigured => ModelCount > 0 && modelName != null && stats != null && number != null;

    public void Initialize()
    {
        if (!IsConfigured) return;
        selected = Mathf.Clamp(CarModelChoice.ReadSelection(), 0, ModelCount - 1);
        Refresh();
    }

    public void Previous() => Select(-1);
    public void Next() => Select(1);

    private void Select(int direction)
    {
        if (!IsConfigured || ModelCount < 2) return;
        selected = (selected + direction + ModelCount) % ModelCount;
        Refresh();
    }

    private void Refresh()
    {
        // Pre-created previews are reused. No Instantiate/Destroy or per-frame Canvas writes.
        for (int i = 0; i < models.Length; i++)
            if (models[i] != null && models[i].activeSelf != (i == selected))
                models[i].SetActive(i == selected);
        modelName.text = CarModelChoice.Name(selected);
        stats.text = Details;
        number.text = (selected + 1).ToString("00") + " / " + ModelCount.ToString("00");
    }

    public void Back()
    {
        if (IsConfigured) CarModelChoice.SaveSelection(selected);
        SceneManager.LoadScene("MainMenu");
    }

    public void StartGame()
    {
        if (!IsConfigured) return;
        CarModelChoice.SaveSelection(selected);
        SceneManager.LoadScene("SampleScene");
    }
}
