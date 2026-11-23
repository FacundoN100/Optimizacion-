using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

// Bake steady running lights into the player previews and the drivable car.
public static class AsfaltoPlayerLighting
{
    private const string Folder = "Assets/Models/Materials/";
    public const string RigName = "Luces del jugador";
    public const string LensName = "Opticas delanteras y traseras";
    public const string SpillName = "Haces delanteros en el piso";
    private const float FrontCenterX = -.000035f;
    private const float FrontSpacing = .00679f;
    private const float FrontZ = .02232f;

    public static void Apply(Transform car)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Police/PoliceOptics.mat");
        if (!material) throw new InvalidOperationException("Falta el material compartido de las luces.");
        Mesh lenses = SharedLenses(car.GetComponent<MeshFilter>().sharedMesh);
        const string spillPath = Folder + "PlayerCar_HeadlightSpill.asset";
        Mesh spill = AssetDatabase.LoadAssetAtPath<Mesh>(spillPath);
        if (!spill)
        {
            spill = AsfaltoPoliceLighting.MakeHeadlightSpill(FrontCenterX, FrontSpacing, FrontZ);
            spill.name = "PlayerCar_HeadlightSpill";
            AssetDatabase.CreateAsset(spill, spillPath);
        }
        Transform rig = car.Find(RigName);
        if (!rig)
        {
            rig = new GameObject(RigName).transform;
            rig.SetParent(car, false);
        }
        AsfaltoPoliceLighting.InstallMesh(rig, LensName, lenses, material, 0f);
        AsfaltoPoliceLighting.InstallMesh(rig, SpillName, spill, material, GroundOffset(car));

        Transform previous = car.Find("Frente y faros");
        if (!previous) return;
        foreach (Transform part in previous)
        {
            if (part.name == "Optica" && part.TryGetComponent(out MeshRenderer oldLens))
                oldLens.enabled = false;
            if (part.name == "Haz del faro" && part.TryGetComponent(out Light lamp))
                lamp.enabled = false;
            if (car.name == "Fugitivo" && part.name == "Reflejo en el asfalto (1)" &&
                part.TryGetComponent(out MeshFilter filter) && filter.sharedMesh &&
                AssetDatabase.GetAssetPath(filter.sharedMesh) == "Assets/Models/Police/PoliceRoadSpill.asset" &&
                part.TryGetComponent(out MeshRenderer oldSpill))
                oldSpill.enabled = false;
        }
    }

    private static float GroundOffset(Transform car)
    {
        // In the workshop the hood extends beyond the edge of the display turntable.
        // Both beams therefore fall on the floor below it.
        const float clearance = .025f;
        if (car.TryGetComponent(out BoxCollider body))
            return body.center.y - body.size.y * .5f + clearance / car.lossyScale.y;
        Transform ground = car.parent.Find("Asfalto");
        if (!ground) ground = car.parent.Find("Suelo");
        if (!ground || !ground.TryGetComponent(out Renderer surface))
            throw new InvalidOperationException("Falta la superficie bajo " + car.name);
        Vector3 point = car.position;
        point.y = surface.bounds.max.y + clearance;
        return car.InverseTransformPoint(point).y;
    }

    // Limits of the two glass shells, measured in the local space of the imported FBX.
    // The painted bumper and grille immediately surrounding them lie outside these bounds.
    private static bool IsHeadlampVertex(Vector3 point)
    {
        float x = Mathf.Abs(point.x);
        return x > .00575f && x < .00695f && point.y > .00438f && point.y < .00586f &&
               point.z > .02034f && point.z < .02181f;
    }

    private static Mesh SharedLenses(Mesh body)
    {
        var vertices = new List<Vector3>(60);
        var channels = new List<Color32>(60);
        var uv = new List<Vector2>(60);
        var indices = new List<int>(84);

        // Copy the actual front, top and outside faces of the factory lamps. A shared
        // outward offset per side keeps the shell joined, including at its upper edge.
        using (var data = MeshUtility.AcquireReadOnlyMeshData(body))
        using (var points = new NativeArray<Vector3>(body.vertexCount, Allocator.Temp))
        {
            data[0].GetVertices(points);
            for (int sub = 0; sub < body.subMeshCount; sub++)
            {
                using (var faces = new NativeArray<int>(data[0].GetSubMesh(sub).indexCount, Allocator.Temp))
                {
                    data[0].GetIndices(faces, sub);
                    for (int i = 0; i < faces.Length; i += 3)
                    {
                        Vector3 a = points[faces[i]], b = points[faces[i + 1]], c = points[faces[i + 2]];
                        if (!IsHeadlampVertex(a) || !IsHeadlampVertex(b) || !IsHeadlampVertex(c)) continue;
                        Vector3 normal = Vector3.Cross((b - a) * 1000f, (c - a) * 1000f).normalized;
                        // The forward glass is brightest. Top and wraparound side keep the same hue.
                        float intensity = .68f + .32f * Mathf.Abs(normal.z);
                        Color32 color = new Color(0f, intensity, 0f);
                        Vector3 offset = new Vector3(Mathf.Sign(a.x), 1f, 1f) * .000006f;
                        for (int j = i; j < i + 3; j++)
                        {
                            indices.Add(vertices.Count);
                            vertices.Add(points[faces[j]] + offset);
                            channels.Add(color);
                            uv.Add(Vector2.zero);
                        }
                    }
                }
            }
        }
        if (vertices.Count == 0) throw new InvalidOperationException("No se encontraron las ópticas originales de PlayerCar.");

        void RearLens(float x)
        {
            int first = vertices.Count;
            const float bevel = .00018f, halfWidth = .00114f, halfHeight = .00124f;
            void Point(float dx, float dy)
            {
                // Original rear sockets end at z=-.015950, below the trunk lid.
                vertices.Add(new Vector3(x + dx, .00585f + dy, -.01596f));
                channels.Add(Color.red);
                uv.Add(new Vector2(0f, .63f));
            }
            Point(-halfWidth+bevel, -halfHeight); Point(halfWidth-bevel, -halfHeight);
            Point(halfWidth, -halfHeight+bevel);  Point(halfWidth, halfHeight-bevel);
            Point(halfWidth-bevel, halfHeight);   Point(-halfWidth+bevel, halfHeight);
            Point(-halfWidth, halfHeight-bevel); Point(-halfWidth, -halfHeight+bevel);
            for (int i = 1; i < 7; i++)
            {
                indices.Add(first);
                indices.Add(first + i + 1);
                indices.Add(first + i);
            }
        }

        for (int side = -1; side <= 1; side += 2)
            RearLens(-.000038f + side * .0067485f);
        // Refill the original Unity mesh so both the live GPU buffer and scene references update.
        const string path = Folder + "PlayerCar_LampLenses.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        var mesh = existing ? existing : new Mesh { name = "PlayerCar_LampLenses" };
        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetColors(channels);
        mesh.SetUVs(0, uv);
        mesh.SetTriangles(indices, 0);
        if (existing)
        {
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
        }
        else AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}
