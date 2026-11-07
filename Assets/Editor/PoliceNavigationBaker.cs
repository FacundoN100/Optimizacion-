using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class PoliceNavigationBaker
{
    private const string AssetPath = "Assets/Navigation/PoliceNavigation.asset";

    [MenuItem("Tools/Police Navigation/Rebuild for active scene")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before baking navigation.");
        var manager = UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None)
            .Single(m => m.CompareTag("Player"));
        var serialized = new SerializedObject(manager);
        var prefab = (GameObject)serialized.FindProperty("combat.enemies.enemyPrefab").objectReferenceValue;
        var floor = GameObject.Find("Floor");
        var floorCollider = floor != null ? floor.GetComponent<Collider>() : null;
        if (floorCollider == null || floorCollider.attachedRigidbody != null || floorCollider.isTrigger)
            throw new InvalidOperationException("The scene requires a solid static Floor collider.");

        var flags = GameObjectUtility.GetStaticEditorFlags(floor);
        GameObjectUtility.SetStaticEditorFlags(floor, flags | StaticEditorFlags.BatchingStatic
            | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        Physics.SyncTransforms();
        var sources = new List<NavMeshBuildSource>();
        Bounds bounds = floorCollider.bounds;
        bounds.Expand(new Vector3(0f, 80f, 0f));
        NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 1,
            new List<NavMeshBuildMarkup>(), sources);
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            NavMeshBuildSource source = sources[i];
            var collider = source.component as Collider;
            if (collider == null || collider.gameObject.scene != SceneManager.GetActiveScene()
                || collider.attachedRigidbody != null || collider.isTrigger || !collider.enabled)
            {
                sources.RemoveAt(i);
                continue;
            }
            source.area = collider == floorCollider ? 0 : 1;
            sources[i] = source;
        }
        var box = prefab.GetComponent<BoxCollider>();
        Vector3 size = Vector3.Scale(box.size, prefab.transform.lossyScale);
        var settings = NavMesh.GetSettingsByIndex(0);
        settings.agentRadius = Mathf.Ceil((new Vector2(size.x, size.z).magnitude * 0.5f + 0.6f) * 10f) / 10f;
        settings.agentHeight = Mathf.Max(size.y, 1.5f);
        settings.agentClimb = 0.2f;
        settings.agentSlope = 15f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.25f;
        settings.overrideTileSize = true;
        settings.tileSize = 128;
        var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        if (data == null) throw new InvalidOperationException("Navigation bake failed.");
        data.name = "PoliceNavigation";
        if (!AssetDatabase.IsValidFolder("Assets/Navigation")) AssetDatabase.CreateFolder("Assets", "Navigation");
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(AssetPath);
        if (existing == null) AssetDatabase.CreateAsset(data, AssetPath);
        else
        {
            EditorUtility.CopySerialized(data, existing);
            UnityEngine.Object.DestroyImmediate(data);
            data = existing;
            EditorUtility.SetDirty(data);
        }
        serialized.FindProperty("combat.enemies.navigationData").objectReferenceValue = data;
        serialized.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Police navigation baked from {sources.Count} solid colliders. Radius: {settings.agentRadius:F2}. Save the scene to persist the reference.");
    }
}
