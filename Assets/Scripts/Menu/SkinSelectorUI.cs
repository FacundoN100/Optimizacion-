using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinSelectorUI : MonoBehaviour
{
    [SerializeField] private Sprite[] skins;
    [SerializeField] private string[] skinNames;
    [SerializeField] private string[] materialAddress = { "Color_Red", "Color_Blue", "Color_Orange" };

    [SerializeField] private Image skinPreview;
    [SerializeField] private TMP_Text skinNameText;

    private int currentSkin;
    private const string SkinPreferenceKey = "SelectedCarMaterial";

    private void Start()
    {
        currentSkin = PlayerPrefs.GetInt("SelectedSkinIndex", 0);
        UpdateUI();
    }

    public void NextSkin()
    {
        currentSkin++;

        if (currentSkin >= skins.Length)
            currentSkin = 0;

        UpdateUI();
        SaveSelection();
    }

    public void PreviousSkin()
    {
        currentSkin--;

        if (currentSkin < 0)
            currentSkin = skins.Length - 1;

        UpdateUI();
        SaveSelection();
    }

    private void UpdateUI()
    {
        skinPreview.sprite = skins[currentSkin];
        skinNameText.text = skinNames[currentSkin];
    }

    private void SaveSelection()
    {
        PlayerPrefs.SetInt("SelectedSkinIndex", currentSkin);
        PlayerPrefs.SetString(SkinPreferenceKey, materialAddress[currentSkin]);
        PlayerPrefs.Save();
    }
}
