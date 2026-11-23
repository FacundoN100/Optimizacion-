using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor-only repair for the openings in PlayerCar.fbx. All instances share one mesh and material.
public static class AsfaltoBodywork
{
    private const string Folder = "Assets/Models/Materials/";
    private const string DetailName = "Parabrisas y frente";

    public static void Apply(Transform player)
    {
        MeshFilter body = player.GetComponent<MeshFilter>();
        if (!body || !body.sharedMesh || AssetDatabase.GetAssetPath(body.sharedMesh) != "Assets/Models/PlayerCar.fbx")
            throw new InvalidOperationException("La reparación requiere la malla original de PlayerCar: " + player.name);

        Transform part = player.Find(DetailName);
        if (!part)
        {
            part = new GameObject(DetailName, typeof(MeshFilter), typeof(MeshRenderer)).transform;
            part.SetParent(player, false);
        }
        part.GetComponent<MeshFilter>().sharedMesh = SharedMesh();
        part.GetComponent<MeshRenderer>().sharedMaterial = SharedMaterial();

        // Older scenes have cuboid fillers hidden behind the fitted fascia.
        Transform oldFront = player.Find("Frente y faros");
        if (oldFront)
        {
            foreach (Transform element in oldFront)
            {
                if (element.name == "Parrilla" || element.name == "Paragolpes continuo")
                    element.gameObject.SetActive(false);
            }
        }
    }

    private static Mesh SharedMesh()
    {
        string path = Folder + "PlayerCar_FrontDetail.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh) return mesh;

        var vertices = new List<Vector3>(16);
        var uv = new List<Vector2>(16);
        var triangles = new List<int>(24);
        // Coordinates measured at roof, A-pillars, hood and headlamp seams in the source FBX.
        // The original importer converts centimetres into Unity units.
        void Segment(float leftBottom, float rightBottom, float bottomY, float bottomZ,
                     float leftTop, float rightTop, float topY, float topZ, float uvBottom, float uvTop)
        {
            int start = vertices.Count;
            vertices.Add(new Vector3(leftBottom, bottomY, bottomZ) * .01f);
            vertices.Add(new Vector3(rightBottom, bottomY, bottomZ) * .01f);
            vertices.Add(new Vector3(rightTop, topY, topZ) * .01f);
            vertices.Add(new Vector3(leftTop, topY, topZ) * .01f);
            uv.Add(new Vector2(.004f, uvBottom));
            uv.Add(new Vector2(.996f, uvBottom));
            uv.Add(new Vector2(.996f, uvTop));
            uv.Add(new Vector2(.004f, uvTop));
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        Segment(-.721f, .714f, .747f, 1.106f, -.638f, .631f, 1.104f, .769f, .508f, .992f);
        // A narrow sill joins the broad lower bumper to the grille between the factory headlights.
        Segment(-.725f, .718f, .237f, 2.195f, -.718f, .711f, .360f, 2.211f, .008f, .114f);
        Segment(-.718f, .711f, .360f, 2.211f, -.710f, .704f, .440f, 2.200f, .114f, .190f);
        Segment(-.591f, .584f, .437f, 2.197f, -.589f, .582f, .587f, 2.197f, .200f, .484f);

        mesh = new Mesh { name = "PlayerCar_FrontDetail" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static Material SharedMaterial()
    {
        string path = Folder + "PlayerCar_FrontDetail.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Smoothness", .55f);
            material.SetFloat("_Metallic", .15f);
            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, path);
        }

        Texture2D atlas = FindAtlas(path);
        if (!atlas)
        {
            atlas = MakeAtlas();
            AssetDatabase.AddObjectToAsset(atlas, path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            atlas = FindAtlas(path);
        }
        if (!atlas) throw new InvalidOperationException("No se pudo recuperar el atlas de PlayerCar.");
        if (material.GetTexture("_BaseMap") == atlas && material.GetTexture("_EmissionMap") == atlas)
            return material;

        material.SetTexture("_BaseMap", atlas);
        material.SetTexture("_MainTex", atlas);
        material.SetTexture("_EmissionMap", atlas);
        material.SetColor("_EmissionColor", new Color(.13f, .15f, .17f));
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        return material;
    }

    private static Texture2D FindAtlas(string path)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Texture2D texture && texture.name == "Vidrio y parrilla") return texture;
        return null;
    }

    private static Texture2D MakeAtlas()
    {
        var texture = new Texture2D(128, 128, TextureFormat.RGB24, true)
        {
            name = "Vidrio y parrilla", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear
        };
        var pixels = new Color32[texture.width * texture.height];
        for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
                pixels[y * texture.width + x] = AtlasColor(x, y);
        texture.SetPixels32(pixels);
        texture.Apply(true);
        return texture;
    }

    private static Color32 AtlasColor(int x, int y)
    {
        if (y >= 64)
        {
            float t = (y - 64) / 63f;
            Color color = Color.Lerp(new Color(.17f, .36f, .43f), new Color(.36f, .62f, .7f), t);
            float reflection = x / 127f + t * .24f;
            if (reflection > .45f && reflection < .52f || reflection > .54f && reflection < .555f)
                color = Color.Lerp(color, new Color(.71f, .86f, .88f), .4f);
            if (y < 67 || y > 124 || x < 3 || x > 124)
                color = new Color(.075f, .12f, .15f);
            return color;
        }

        if (y < 24)
        {
            if (y > 11 && y < 15) return new Color32(88, 101, 110, 255);
            return new Color32(43, 52, 61, 255);
        }
        if (x < 4 || x > 123 || y < 28 || y > 60) return new Color32(82, 95, 104, 255);
        int rib = (y - 29) % 10;
        if (rib == 0) return new Color32(118, 129, 136, 255);
        if (rib < 3) return new Color32(76, 88, 97, 255);
        return new Color32(31, 38, 44, 255);
    }
}
