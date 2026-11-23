using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Scene dressing is authored once in the editor. The skyline needs a single renderer in Play.
public static class AsfaltoNightStreet
{
    const string Folder = "Assets/Settings/AsfaltoGarage/";

    public static void Apply()
    {
        var street = GameObject.Find("Police Chase Backdrop");
        if (street == null || street.scene.name != "MainMenu")
            throw new InvalidOperationException("Abrí MainMenu para ajustar la calle.");

        Transform road = street.transform.Find("Asfalto");
        var roadSurface = NewNightRoadMaterial();
        road.GetComponent<MeshRenderer>().sharedMaterial = roadSurface;
        road.localScale = new Vector3(110, .3f, 150);
        road.localPosition = new Vector3(0, -.18f, 52);

        Camera camera = Camera.main;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .060f, .126f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.25f, .31f, .42f);
        RenderSettings.ambientEquatorColor = new Color(.11f, .15f, .23f);
        RenderSettings.ambientGroundColor = new Color(.07f, .075f, .112f);
        RenderSettings.reflectionIntensity = .27f;

        Transform environment = street.transform.Find("Ambiente urbano");
        foreach (Transform prop in environment)
        {
            if (prop.name != "Farol de calle") continue;
            // The lamp prefab already includes a working spotlight under its luminous head.
            // Bring the crossarm into the 22-degree camera framing and point it over the lane.
            prop.localScale = Vector3.one * .64f;
            prop.rotation = Quaternion.Euler(0, prop.position.x < 0 ? 180 : 0, 0);
            var lamp = prop.GetComponentInChildren<Light>(true);
            lamp.enabled = true;
            lamp.cullingMask |= 1;
            lamp.color = new Color(1f, .73f, .46f);
            lamp.intensity = 11f;
            lamp.range = 12f;
            lamp.shadows = LightShadows.None;
            PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
            PrefabUtility.RecordPrefabInstancePropertyModifications(lamp);
        }
        foreach (var light in street.GetComponentsInChildren<Light>(true))
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(.66f, .77f, 1f);
                light.intensity = .28f;
                light.shadowStrength = .65f;
                light.shadowNormalBias = .08f;
                light.shadowBias = .025f;
            }
            else if (light.type == LightType.Spot && light.transform.parent.name == "Ambiente urbano")
            {
                light.enabled = false;
            }
        }

        LightPlayer(street.transform, camera);
        var geometry = new SkylineMesh();
        // The ground intersects the low layer. The top fades to the camera clear color.
        geometry.Gradient(-480, 480, -5, 1, 160, 0x36425d, 0x807081);
        geometry.Gradient(-480, 480, 1, 9, 160, 0x807081, 0x4c567a);
        geometry.Gradient(-480, 480, 9, 24, 160, 0x4c567a, 0x223759);
        geometry.Gradient(-480, 480, 24, 65, 160, 0x223759, 0x091020);
        var random = new System.Random(1123);
        AddBuildings(geometry, random, 88, true);
        AddBuildings(geometry, random, 63, false);
        foreach (Transform child in street.transform)
        {
            if (child.name != "Silueta urbana") continue;
            var b = child.GetComponent<Renderer>().bounds;
            // Street-facing end walls were bare brick. Recessed pairs share the skyline mesh.
            for (float y = b.min.y + 1.1f; y < b.max.y - .6f; y += 1.6f)
                for (float x = b.min.x + .75f; x < b.max.x - .55f; x += 1.55f)
                {
                    float z = b.min.z - .025f;
                    geometry.Rect(x, y, .70f, .80f, z, 0x162334);
                    int color = random.Next(5) == 0 ? 0x997552 : 0x293a4b;
                    geometry.Rect(x + .06f, y + .08f, .25f, .63f, z - .008f, color);
                    geometry.Rect(x + .39f, y + .08f, .25f, .63f, z - .008f, color);
                }
        }

        var skyline = street.transform.Find("Horizonte nocturno");
        if (skyline == null)
        {
            skyline = new GameObject("Horizonte nocturno", typeof(MeshFilter), typeof(MeshRenderer)).transform;
            skyline.SetParent(street.transform, false);
        }
        skyline.GetComponent<MeshFilter>().sharedMesh = geometry.Save();
        var renderer = skyline.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = SkylineMaterial();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    static void LightPlayer(Transform street, Camera camera)
    {
        Transform car = street.Find("Fugitivo");
        var body = car.GetComponent<MeshRenderer>();
        var original = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Materials/PlayerCar_Red.mat");
        var paint = MenuMaterial("Pintura roja nocturna", "Universal Render Pipeline/Lit");
        Texture atlas = original.GetTexture("_BaseMap");
        paint.SetTexture("_BaseMap", atlas);
        paint.SetTexture("_EmissionMap", atlas);
        paint.SetColor("_BaseColor", Color.white);
        paint.SetColor("_EmissionColor", new Color(.69f, .53f, .51f));
        paint.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        paint.EnableKeyword("_EMISSION");
        paint.SetFloat("_Smoothness", .62f);
        paint.SetFloat("_Metallic", .13f);
        EditorUtility.SetDirty(paint);
        body.sharedMaterial = paint;
        if (PrefabUtility.IsPartOfPrefabInstance(body))
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);

        const string keyName = "Reflejo sobre el fugitivo";
        Transform mount = street.Find(keyName);
        if (!mount)
        {
            mount = new GameObject(keyName, typeof(Light)).transform;
            mount.SetParent(street, false);
        }
        Vector3 center = body.bounds.center;
        mount.position = center + Vector3.up * 2.8f + camera.transform.right * 1.5f - camera.transform.forward * 1.5f;
        mount.LookAt(center);
        var key = mount.GetComponent<Light>();
        key.type = LightType.Spot;
        key.spotAngle = 54f;
        key.innerSpotAngle = 32f;
        key.color = new Color(.88f, .91f, 1f);
        key.intensity = 15f;
        key.range = 6f;
        key.shadows = LightShadows.None;
    }

    static Material MenuMaterial(string name, string shader)
    {
        string path = Folder + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader desiredShader = Shader.Find(shader);
        if (material)
        {
            if (material.shader != desiredShader) material.shader = desiredShader;
            return material;
        }
        material = new Material(desiredShader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static Material NewNightRoadMaterial()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Asphalt.mat");
        var mat = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Asfalto nocturno.mat");
        if (mat == null)
        {
            mat = new Material(source);
            AssetDatabase.CreateAsset(mat, Folder + "Asfalto nocturno.mat");
        }
        mat.SetColor("_BaseColor", new Color(.57f, .61f, .70f));
        mat.SetTextureScale("_BaseMap", new Vector2(32, 54));
        mat.SetFloat("_Smoothness", .40f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material SkylineMaterial()
    {
        const string path = Folder + "Luces de la ciudad.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        // The project's particle shader accepts vertex colours on an ordinary opaque mesh.
        mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetInt("_Cull", (int)CullMode.Off);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void AddBuildings(SkylineMesh mesh, System.Random random, float depth, bool distant)
    {
        int facade = distant ? 0x3d485e : 0x27364b;
        int side = distant ? 0x364256 : 0x202d40;
        for (float x = -95; x < 100;)
        {
            float width = random.Next(3, 7);
            float height = distant ? random.Next(7, 14) : random.Next(3, 8);
            float z = depth + random.Next(5);
            mesh.Rect(x, -1, width, height + 1, z, facade);
            mesh.Rect(x + width * .77f, -1, width * .23f, height + 1, z - .012f, side);
            if (random.Next(3) == 0)
                mesh.Rect(x + .55f, height, width * .36f, .5f, z, side);

            for (float y = .65f; y < height - .55f; y += 1.15f)
                for (float wx = x + .44f; wx < x + width - .46f; wx += .86f)
                {
                    int chance = random.Next(10);
                    if (chance > 4) continue;
                    int warm = distant ? 0x9b8390 : 0xb5906e;
                    int cool = distant ? 0x657897 : 0x647b91;
                    mesh.Rect(wx, y, .37f, .34f, z - .05f, chance == 0 ? warm : cool);
                }
            x += width + (distant ? .25f : .65f);
        }
    }

    sealed class SkylineMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Color> colours = new List<Color>();
        readonly List<int> indices = new List<int>();

        public void Rect(float x, float y, float width, float height, float depth, int rgb)
        {
            Gradient(x, x + width, y, y + height, depth, rgb, rgb);
        }

        public void Gradient(float left, float right, float bottom, float top, float z, int bottomRGB, int topRGB)
        {
            int i = vertices.Count;
            vertices.Add(new Vector3(left, bottom, z));
            vertices.Add(new Vector3(left, top, z));
            vertices.Add(new Vector3(right, top, z));
            vertices.Add(new Vector3(right, bottom, z));
            Color low = Colour(bottomRGB);
            Color high = Colour(topRGB);
            colours.Add(low);
            colours.Add(high);
            colours.Add(high);
            colours.Add(low);
            indices.Add(i); indices.Add(i + 1); indices.Add(i + 2);
            indices.Add(i); indices.Add(i + 2); indices.Add(i + 3);
        }

        static Color Colour(int rgb)
        {
            Color c = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
        }

        public Mesh Save()
        {
            const string path = Folder + "Horizonte nocturno.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = "Horizonte nocturno" };
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colours);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }
    }
}
