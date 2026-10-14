using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public sealed class SkinSelectorUI
{
    [SerializeField] private Sprite[] skins;
    [SerializeField] private string[] skinNames;
    [SerializeField] private string[] materialAddress = { "Color_Red", "Color_Blue", "Color_Orange" };

    [SerializeField] private Image skinPreview;
    [SerializeField] private TMP_Text skinNameText;

    private int currentSkin;
    private const string SkinPreferenceKey = "SelectedCarMaterial";

    public bool IsConfigured => skinPreview != null && skinNameText != null && SkinCount > 0;

    private int SkinCount => Mathf.Min(skins?.Length ?? 0,
        Mathf.Min(skinNames?.Length ?? 0, materialAddress?.Length ?? 0));

    public void Initialize()
    {
        currentSkin = Mathf.Clamp(PlayerPrefs.GetInt("SelectedSkinIndex", 0), 0, SkinCount - 1);
        UpdateUI();
    }

    public void NextSkin()
    {
        if (!IsConfigured) return;
        currentSkin++;

        if (currentSkin >= SkinCount)
            currentSkin = 0;

        UpdateUI();
        SaveSelection();
    }

    public void PreviousSkin()
    {
        if (!IsConfigured) return;
        currentSkin--;

        if (currentSkin < 0)
            currentSkin = SkinCount - 1;

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
