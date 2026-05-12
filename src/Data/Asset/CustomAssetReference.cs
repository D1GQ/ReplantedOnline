using UnityEngine.AddressableAssets;

namespace ReplantedOnline.Data.Asset;

/// <summary>
/// Base class for custom asset references used in the Replanted Online data system.
/// </summary>
internal abstract class CustomAssetReference
{
    /// <summary>
    /// Determines whether the given asset reference is a custom runtime asset reference.
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
    /// Initializes a new instance of the CustomAssetReference class with a custom GUID prefix.
    /// </summary>
    /// <param name="guid">The unique identifier for the asset, which will be prefixed with the custom asset reference GUID prefix.</param>
    internal CustomAssetReference(string guid)
    {
        Asset = Activator.CreateInstance(typeof(T), args: ReplantedOnlineAssets.CUSTOM_ASSET_REF_GUID_PREFIX + guid) as T;
    }

    /// <summary>
    /// The typed asset reference instance created with the custom prefixed GUID.
    /// </summary>
    internal readonly T Asset;
}