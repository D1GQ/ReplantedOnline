namespace ReplantedOnline.Data.Asset.Resource;

/// <summary>
/// Represents an abstract base class for loading and managing Unity assets from resources.
/// </summary>
/// <typeparam name="T">The type a asset represents.</typeparam>
/// <param name="path">The resource path to the asset file.</param>
internal abstract class ResourceAsset<T>(string path) where T : class
{
    /// <summary>
    /// Gets the loaded asset instance.
    /// </summary>
    /// <value>The asset of type <typeparamref name="T"/> once loaded; otherwise, null.</value>
    /// <exception cref="Exception">Thrown when the asset failed to load.</exception>
    public T Asset
    {
        get
        {
            if (field == null && !Failed && !Loadded)
            {
                Load();
            }

            if (Failed)
            {
                throw new Exception($"Failed to load asset: {ResourcePath}");
            }

            return field!;
        }
        set;
    }

    /// <summary>
    /// Gets the resource path used to load this asset.
    /// </summary>
    /// <value>The full resource path to the asset file.</value>
    internal string ResourcePath { get; } = path;

    /// <summary>
    /// Gets a value indicating whether the asset has been successfully loaded.
    /// </summary>
    /// <value><c>true</c> if the asset is loaded; otherwise, <c>false</c>.</value>
    public bool Loadded { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether the asset failed to load.
    /// </summary>
    /// <value><c>true</c> if the asset failed to load; otherwise, <c>false</c>.</value>
    public bool Failed { get; protected set; }

    /// <summary>
    /// Loads the asset from the resource path.
    /// </summary>
    /// <remarks>
    /// This method must be implemented by derived classes to handle the specific loading logic
    /// for the asset type. After successful loading, <see cref="Asset"/> should be assigned
    /// and <see cref="Loadded"/> should be set to <c>true</c>.
    /// </remarks>
    internal abstract void Load();
}