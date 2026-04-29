namespace ReplantedOnline.Interfaces.Data;

/// <summary>
/// Defines a contract for asset reference overrides that can inject custom assets into Unity Addressables operations at runtime.
/// </summary>
internal interface IAssetReferenceOverride
{
    private static readonly List<IAssetReferenceOverride> customAssetReferences = [];

    /// <summary>
    /// Registers a custom asset reference override.
    /// </summary>
    /// <param name="customAssetReference">The override instance to register.</param>
    internal static void Register(IAssetReferenceOverride customAssetReference)
    {
        if (!customAssetReferences.Contains(customAssetReference))
        {
            customAssetReferences.Add(customAssetReference);
        }
    }

    /// <summary>
    /// Updates all registered asset reference overrides.
    /// </summary>
    internal static void UpdateAllOverrides()
    {
        foreach (var customAssetReference in customAssetReferences)
        {
            customAssetReference.UpdateOverride();
        }
    }

    /// <summary>
    /// Applies the override logic for the implementing asset reference.
    /// </summary>
    void UpdateOverride();
}