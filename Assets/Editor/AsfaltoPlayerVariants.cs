using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// All customisation is baked in the editor. Each kit shares its mesh and existing materials.
// No changes to the source FBX, the repaired fascia, optical lenses, colliders or handling.
public static class AsfaltoPlayerVariants
{
    private const string MeshFolder = "Assets/Models/Materials/";
    private const string RallyLensPath = "Assets/Settings/AsfaltoGarage/Opticas de noche.mat";

    public static void LightRallyLenses(Transform body)
    {
        var kit = body.Find(CarModelChoice.AccessoriesRoot + "/" + CarModelChoice.VariantName(1));
        if (!kit || !kit.TryGetComponent(out MeshRenderer renderer)) return;
        Material[] materials = renderer.sharedMaterials;
        if (materials.Length < 2) return;
        materials[1] = AssetDatabase.LoadAssetAtPath<Material>(RallyLensPath);
        renderer.sharedMaterials = materials;
    }

    public static void Apply(Transform body, int previewIndex, bool selectable)
    {
        MeshFilter filter = body.GetComponent<MeshFilter>();
        if (!filter || AssetDatabase.GetAssetPath(filter.sharedMesh) != "Assets/Models/PlayerCar.fbx")
            throw new InvalidOperationException("Las variantes requieren PlayerCar.fbx: " + body.name);

        Material trim = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/AsfaltoGarage/Paragolpes.mat");
        Material lens = AssetDatabase.LoadAssetAtPath<Material>(RallyLensPath);
        if (!trim || !lens)
            throw new InvalidOperationException("Preparar los materiales de frente antes de las variantes.");

        Transform root = body.Find(CarModelChoice.AccessoriesRoot);
        if (!root)
        {
            root = new GameObject(CarModelChoice.AccessoriesRoot).transform;
            root.SetParent(body, false);
        }

        previewIndex = Mathf.Clamp(previewIndex, 0, CarModelChoice.Count - 1);
        for (int i = 0; i < CarModelChoice.Count; i++)
        {
            Transform kit = root.Find(CarModelChoice.VariantName(i));
            if (!selectable && i != previewIndex)
            {
                if (kit) kit.gameObject.SetActive(false);
                continue;
            }
            if (!kit)
            {
                kit = new GameObject(CarModelChoice.VariantName(i), typeof(MeshFilter), typeof(MeshRenderer)).transform;
                kit.SetParent(root, false);
            }
            kit.GetComponent<MeshFilter>().sharedMesh = SharedKit(i);
            MeshRenderer renderer = kit.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = i == 1 ? new[] { trim, lens } : new[] { trim };
            renderer.receiveShadows = true;
            kit.gameObject.SetActive(i == previewIndex);
        }
    }

    private static Mesh SharedKit(int index)
    {
        string path = MeshFolder + "PlayerCar_" + CarModelChoice.VariantName(index) + "Kit.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        // Older GT meshes included a rear wing behind the origin. Keep the same
        // asset and GUID when converting them to the hood-only trim.
        if (existing && (index != 2 || existing.bounds.min.z >= 0f)) return existing;
        var mesh = new KitMesh();

        if (index == 0)
        {
            // Street: a low front splitter and side skirts between the wheel arches.
            mesh.Box(new Vector3(0, .205f, 2.14f), new Vector3(1.83f, .055f, .35f));
            for (int side = -1; side <= 1; side += 2)
            {
                mesh.Box(new Vector3(side * .865f, .263f, 2.13f), new Vector3(.045f, .11f, .29f));
                mesh.Box(new Vector3(side * .88f, .245f, .10f), new Vector3(.08f, .10f, 1.65f));
            }
        }
        else if (index == 1)
        {
            // Rally: auxiliary grille lamps (the factory headlights are untouched) and roof intake.
            mesh.Box(new Vector3(0, 1.202f, .10f), new Vector3(.64f, .16f, .64f));
            mesh.Box(new Vector3(0, 1.21f, .435f), new Vector3(.50f, .085f, .025f));
            mesh.Box(new Vector3(0, .365f, 2.25f), new Vector3(.77f, .06f, .09f));
            for (int side = -1; side <= 1; side += 2)
            {
                mesh.Disc(new Vector3(side * .22f, .53f, 2.24f), .145f, .11f, 0);
                mesh.Disc(new Vector3(side * .22f, .53f, 2.302f), .113f, .018f, 1);
            }
        }
        else
        {
            // GT: small hood louvers, clear of the glass and factory lamps.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int slot = 0; slot < 4; slot++)
                    mesh.Box(new Vector3(side * .32f, .748f - slot * .016f, 1.25f + slot * .105f),
                        new Vector3(.28f, .025f, .055f));
            }
        }

