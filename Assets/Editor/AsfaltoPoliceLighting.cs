using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class AsfaltoPoliceLighting
{
    private const string Folder = "Assets/Models/Police/";
    private static readonly Color Red = Color.red;
    private static readonly Color Blue = Color.blue;
    private static readonly Color Headlight = Color.green;

    public static void Apply()
    {
        var backdrop = GameObject.Find("Police Chase Backdrop");
        if (!backdrop || backdrop.scene.name != "MainMenu")
            throw new InvalidOperationException("La iluminación de patrullas requiere MainMenu.");

        var original = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "PoliceCar.fbx");
        var source = original.GetComponentInChildren<MeshFilter>().sharedMesh;
        Mesh glass = SharedMesh("PoliceGlass", () => MakeGlass(source));
        Mesh spill = SharedMesh("PoliceRoadSpill", MakeSpill);
        const string materialPath = Folder + "PoliceOptics.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            Shader shader = Shader.Find("Asfalto Rebelde/Police glass and spill");
            if (!shader) throw new InvalidOperationException("Falta el shader de ópticas policiales.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        const string prefabPath = Folder + "PoliceCar.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            BoxCollider collider = prefab.GetComponent<BoxCollider>();
            float sole = collider.center.y - .5f * collider.size.y;
            Install(prefab.transform, sole + .025f / prefab.transform.lossyScale.y, glass, spill, material);
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath, out bool success);
            if (!success) throw new InvalidOperationException("No se pudo guardar PoliceCar.prefab.");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        var road = backdrop.transform.Find("Asfalto").GetComponent<Renderer>();
        float roadHeight = road.bounds.max.y + .025f;
        foreach (Transform car in backdrop.transform)
        {
            if (!car.name.StartsWith("Patrulla", StringComparison.Ordinal)) continue;
            Vector3 floor = car.position;
            floor.y = roadHeight;
            Install(car, car.InverseTransformPoint(floor).y, glass, spill, material);
            foreach (Light light in car.GetComponentsInChildren<Light>(true))
                if (light.name == "Haz del faro (1)") light.enabled = false;
        }
    }

    private static void Install(Transform car, float localFloor, Mesh glass, Mesh spill, Material material)
    {
        Transform rig = car.Find(PoliceLights.RigName);
        if (!rig)
        {
            rig = new GameObject(PoliceLights.RigName).transform;
            rig.SetParent(car, false);
        }
        foreach (string oldName in new[] { "Lightbar Base", "Red Beacon", "Blue Beacon", "Beacon Light" })
        {
            Transform old = rig.Find(oldName);
            if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        rig.localPosition = Vector3.zero;
        rig.localRotation = Quaternion.identity;
        rig.localScale = Vector3.one;
        InstallMesh(rig, PoliceLights.GlassName, glass, material, 0f);
        InstallMesh(rig, PoliceLights.SpillName, spill, material, localFloor);
    }

    internal static void InstallMesh(Transform rig, string name, Mesh mesh, Material material, float localY)
    {
        Transform node = rig.Find(name);
        if (!node)
        {
            node = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)).transform;
            node.SetParent(rig, false);
        }
        node.gameObject.layer = rig.parent.gameObject.layer;
        node.localPosition = Vector3.up * localY;
        node.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = node.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    private static Mesh SharedMesh(string name, Func<Mesh> create)
    {
        string path = Folder + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh) return mesh;
        mesh = create();
        mesh.name = name;
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Mesh MakeGlass(Mesh original)
    {
        var shape = new Surface(false);
        // Illuminate the factory lenses in place. Coordinates are the FBX's local centimetre scale.
        using (var data = MeshUtility.AcquireReadOnlyMeshData(original))
        using (var points = new NativeArray<Vector3>(original.vertexCount, Allocator.Temp))
        {
            data[0].GetVertices(points);
            for (int sub = 0; sub < original.subMeshCount; sub++)
            {
                using (var indices = new NativeArray<int>(data[0].GetSubMesh(sub).indexCount, Allocator.Temp))
                {
                    data[0].GetIndices(indices, sub);
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        Vector3 a = points[indices[i]], b = points[indices[i + 1]], c = points[indices[i + 2]];
                        Vector3 center = (a + b + c) / 3f;
                        if (center.y < .01075f || Mathf.Abs(center.x + .0003973f) < .00125f) continue;
                        Color side = center.x < -.0003973f ? Red : Blue;
                        Vector3 outward = Vector3.Cross(b-a, c-a).normalized * .000004f;
                        int first = shape.Add(a + outward, side);
                        shape.Add(b + outward, side);
                        shape.Add(c + outward, side);
                        shape.Triangle(first, first + 1, first + 2);
                    }
                }
            }
        }
        if (shape.Count == 0) throw new InvalidOperationException("No se encontró la barra original de PoliceCar.");

        // Small chamfered headlamp lenses follow the front edge of the bumper.
        foreach (float x in new[] { -.00571f, .00492f })
        {
            const float y = .00516f, z = .01884f, w = .00104f, h = .00061f, bevel = .00018f;
            int first = shape.Add(new Vector3(x-w+bevel,y-h,z), Headlight);
            shape.Add(new Vector3(x+w-bevel,y-h,z), Headlight);
            shape.Add(new Vector3(x+w,y-h+bevel,z), Headlight);
            shape.Add(new Vector3(x+w,y+h-bevel,z), Headlight);
            shape.Add(new Vector3(x+w-bevel,y+h,z), Headlight);
            shape.Add(new Vector3(x-w+bevel,y+h,z), Headlight);
            shape.Add(new Vector3(x-w,y+h-bevel,z), Headlight);
            shape.Add(new Vector3(x-w,y-h+bevel,z), Headlight);
            for (int i = 1; i < 7; i++) shape.Triangle(first, first+i, first+i+1);
        }
        return shape.Build();
    }

    private static Mesh MakeSpill()
    {
        var shape = new Surface(true);
        for (int side = -1; side <= 1; side += 2)
        {
            // A soft elliptical flash beside the car; the tires hide its inner edge.
            const int spokes = 16, rings = 3;
            Color sideColor = side < 0 ? Red : Blue;
            float cx = -.000397f + side * .0064f;
            int first = shape.Count;
            for (int ring = 0; ring <= rings; ring++)
            {
                float r = ring / (float)rings;
                sideColor.a = .46f * (1f - r) * (1f - r);
                for (int i = 0; i < spokes; i++)
                {
                    float angle = 2f * Mathf.PI * i / spokes;
                    shape.Add(new Vector3(cx + .015f * r * Mathf.Cos(angle), 0f,
                        -.0016f + .021f * r * Mathf.Sin(angle)), sideColor);
                    if (ring == 0) continue;
                    int inner = first + (ring-1)*spokes, outer = inner+spokes, next=(i+1)%spokes;
                    shape.Triangle(inner+i, outer+next, outer+i);
                    if (ring > 1) shape.Triangle(inner+i, inner+next, outer+next);
                }
            }

            AddHeadlightSpill(shape, side, -.000397f, .0053f, .0186f);
        }
        return shape.Build();
    }

    internal static Mesh MakeHeadlightSpill(float centerX, float spacing, float frontZ)
    {
        var shape = new Surface(true);
        AddHeadlightSpill(shape, -1, centerX, spacing, frontZ);
        AddHeadlightSpill(shape, 1, centerX, spacing, frontZ);
        return shape.Build();
    }

    // The two headlight pools widen forward and fade toward their edges.
    private static void AddHeadlightSpill(Surface shape, int side, float centerX, float spacing, float frontZ)
    {
        const int rows = 7, columns = 8;
        int first = shape.Count;
        for (int row = 0; row <= rows; row++)
        {
            float t = row / (float)rows;
            float fade = Mathf.Min(1f, 7f*t) * (1f - t) * (1f - t);
            for (int column = 0; column <= columns; column++)
            {
                float u = 2f * column / columns - 1f;
                Color color = Headlight;
                color.a = .52f * fade * (1f-u*u) * (1f-u*u);
                shape.Add(new Vector3(centerX + side*(spacing+t*.003f) + u*Mathf.Lerp(.002f,.012f,t),
                    0f, frontZ + .043f*t), color);
                if (row == 0 || column == 0) continue;
                int d = first + row*(columns+1) + column, a = d-columns-2;
                shape.Triangle(a, d-1, d);
                shape.Triangle(a, d, a+1);
            }
        }
    }

    private sealed class Surface
    {
        private readonly bool floor;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<Vector2> uv = new List<Vector2>();
        private readonly List<int> indices = new List<int>();
        public int Count => vertices.Count;
        public Surface(bool floor) { this.floor = floor; }
        public int Add(Vector3 position, Color color)
        {
            int index = vertices.Count;
            vertices.Add(position);
            colors.Add(color);
            uv.Add(floor ? Vector2.right : Vector2.zero);
            return index;
        }
        public void Triangle(int a, int b, int c) { indices.Add(a); indices.Add(b); indices.Add(c); }
        public Mesh Build()
        {
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0);
            return mesh;
        }
    }
}
