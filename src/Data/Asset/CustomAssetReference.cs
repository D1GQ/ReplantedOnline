using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ReplantedOnline.Data.Asset;

/// <summary>
/// Base class for custom asset references.
/// </summary>
internal abstract class CustomAssetReference
{
    /// <summary>
    /// Stores all registered custom asset references keyed by their full GUID
    /// </summary>
    private static readonly Dictionary<string, CustomAssetReference> CustomAssetReferences = [];

    /// <summary>
    /// The full GUID of this custom asset reference, including the custom prefix.
    /// </summary>
    protected string _guid;

    /// <summary>
    /// Registers a custom asset reference into the global lookup dictionary.
    /// </summary>
    /// <param name="customAssetReference">The custom asset reference to register.</param>
    internal static void Register(CustomAssetReference customAssetReference)
    {
        if (!CustomAssetReferences.ContainsKey(customAssetReference._guid))
        {
            CustomAssetReferences[customAssetReference._guid] = customAssetReference;
        }
    }

    /// <summary>
    /// Retrieves a registered custom asset reference by its full GUID.
    /// </summary>
    /// <param name="guid">The full GUID to look up.</param>
    /// <returns>The matching CustomAssetReference if found; otherwise, null.</returns>
    internal static CustomAssetReference GetByGuid(string guid)
    {
        if (CustomAssetReferences.TryGetValue(guid, out var asset))
        {
            return asset;
        }

        return null;
    }

    /// <summary>
    /// Loads the asset associated with this custom reference asynchronously.
    /// </summary>
    /// <returns>An AsyncOperationHandle representing the asset load operation.</returns>
    internal abstract AsyncOperationHandle<UnityEngine.Object> LoadAssetAsync();

    /// <summary>
    /// Determines whether the given asset reference is a custom runtime asset reference
    /// </summary>
    /// <param name="assetReference">The asset reference to validate.</param>
    /// <returns>True if the asset reference's GUID starts with the custom asset reference prefix; otherwise, false.</returns>
    internal static bool IsValid(AssetReference assetReference)
    {
        return assetReference.AssetGUID.StartsWith(ReplantedOnlineAssets.CUSTOM_ASSET_REF_GUID_PREFIX);
    }
}

/// <summary>
/// Generic sealed class for creating typed custom asset references.
/// </summary>
/// <typeparam name="T">The type of AssetReference to create, must inherit from AssetReference.</typeparam>
internal sealed class CustomAssetReference<T> : CustomAssetReference where T : AssetReference
{
    /// <summary>
    /// The cached UnityEngine.Object that this custom reference represents.
    /// </summary>
    private readonly UnityEngine.Object _asset;

    /// <summary>
    /// Gets the typed asset reference instance created with the custom prefixed GUID.
    /// </summary>
    internal T AssetRef { get; private set; }

    /// <summary>
    /// Initializes a new instance of the CustomAssetReference class with a custom GUID prefix.
    /// </summary>
    /// <param name="guid">The unique identifier for the asset (without prefix). Will be prefixed with CUSTOM_ASSET_REF_GUID_PREFIX.</param>
    /// <param name="asset">The UnityEngine.Object to be returned when this reference is loaded.</param>
    internal CustomAssetReference(string guid, UnityEngine.Object asset)
    {
        _guid = ReplantedOnlineAssets.CUSTOM_ASSET_REF_GUID_PREFIX + guid;
        _asset = asset;
        AssetRef = Activator.CreateInstance(typeof(T), args: _guid) as T;
    }

    /// <summary>
    /// Loads the asset associated with this custom reference.
    /// </summary>
    /// <returns>A completed AsyncOperationHandle containing the cached UnityEngine.Object.</returns>
    internal override AsyncOperationHandle<UnityEngine.Object> LoadAssetAsync()
    {
        var operation = Addressables.ResourceManager.CreateCompletedOperation(_asset, "");
        AssetRef.m_Operation = operation;
        return operation;
    }
}