        Mesh result = existing ? existing : new Mesh { name = "PlayerCar_" + CarModelChoice.VariantName(index) + "Kit" };
        mesh.Finish(result, index == 1 ? 2 : 1);
        if (existing)
        {
            EditorUtility.SetDirty(result);
            AssetDatabase.SaveAssetIfDirty(result);
        }
        else AssetDatabase.CreateAsset(result, path);
        return result;
    }

    private sealed class KitMesh
    {
        private readonly List<Vector3> vertices = new List<Vector3>(400);
        private readonly List<Vector2> uv = new List<Vector2>(400);
        private readonly List<int>[] triangles = { new List<int>(600), new List<int>(100) };

        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material = 0)
        {
            int offset = vertices.Count;
            vertices.Add(a * .01f); vertices.Add(b * .01f);
            vertices.Add(c * .01f); vertices.Add(d * .01f);
            uv.Add(Vector2.zero); uv.Add(Vector2.right); uv.Add(Vector2.one); uv.Add(Vector2.up);
            triangles[material].AddRange(new[] { offset, offset + 1, offset + 2, offset, offset + 2, offset + 3 });
        }

        private void Triangle(Vector3 a, Vector3 b, Vector3 c, int material)
        {
            int offset = vertices.Count;
            vertices.Add(a * .01f); vertices.Add(b * .01f); vertices.Add(c * .01f);
            uv.Add(new Vector2(.5f, .5f)); uv.Add(Vector2.zero); uv.Add(Vector2.one);
            triangles[material].AddRange(new[] { offset, offset + 1, offset + 2 });
        }

        public void Box(Vector3 position, Vector3 size)
        {
            Vector3 h = size * .5f;
            Vector3 P(float x, float y, float z) => position + new Vector3(x * h.x, y * h.y, z * h.z);
            Quad(P(-1,-1,1), P(1,-1,1), P(1,1,1), P(-1,1,1));
            Quad(P(1,-1,-1), P(-1,-1,-1), P(-1,1,-1), P(1,1,-1));
            Quad(P(1,-1,1), P(1,-1,-1), P(1,1,-1), P(1,1,1));
            Quad(P(-1,-1,-1), P(-1,-1,1), P(-1,1,1), P(-1,1,-1));
            Quad(P(-1,1,1), P(1,1,1), P(1,1,-1), P(-1,1,-1));
            Quad(P(-1,-1,-1), P(1,-1,-1), P(1,-1,1), P(-1,-1,1));
        }

        public void Disc(Vector3 position, float radius, float depth, int material)
        {
            const int sides = 12;
            Vector3 front = position + Vector3.forward * depth * .5f;
            Vector3 rear = position - Vector3.forward * depth * .5f;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                float b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 radialA = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * radius;
                Vector3 radialB = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0) * radius;
                Triangle(front, front + radialA, front + radialB, material);
                Triangle(rear, rear + radialB, rear + radialA, material);
                Quad(rear + radialA, rear + radialB, front + radialB, front + radialA, material);
            }
        }

        public void Finish(Mesh mesh, int submeshes)
        {
            mesh.Clear();
            mesh.subMeshCount = submeshes;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            for (int i = 0; i < submeshes; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
