using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AsfaltoGarageBuilder
{
    private const string GaragePath = "Assets/Scenes/Garage.unity";
    private static readonly Color Navy = new Color(0.035f, 0.055f, 0.105f);
    private static readonly Color Cyan = new Color(0.21f, 0.82f, 0.94f);
    private static readonly Color Orange = new Color(1f, 0.55f, 0.18f);

    [MenuItem("Tools/Asfalto Rebelde/Actualizar luces y GT")]
    public static void UpdateVehicleLights()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene menu = SceneManager.GetActiveScene();
        if (menu.path != "Assets/Scenes/MainMenu.unity")
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            menu = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
        AsfaltoPoliceLighting.Apply();
        // The GT in the garage and on the track uses this same mesh asset.
        AsfaltoPlayerVariants.Apply(GameObject.Find("Police Chase Backdrop/Fugitivo").transform, 2, false);
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Asfalto Rebelde/Actualizar luces del jugador")]
    public static void UpdatePlayerLights()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var openScenes = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string name in new[] { "MainMenu", "Garage", "SampleScene" })
            {
                Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
                FinishPlayerBodies(scene, lightsOnly: true);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("No se pudo guardar " + scene.path);
            }
            AssetDatabase.SaveAssets();
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(openScenes);
        }
    }

    // Actualiza las escenas guardadas sin reconstruir el garaje ni mover los autos.
    [MenuItem("Tools/Asfalto Rebelde/Actualizar presentacion")]
    public static void UpdatePresentation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (string scene in new[] { "MainMenu", "Garage", "SampleScene" })
        {
            Scene current = EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
            if (scene == "MainMenu")
            {
                AsfaltoMenuStyle.Apply();
                AsfaltoNightStreet.Apply();
                AsfaltoPoliceLighting.Apply();
            }
            FinishPlayerBodies(current);
            if (!EditorSceneManager.SaveScene(current))
                throw new InvalidOperationException("No se pudo guardar " + current.path);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }

    private static void FinishPlayerBodies(Scene scene, bool lightsOnly = false)
    {
        var carAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/PlayerCar.fbx");
        Mesh carMesh = carAsset.GetComponentInChildren<MeshFilter>().sharedMesh;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != carMesh) continue;
                AsfaltoPlayerLighting.Apply(filter.transform);
                if (lightsOnly)
                {
                    AsfaltoPlayerVariants.LightRallyLenses(filter.transform);
                    continue;
                }
                AsfaltoBodywork.Apply(filter.transform);
                // The menu displays the GT. Each garage preview is named from the same catalog.
                int variant = 2;
                for (int i = 0; i < CarModelChoice.Count; i++)
                    if (filter.name == CarModelChoice.Name(i)) variant = i;
                AsfaltoPlayerVariants.Apply(filter.transform, variant, scene.name == "SampleScene");
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/Asfalto Rebelde/Construir menu y garaje")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Salí de Play antes de construir escenas."); return; }
        AssetDatabase.Refresh();
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/PlayerCar.fbx");
        var police = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Police/PoliceCar.fbx");
        if (player == null || police == null) { Debug.LogError("Faltan PlayerCar.fbx o PoliceCar.fbx."); return; }

        Material asphalt = Mat("Asphalt", new Color(0.09f, 0.11f, 0.15f));
        Material wall = Mat("Workshop Wall", new Color(0.13f, 0.18f, 0.23f));
        Material trim = Mat("Metal Trim", new Color(0.22f, 0.29f, 0.36f));
        Material orange = Mat("Orange Accent", Orange);
        Material cyan = Mat("Cyan Accent", Cyan);
        Material white = Mat("Road Stripe", new Color(0.78f, 0.79f, 0.70f));

        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EnhanceMainMenu();
        AsfaltoMenuStyle.Apply();
        AsfaltoNightStreet.Apply();
        FinishPlayerBodies(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera garageCamera = CameraSetup(new Vector3(7.2f, 4.6f, 8.7f), new Vector3(0, 1.2f, 0));
        garageCamera.fieldOfView = 55;
        var workshop = new GameObject("Taller 3D").transform;
        Box("Suelo", workshop, new Vector3(0, -0.23f, 0), new Vector3(24, .45f, 22), asphalt);
        Box("Pared trasera", workshop, new Vector3(0, 4.2f, -7), new Vector3(24, 8.5f, .4f), wall);
        Box("Pared izquierda", workshop, new Vector3(-11, 4, 0), new Vector3(.4f, 8, 18), wall);
        Box("Zocalo", workshop, new Vector3(0, .4f, -6.7f), new Vector3(23, .8f, .2f), trim);
        for (int i = -2; i <= 2; i++)
        {
            Box("Panel vertical", workshop, new Vector3(i * 3.4f, 3.4f, -6.65f), new Vector3(.12f, 5, .12f), cyan);
            Box("Linea taller", workshop, new Vector3(i * 4.1f, .018f, 3), new Vector3(.07f, .02f, 7), orange);
        }
        Box("Viga luminosa", workshop, new Vector3(0, 6.6f, -5.7f), new Vector3(19, .14f, .15f), cyan);
        Box("Banco de herramientas", workshop, new Vector3(-7.7f, 1.1f, -3.3f), new Vector3(2.5f, 1.3f, 1), trim);
        Box("Estante", workshop, new Vector3(-7.7f, 2.2f, -3.3f), new Vector3(3, .13f, 1.1f), orange);
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = "Plataforma expositora";
        platform.transform.SetParent(workshop);
        platform.transform.position = new Vector3(2.2f, .14f, 0);
        platform.transform.localScale = new Vector3(3.4f, .14f, 3.4f);
        platform.GetComponent<Renderer>().sharedMaterial = trim;
        UnityEngine.Object.DestroyImmediate(platform.GetComponent<Collider>());
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "Anillo iluminado";
        rim.transform.SetParent(workshop);
        rim.transform.position = new Vector3(2.2f, .04f, 0);
        rim.transform.localScale = new Vector3(3.55f, .055f, 3.55f);
        rim.GetComponent<Renderer>().sharedMaterial = orange;
        UnityEngine.Object.DestroyImmediate(rim.GetComponent<Collider>());
        LightSetup(workshop, new Vector3(-4, 7, 2), new Color(.6f, .85f, 1), 2.6f, LightType.Spot);
        LightSetup(workshop, new Vector3(4, 5, -1), Orange, 2.3f, LightType.Spot);
        LightSetup(workshop, new Vector3(0, 8, -3), Color.white, .8f, LightType.Directional);

        var cars = new GameObject[3];
        string[] materials = { "PlayerCar_Blue", "Atlas", "PlayerCar_Red" };
        for (int i = 0; i < cars.Length; i++)
        {
            cars[i] = PlaceModel(player, workshop, CarModelChoice.Name(i), new Vector3(2.2f, .35f, 0), 4.6f);
            foreach (Renderer renderer in cars[i].GetComponentsInChildren<Renderer>())
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Materials/" + materials[i] + ".mat");
            cars[i].SetActive(i == 2);
        }
        Canvas canvas = CanvasSetup();
        var header = Panel(canvas.transform, "Cabecera", new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -78), new Vector2(0, 156), new Color(.025f, .035f, .07f, .84f));
        Text(header, "ASFALTO REBELDE", new Vector2(45, -10), new Vector2(760, 70), 48, Color.white, TextAlignmentOptions.Left);
        Text(header, "GARAJE  /  ELEGÍ TU AUTO", new Vector2(48, -68), new Vector2(660, 35), 19, Cyan, TextAlignmentOptions.Left);
        var card = Panel(canvas.transform, "Ficha del vehiculo", new Vector2(1, .5f), new Vector2(1, .5f),
            new Vector2(-35, -26), new Vector2(440, 285), new Color(.025f, .04f, .075f, .86f));
        TMP_Text title = Text(card, "REBELDE GT", new Vector2(20, -12), new Vector2(390, 65), 35, Color.white, TextAlignmentOptions.Left);
        TMP_Text stats = Text(card, "", new Vector2(20, -98), new Vector2(390, 120), 20, new Color(.86f, .91f, .95f), TextAlignmentOptions.TopLeft);
        TMP_Text number = Text(card, "03 / 03", new Vector2(20, -235), new Vector2(390, 32), 19, Orange, TextAlignmentOptions.Left);
        var footer = Panel(canvas.transform, "Acciones", new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 76), new Vector2(0, 152), new Color(.025f, .035f, .07f, .88f));
        GameManager controller = new GameObject("Garage").AddComponent<GameManager>();
        var serialized = new SerializedObject(controller);
        SerializedProperty models = serialized.FindProperty("garage.models"); models.arraySize = 3;
        models.GetArrayElementAtIndex(0).objectReferenceValue = cars[0];
        models.GetArrayElementAtIndex(1).objectReferenceValue = cars[1];
        models.GetArrayElementAtIndex(2).objectReferenceValue = cars[2];
        serialized.FindProperty("garage.modelName").objectReferenceValue = title;
        serialized.FindProperty("garage.stats").objectReferenceValue = stats;
        serialized.FindProperty("garage.number").objectReferenceValue = number;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Button(footer, "Volver", new Vector2(25, -54), new Vector2(185, 58), trim, controller.GarageBack);
        Button(footer, "<", new Vector2(360, -54), new Vector2(80, 58), trim, controller.GaragePrevious);
        Button(footer, ">", new Vector2(458, -54), new Vector2(80, 58), trim, controller.GarageNext);
        Button(footer, "START", new Vector2(-165, -54), new Vector2(270, 65), orange, controller.GarageStart, true);
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        FinishPlayerBodies(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), GaragePath);
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true),
            new EditorBuildSettingsScene(GaragePath, true)
        };
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        FinishPlayerBodies(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("Asfalto Rebelde: MainMenu, Garage y SampleScene conectados.");
    }

    private static void EnhanceMainMenu()
    {
        // Se conserva la camara, la UI y la ubicacion de los autos editadas en la escena.
        Transform backdrop = Find("Police Chase Backdrop");
        if (backdrop == null) throw new InvalidOperationException("Falta la composicion de MainMenu.");
        Transform background = Find("Background");
        if (background != null && background.GetComponent<Image>() != null) background.gameObject.SetActive(false);
        Transform previous = backdrop.Find("Ambiente urbano");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
        Transform environment = new GameObject("Ambiente urbano").transform;
        environment.SetParent(backdrop, false);

        Material asphalt = TexturedMat("Asphalt", "Assets/PBS Materials Variety Pack/Asphalt/Asphalt 1.mat", new Vector2(12, 18));
        Material brick = TexturedMat("Fachada ladrillo", "Assets/PBS Materials Variety Pack/Brick/Brick 1.mat", new Vector2(3, 4));
        Material concrete = TexturedMat("Hormigon", "Assets/PBS Materials Variety Pack/Concrete/Concrete 1.mat", new Vector2(4, 4));
        Material warm = Mat("Ventana encendida", new Color(.7f, .47f, .2f));
        warm.EnableKeyword("_EMISSION"); warm.SetColor("_EmissionColor", new Color(.55f, .30f, .08f));
        Material glass = Mat("Ventana oscura", new Color(.045f, .09f, .15f));
        foreach (Transform child in backdrop)
        {
            if (child.name == "Asfalto") child.GetComponent<Renderer>().sharedMaterial = asphalt;
            if (child.name == "Barrera") child.GetComponent<Renderer>().sharedMaterial = concrete;
            if (child.name != "Silueta urbana") continue;
            child.GetComponent<Renderer>().sharedMaterial = brick;
            Bounds bounds = child.GetComponent<Renderer>().bounds;
            float x = bounds.center.x > 0 ? bounds.min.x - .02f : bounds.max.x + .02f;
            for (int row = 0; row < 3; row++)
                for (int column = 0; column < 3; column++)
                    Box("Ventana", environment, new Vector3(x, bounds.min.y + 1.1f + row * 1.5f,
                        bounds.center.z + (column - 1) * 1.6f), new Vector3(.035f, .8f, .7f),
                        (row + column) % 3 == 0 ? warm : glass);
        }
        // Faroles existentes del proyecto: misma malla y materiales compartidos.
        var streetLamp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/StreetLights/IntersectionStreetLight.prefab");
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
            {
                var lamp = (GameObject)PrefabUtility.InstantiatePrefab(streetLamp);
                lamp.transform.SetParent(environment, false);
                lamp.name = "Farol de calle";
                lamp.transform.position = new Vector3(side * 7.4f, 0, -3 + i * 10);
                lamp.transform.rotation = Quaternion.Euler(0, side < 0 ? 90 : -90, 0);
                foreach (Collider collider in lamp.GetComponentsInChildren<Collider>()) collider.enabled = false;
                LightSetup(environment, new Vector3(side * 6, 5.8f, -3 + i * 10), new Color(1f, .76f, .45f), 4.2f, LightType.Spot);
                Transform lightTransform = environment.GetChild(environment.childCount - 1);
                lightTransform.LookAt(lightTransform.position + Vector3.down * 6);
                lightTransform.GetComponent<Light>().range = 12;
            }

        Transform player = backdrop.Find("Fugitivo");
        if (player != null)
        {
            player.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Materials/PlayerCar_Red.mat");
            Bounds b = player.GetComponent<MeshFilter>().sharedMesh.bounds;
            // Se reutilizan dos emisores, sin Instantiate/Destroy durante la presentacion.
            var smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Effects/PlayerDriftSmoke.prefab");
            for (int side = -1; side <= 1; side += 2)
            {
                var smoke = (GameObject)PrefabUtility.InstantiatePrefab(smokePrefab);
                smoke.name = "Estela sutil"; smoke.transform.SetParent(environment, false);
                smoke.transform.position = player.TransformPoint(new Vector3(b.center.x + side * b.size.x * .3f, b.min.y + b.size.y * .12f, b.min.z));
                smoke.transform.rotation = player.rotation;
                var particles = smoke.GetComponent<ParticleSystem>();
                var main = particles.main; main.playOnAwake = true; main.loop = true; main.prewarm = true;
                main.startLifetime = 1.6f; main.startSpeed = .15f; main.startSize = new ParticleSystem.MinMaxCurve(.18f, .42f);
                main.startColor = new Color(.7f, .75f, .8f, .23f); main.maxParticles = 24;
                main.simulationSpace = ParticleSystemSimulationSpace.World; main.scalingMode = ParticleSystemScalingMode.Shape;
                var emission = particles.emission; emission.rateOverTime = 9;
                var shape = particles.shape; shape.enabled = true; shape.radius = .1f;
                var velocity = particles.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = 0; velocity.y = .16f; velocity.z = -.85f;
            }
        }

        Transform[] patrols = backdrop.Cast<Transform>().Where(t => t.name.StartsWith("Patrulla")).ToArray();
        AsfaltoPoliceLighting.Apply();
        foreach (Light light in backdrop.GetComponentsInChildren<Light>())
        {
            if (light.transform.parent != backdrop) continue;
            if (light.type == LightType.Directional) { light.intensity = .9f; light.shadows = LightShadows.Soft; }
            else light.enabled = false;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.25f, .30f, .40f);
        GameManager manager = UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).First(m => m.name == "MenuManager");
        var settings = new SerializedObject(manager);
        SerializedProperty refs = settings.FindProperty("menuAtmosphere.policeCars"); refs.arraySize = patrols.Length;
        for (int i = 0; i < patrols.Length; i++) refs.GetArrayElementAtIndex(i).objectReferenceValue = patrols[i];
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static Material TexturedMat(string name, string sourcePath, Vector2 tiling)
    {
        Material mat = Mat(name, Color.white);
        Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
        mat.SetTexture("_BaseMap", source.GetTexture("_MainTex")); mat.SetTextureScale("_BaseMap", tiling);
        mat.SetFloat("_Smoothness", .22f); mat.SetFloat("_Metallic", 0);
        if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null)
        {
            mat.SetTexture("_BumpMap", source.GetTexture("_BumpMap")); mat.SetTextureScale("_BumpMap", tiling);
            mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale", .4f);
        }
        return mat;
    }

    private static Transform Find(string name) => UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
        .FirstOrDefault(t => t.name == name);

    private static Material Mat(string name, Color color)
    {
        const string folder = "Assets/Settings/AsfaltoGarage";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Settings", "AsfaltoGarage");
        string path = folder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic);
        return obj;
    }

    private static GameObject PlaceModel(GameObject prefab, Transform parent, string name, Vector3 position, float width)
    {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(0, 25, 0);
        var renderers = obj.GetComponentsInChildren<Renderer>(true);
        if (prefab.name == "PoliceCar")
        {
            Material policeAtlas = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Police/PoliceAtlas.mat");
            if (policeAtlas != null)
                foreach (Renderer renderer in renderers) renderer.sharedMaterial = policeAtlas;
        }
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        obj.transform.localScale *= width / Mathf.Max(b.size.x, b.size.z);
        b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        obj.transform.position += new Vector3(position.x - b.center.x, position.y - b.min.y, position.z - b.center.z);
        return obj;
    }

    private static Camera CameraSetup(Vector3 position, Vector3 target)
    {
        var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        go.tag = "MainCamera";
        go.transform.position = position;
        go.transform.LookAt(target);
        var camera = go.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Navy;
        return camera;
    }

    private static void LightSetup(Transform parent, Vector3 position, Color color, float intensity, LightType type)
    {
        var go = new GameObject(type + " Light", typeof(Light));
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.LookAt(Vector3.zero);
        Light light = go.GetComponent<Light>();
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        light.range = 14;
        light.spotAngle = 70;
        light.shadows = LightShadows.None;
    }

    private static Canvas CanvasSetup()
    {
        var go = new GameObject("Garage Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1334, 720);
        return canvas;
    }

    private static RectTransform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(anchorMax.x, anchorMax.y);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private static TMP_Text Text(Transform parent, string content, Vector2 position, Vector2 size, float font,
        Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(content.Length > 20 ? "Detalles" : content, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = content; text.fontSize = font; text.color = color;
        text.alignment = alignment; text.raycastTarget = false;
        return text;
    }

    private static void Button(Transform parent, string label, Vector2 position, Vector2 size, Material material,
        UnityEngine.Events.UnityAction onClick, bool right = false)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(right ? 1 : 0, 1);
        rect.pivot = new Vector2(right ? 1 : 0, 1);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        go.GetComponent<Image>().color = material.color;
        UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, onClick);
        TMP_Text text = Text(rect, label, Vector2.zero, size, 25, Color.white, TextAlignmentOptions.Center);
        text.rectTransform.pivot = new Vector2(0, 1);
    }
}
