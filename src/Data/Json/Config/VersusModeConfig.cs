using Il2CppReloaded.Gameplay;

namespace ReplantedOnline.Data.Json.Config;

/// <summary>
/// Represents the root configuration object for versus mode, containing all seed packet and zombie configurations.
/// </summary>
internal sealed class VersusModeConfig : JsonObject<VersusModeConfig>
{
    /// <summary>
    /// Gets the collection of seed packet configurations for plants and zombies available in versus mode.
    /// </summary>
    public List<SeedPacketConfig> SeedPacketConfigs { get; init; } = [];

    /// <summary>
    /// Gets the collection of zombie configurations defining health values for each zombie type.
    /// </summary>
    public List<ZombieConfig> ZombieConfigs { get; init; } = [];

    private readonly Dictionary<SeedType, SeedPacketConfig> _seedPacketConfigLookup = [];
    private readonly Dictionary<ZombieType, ZombieConfig> _zombieConfigLookup = [];

    /// <inheritdoc/>
    internal sealed override void OnDeserialize()
    {
        _seedPacketConfigLookup.Clear();
        foreach (var seedPacketConfig in SeedPacketConfigs)
        {
            _seedPacketConfigLookup[seedPacketConfig.Type] = seedPacketConfig;
        }
        _zombieConfigLookup.Clear();
        foreach (var zombieConfig in ZombieConfigs)
        {
            _zombieConfigLookup[zombieConfig.Type] = zombieConfig;
        }
    }

    /// <summary>
    /// Retrieves the seed packet configuration for the specified seed type.
    /// </summary>
    /// <param name="seedType">The seed type to look up.</param>
    /// <returns>The seed packet configuration for the specified seed type.</returns>
    /// <exception cref="Exception">Thrown when the seed type is not found in the configuration.</exception>
    internal SeedPacketConfig GetSeedPacketConfig(SeedType seedType)
    {
        if (_seedPacketConfigLookup.TryGetValue(seedType, out var config))
        {
            return config;
        }

        throw new Exception($"SeedPacketConfig not found by {Enum.GetName(seedType)}");
    }

    /// <summary>
    /// Attempts to retrieve the seed packet configuration for the specified seed type.
    /// </summary>
    /// <param name="seedType">The seed type to look up.</param>
    /// <param name="config">When this method returns, contains the seed packet configuration if found; otherwise, null.</param>
    /// <returns>true if the seed packet configuration was found; otherwise, false.</returns>
    internal bool TryGetSeedPacketConfig(SeedType seedType, out SeedPacketConfig config)
    {
        return _seedPacketConfigLookup.TryGetValue(seedType, out config!);
    }

    /// <summary>
    /// Retrieves the zombie configuration for the specified zombie type.
    /// </summary>
    /// <param name="zombieType">The zombie type to look up.</param>
    /// <returns>The zombie configuration for the specified zombie type.</returns>
    /// <exception cref="Exception">Thrown when the zombie type is not found in the configuration.</exception>
    internal ZombieConfig GetZombieConfig(ZombieType zombieType)
    {
        if (_zombieConfigLookup.TryGetValue(zombieType, out var config))
        {
            return config;
        }

        throw new Exception($"ZombieConfig not found by {Enum.GetName(zombieType)}");
    }

    /// <summary>
    /// Attempts to retrieve the zombie configuration for the specified zombie type.
    /// </summary>
    /// <param name="zombieType">The zombie type to look up.</param>
    /// <param name="config">When this method returns, contains the zombie configuration if found; otherwise, null.</param>
    /// <returns>true if the zombie configuration was found; otherwise, false.</returns>
    internal bool TryGetZombieConfig(ZombieType zombieType, out ZombieConfig config)
    {
        return _zombieConfigLookup.TryGetValue(zombieType, out config!);
    }
}