using ReplantedOnline.Interfaces.Data;
using UnityEngine.AddressableAssets;

namespace ReplantedOnline.Data.Asset;

/// <summary>
/// Represents an override wrapper for an <see cref="AssetReference"/> that allows injecting a custom loaded asset instance into Unity Addressables operations.
/// </summary>
/// <typeparam name="T">The type of asset being overridden.</typeparam>
internal sealed class AssetReferenceOverride<T> : IAssetReferenceOverride where T : UnityEngine.Object
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssetReferenceOverride{T}"/> class.
    /// </summary>
    /// <param name="assetReference">The underlying asset reference to override.</param>
    internal AssetReferenceOverride(AssetReference assetReference)
    {
        _assetReference = assetReference;
    }

    private readonly AssetReference _assetReference;
    private T? _asset;
    private Func<bool>? _shouldApply;

    /// <summary>
    /// Sets the asset instance that will be used when overriding the addressable operation.
    /// </summary>
    /// <param name="asset">The asset instance to inject.</param>
    internal void SetOverride(T asset)
    {
        _asset = asset;
    }

    /// <summary>
    /// Sets the asset instance that will be used when overriding the addressable operation.
    /// </summary>
    /// <param name="asset">The asset instance to inject.</param>
    /// <param name="shouldApply">When the asset should be forcibly injected.</param>
    internal void SetOverride(T asset, Func<bool> shouldApply)
    {
        _asset = asset;
        _shouldApply = shouldApply;
    }

    /// <summary>
    /// Applies the override to the underlying <see cref="AssetReference"/> if it is not already valid.
    /// </summary>
    public void UpdateOverride()
    {
        if (_assetReference == null) return;

        // If in a lobby override assets
        if (_shouldApply == null || _shouldApply.Invoke())
        {
            if (!_assetReference.m_Operation.IsValid() || _assetReference.m_Operation.Result != _asset)
            {
                _assetReference.m_Operation = Addressables.ResourceManager.CreateCompletedOperation(_asset, "");
            }
        }
        else
        {
            if (_assetReference.m_Operation.IsValid())
            {
                _assetReference.ReleaseAsset();
            }
        }
    }
}