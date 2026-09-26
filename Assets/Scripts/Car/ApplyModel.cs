using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ApplyModel : MonoBehaviour
{
    private string materialAddress = "Color_Red";
    private const string SkinPreferenceKey = "SelectedCarMaterial";

    private Renderer objectRenderer;
    private AsyncOperationHandle<Material> materialHandle;

    private void Awake()
    {
        objectRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey(SkinPreferenceKey))
        {
            materialAddress = PlayerPrefs.GetString(SkinPreferenceKey);
        }

        LoadMaterial();
    }

    public void LoadMaterial()
    {
        materialHandle = Addressables.LoadAssetAsync<Material>(materialAddress);
        materialHandle.Completed += OnMaterialLoaded;
    }

    private void OnMaterialLoaded(AsyncOperationHandle<Material> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            objectRenderer.material = handle.Result;
        }
    }

    private void OnDestroy()
    {
        if (materialHandle.IsValid())
        {
            Addressables.Release(materialHandle);
        }
    }

    public void UpdateCatalog()
    {
        StartCoroutine(UpdateCatalogAndReload());
    }
    
    private IEnumerator UpdateCatalogAndReload()
    {
        var updateHandle = Addressables.UpdateCatalogs();
        yield return updateHandle;
        Addressables.Release(updateHandle);

        if (materialHandle.IsValid())
        {
            Addressables.Release(materialHandle);
        }

        var clearHandle = Addressables.ClearDependencyCacheAsync(materialAddress, false);
        yield return clearHandle;
        Addressables.Release(clearHandle);

        if (PlayerPrefs.HasKey(SkinPreferenceKey))
        {
            materialAddress = PlayerPrefs.GetString(SkinPreferenceKey);
        }

        LoadMaterial();
    }
}
