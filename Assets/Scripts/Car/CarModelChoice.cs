using UnityEngine;

// Player-only catalog. PoliceCar belongs exclusively to the pursuit system.
// The order matches the original SkinSelectorUI saved indices.
public static class CarModelChoice
{
    public const string SkinPreference = "SelectedSkinIndex";
    public const string MaterialPreference = "SelectedCarMaterial";
    public const int Count = 3;
    public const string AccessoriesRoot = "Carrocerias del jugador";

    private static readonly string[] VariantNames = { "Street", "Rally", "GT" };

    private static readonly string[] Names =
    {
        "REBELDE AZUL", "REBELDE NARANJA", "REBELDE GT"
    };
    private static readonly string[] Materials =
    {
        "Color_Blue", "Color_Oranje", "Color_Red"
    };

    public static string Name(int index) => Names[Mathf.Clamp(index, 0, Count - 1)];
    public static string MaterialAddress(int index) => Materials[Mathf.Clamp(index, 0, Count - 1)];
    public static string VariantName(int index) => VariantNames[Mathf.Clamp(index, 0, Count - 1)];

    // The three kits are serialized in SampleScene. Select once, with no runtime spawning.
    public static void ApplyAccessories(Transform body)
    {
        if (body == null || !body.CompareTag("Player")) return;
        Transform root = body.Find(AccessoriesRoot);
        if (root == null) return;
        string selectedName = VariantName(ReadSelection());
        for (int i = 0; i < root.childCount; i++)
        {
            GameObject variant = root.GetChild(i).gameObject;
            bool selected = variant.name == selectedName;
            if (variant.activeSelf != selected) variant.SetActive(selected);
        }
    }

    public static int ReadSelection()
    {
        // Material is authoritative because it is also consumed by ApplyModel.
        string material = CarMaterialAddress.Resolve(PlayerPrefs.GetString(MaterialPreference, ""));
        for (int i = 0; i < Count; i++)
            if (Materials[i] == material) return i;
        return Mathf.Clamp(PlayerPrefs.GetInt(SkinPreference, 2), 0, Count - 1);
    }

    public static void SaveSelection(int index)
    {
        index = Mathf.Clamp(index, 0, Count - 1);
        PlayerPrefs.SetInt(SkinPreference, index);
        PlayerPrefs.SetString(MaterialPreference, Materials[index]);
        PlayerPrefs.Save();
    }
}
