using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class ApplyModel : IDisposable
{
    private const string SkinPreferenceKey = "SelectedCarMaterial";
    private readonly Renderer[] renderers;
    private AsyncOperationHandle<Material> materialHandle;
    private AsyncOperationHandle catalogHandle;
    private AsyncOperationHandle cacheHandle;
    private string materialAddress;
    private bool disposed;
    private bool updatingCatalog;

    public ApplyModel(Renderer[] targets)
    {
        renderers = targets ?? Array.Empty<Renderer>();
    }

    public void LoadMaterial()
    {
        if (disposed || renderers.Length == 0) return;
        ReleaseMaterial();
        materialAddress = PlayerPrefs.GetString(SkinPreferenceKey, "Color_Red");
        materialHandle = Addressables.LoadAssetAsync<Material>(materialAddress);
        materialHandle.Completed += OnMaterialLoaded;
    }

    private void OnMaterialLoaded(AsyncOperationHandle<Material> handle)
    {
        if (disposed || !handle.Equals(materialHandle)
            || handle.Status != AsyncOperationStatus.Succeeded) return;
        foreach (Renderer target in renderers)
        {
            if (target != null) target.sharedMaterial = handle.Result;
        }
    }

    public IEnumerator UpdateCatalog()
    {
        if (disposed || updatingCatalog) yield break;
        updatingCatalog = true;
        try
        {
            catalogHandle = Addressables.UpdateCatalogs(null, false);
            yield return catalogHandle;
            if (disposed || catalogHandle.Status != AsyncOperationStatus.Succeeded) yield break;
            Release(ref catalogHandle);

            ReleaseMaterial();
            materialAddress = PlayerPrefs.GetString(SkinPreferenceKey, "Color_Red");
            cacheHandle = Addressables.ClearDependencyCacheAsync(materialAddress, false);
            yield return cacheHandle;
            if (disposed) yield break;
            Release(ref cacheHandle);
            LoadMaterial();
        }
        finally
        {
            Release(ref catalogHandle);
            Release(ref cacheHandle);
            updatingCatalog = false;
        }
    }

    private void ReleaseMaterial()
    {
        if (!materialHandle.IsValid()) return;
        materialHandle.Completed -= OnMaterialLoaded;
        Addressables.Release(materialHandle);
        materialHandle = default;
    }

    private static void Release(ref AsyncOperationHandle handle)
    {
        if (handle.IsValid()) Addressables.Release(handle);
        handle = default;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ReleaseMaterial();
        Release(ref catalogHandle);
        Release(ref cacheHandle);
    }
}
