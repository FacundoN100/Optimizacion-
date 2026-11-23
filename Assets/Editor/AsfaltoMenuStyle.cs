using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Applies the menu's visual layout in the editor. All button listeners stay on
// the original objects and the resulting Canvas works without a runtime script.
public static class AsfaltoMenuStyle
{
    private static readonly Color32 Amber = new Color32(255, 188, 65, 255);
    private static readonly Color32 Ink = new Color32(14, 26, 41, 255);
    private static readonly Color32 Paper = new Color32(245, 248, 251, 255);
    private const string ButtonSpritePath = "Assets/Settings/AsfaltoGarage/MenuButtonCut.png";

    public static void Apply()
    {
        var panel = GameObject.Find("Canvas/MainMenuPanel").GetComponent<RectTransform>();
        var scaler = panel.GetComponentInParent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // The project's other menu panels are authored at 1612 x 720.
        scaler.referenceResolution = new Vector2(1612, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        Stretch(panel);

        var title = panel.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
        var titleRect = title.rectTransform;
        Stretch(titleRect);
        titleRect.anchorMin = new Vector2(.035f, .782f);
        titleRect.anchorMax = new Vector2(.965f, .96f);
        title.richText = true;
        title.text = "ASFALTO <color=#FFBC41>REBELDE</color>";
        title.color = Paper;
        title.enableVertexGradient = false;
        title.fontStyle = FontStyles.Bold | FontStyles.Italic;
        title.characterSpacing = -2f;
        title.alignment = TextAlignmentOptions.Center;
        title.enableAutoSizing = true;
        title.fontSizeMin = 16;
        title.fontSizeMax = 112;
        title.enableWordWrapping = false;
        title.margin = Vector4.zero;
        title.raycastTarget = false;
        title.overrideColorTags = false;
        title.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Drop Shadow.mat");

        Sprite shape = ButtonSprite();
        string[] actions = { "Play", "Options", "Credits", "Exit" };
        for (int index = 0; index < actions.Length; index++)
        {
            Button button = panel.Find(actions[index]).GetComponent<Button>();
            PositionMainAction((RectTransform)button.transform, (actions.Length - 1 - index) * 60f);
            StyleButton(button, shape, index == 0, false);
        }
        foreach (Transform section in panel.parent)
        {
            if (section == panel || section.name == "SkinsPanel") continue;
            foreach (Button button in section.GetComponentsInChildren<Button>(true))
                StyleButton(button, shape, false, true);
        }
    }

    private static void PositionMainAction(RectTransform rect, float y)
    {
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.pivot = Vector2.zero;
        rect.anchorMin = rect.anchorMax = new Vector2(.055f, .07f);
        rect.sizeDelta = new Vector2(278, 51);
        rect.anchoredPosition = new Vector2(0, y);
    }

    private static void StyleButton(Button button, Sprite shape, bool primary, bool compact)
    {
        RectTransform rect = (RectTransform)button.transform;
        Image surface = button.image;
        surface.sprite = shape;
        surface.type = Image.Type.Sliced;
        surface.color = Color.white;
        surface.preserveAspect = false;
        button.targetGraphic = surface;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock states = button.colors;
        states.normalColor = primary ? Amber : new Color32(23, 39, 58, 247);
        states.highlightedColor = primary ? new Color32(255, 217, 127, 255) : new Color32(46, 82, 111, 255);
        states.selectedColor = states.highlightedColor;
        states.pressedColor = primary ? new Color32(231, 152, 42, 255) : new Color32(30, 64, 88, 255);
        states.disabledColor = new Color32(102, 105, 107, 255);
        states.colorMultiplier = 1f;
        states.fadeDuration = .09f;
        button.colors = states;

        var label = rect.GetComponentInChildren<TextMeshProUGUI>(true);
        Stretch(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(compact ? 13 : 24, 0);
        label.rectTransform.offsetMax = new Vector2(compact ? -8 : -38, 0);
        label.color = primary ? Ink : Paper;
        label.enableVertexGradient = false;
        label.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        float fontSize = compact ? 20 : 24;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMax = fontSize;
        label.fontSizeMin = compact ? 10 : 14;
        label.characterSpacing = 2.5f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;

        if (primary)
        {
            Detail(rect, "Flecha superior", Ink, new Vector2(1, .5f), new Vector2(-25, 3.5f), new Vector2(13, 3), -38);
            Detail(rect, "Flecha inferior", Ink, new Vector2(1, .5f), new Vector2(-25, -3.5f), new Vector2(13, 3), 38);
        }
        else
        {
            Detail(rect, "Marca lateral", Amber, new Vector2(0, .5f), new Vector2(4, 0), new Vector2(3, compact ? 11 : 19), 0);
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(.5f, .5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Detail(RectTransform parent, string name, Color color,
        Vector2 anchor, Vector2 position, Vector2 size, float rotation)
    {
        RectTransform rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.layer = parent.gameObject.layer;
            rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
        }
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.Euler(0, 0, rotation);
        Image image = rect.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    // Nine-slicing keeps the diagonal corners the same size on every button.
    private static Sprite ButtonSprite()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
        if (sprite != null) return sprite;

        const int size = 32;
        const int cut = 10;
        var mask = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float lowerLeft = x + y + 1 - cut;
                float upperRight = (size - x - 1) + (size - y - 1) + 1 - cut;
                float coverage = Mathf.Clamp01(Mathf.Min(lowerLeft, upperRight) + .5f);
                mask.SetPixel(x, y, new Color(1, 1, 1, coverage));
            }
        }
        mask.Apply();
        File.WriteAllBytes(ButtonSpritePath, mask.EncodeToPNG());
        Object.DestroyImmediate(mask);
        AssetDatabase.ImportAsset(ButtonSpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(ButtonSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = new Vector4(cut, cut, cut, cut);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
    }
}
