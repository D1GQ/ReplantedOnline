using Il2CppReloaded.Gameplay;
using ReplantedOnline.Data.Json.Config.Reloaded.Arenas;
using ReplantedOnline.Data.Json.Converters;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs;
using ReplantedOnline.Utilities.Modded;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Config.Reloaded;

/// <summary>
/// Represents the root configuration object for versus mode, containing all seed packet and zombie configurations.
/// </summary>
internal sealed class VersusModeConfig : JsonObject<VersusModeConfig>, INetworkConfigSerializable
{
    /// <summary>
    /// Gets the initial sky production rate.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime InitialSkyProductionRate { get; set; }

    /// <summary>
    /// Gets the maximum plant and zombie initial production rate.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime InitialProductionRateMax { get; set; }

    /// <summary>
    /// Gets the minimum plant and zombie initial production rate.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime InitialProductionRateMin { get; set; }

    /// <summary>
    /// Gets the sky brain and sun production rate.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime SkyProductionRate { get; set; }

    /// <summary>
    /// Gets the plant sun production rate.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime PlantProductionRate { get; set; }

    /// <summary>
    /// Gets the zombie brain production rate.
    /// </summary>
    [JsonConverter(typeof(IntTimeConverter))]
    public IntTime ZombieProductionRate { get; set; }

    /// <summary>
    /// Gets the plant shooter launch rate global surplus.
    /// </summary>
    public int PlantShooterLaunchRateSurplus { get; set; }

    /// <summary>
    /// Gets a collection of seed types that ignore the initial cooldown period and are available immediately when the match starts.
    /// </summary>
    [JsonConverter(typeof(JsonSeedTypeListConverter))]
    public List<SeedType> IgnoreInitialCooldown { get; set; } = [];

    /// <summary>
    /// Gets the list of seed packets that are disabled in sudden death.
    /// </summary>
    [JsonConverter(typeof(JsonSeedTypeListConverter))]
    public List<SeedType> DisabledSeedPacketsInSuddenDeath { get; set; } = [];

    /// <summary>
    /// Gets the configurations for CloudyDayArena.
    /// </summary>
    public CloudyDayArenaConfig CloudyDayArenaConfig { get; set; } = new();

    /// <summary>
    /// Gets the collection of seed packet configurations for plants and zombies available in versus mode.
    /// </summary>
    public List<PlantConfig> PlantConfigs { get; set; } = [];

    /// <summary>
    /// Gets the collection of zombie configurations defining health values for each zombie type.
    /// </summary>
    public List<ZombieConfig> ZombieConfigs { get; set; } = [];

    private readonly Dictionary<SeedType, SeedPacketConfig> _seedPacketLookup = [];
    private readonly Dictionary<SeedType, PlantConfig> _plantConfigLookup = [];
    private readonly Dictionary<ZombieType, ZombieConfig> _zombieConfigLookup = [];

    /// <summary>
    /// Attempts to retrieve the seed packet configuration for the specified seed type.
    /// </summary>
    /// <param name="seedType">The seed type to look up.</param>
    /// <param name="config">When this method returns, contains the seed packet configuration if found; otherwise, null.</param>
    /// <returns>true if the seed packet configuration was found; otherwise, false.</returns>
    internal bool TryGetSeedPacketConfig(SeedType seedType, out SeedPacketConfig config)
    {
        return _seedPacketLookup.TryGetValue(seedType, out config!);
    }

    /// <summary>
    /// Attempts to retrieve the seed packet configuration for the specified zombie type.
    /// </summary>
    /// <param name="zombieType">The zombie type to look up.</param>
    /// <param name="config">When this method returns, contains the seed packet configuration if found; otherwise, null.</param>
    /// <returns>true if the seed packet configuration was found; otherwise, false.</returns>
    internal bool TryGetSeedPacketConfig(ZombieType zombieType, out SeedPacketConfig config)
    {
        var seedType = PvZRUtils.ZombieTypeToIZombieSeedType(zombieType);
        if (seedType == SeedType.None)
        {
            config = null!;
            return false;
        }

        return _seedPacketLookup.TryGetValue(seedType, out config!);
    }

    /// <summary>
    /// Attempts to retrieve the plant configuration for the specified seed type, excluding zombie seed types.
    /// </summary>
    /// <param name="seedType">The seed type to look up.</param>
    /// <param name="config">When this method returns, contains the plant configuration if found; otherwise, null.</param>
    /// <returns>true if the plant configuration was found and the seed type is not a zombie type; otherwise, false.</returns>
    internal bool TryGetPlantConfig(SeedType seedType, out SeedPacketConfig config)
    {
        if (Challenge.IsZombieSeedType(seedType))
        {
            config = null!;
            return false;
        }

        return _seedPacketLookup.TryGetValue(seedType, out config!);
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

    /// <summary>
    /// Gets the seed packet configuration for the specified seed type.
    /// </summary>
    /// <param name="seedType">The seed type to look up.</param>
    /// <returns>The seed packet configuration for the specified seed type.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the seed type is not found in the configuration.</exception>
    internal SeedPacketConfig GetSeedPacketConfig(SeedType seedType)
    {
        if (!TryGetSeedPacketConfig(seedType, out var config))
        {
            throw new KeyNotFoundException($"Seed packet configuration not found for seed type: {seedType}");
        }

        return config;
    }

    /// <summary>
    /// Gets the seed packet configuration for the specified zombie type.
    /// </summary>
    /// <param name="zombieType">The zombie type to look up.</param>
    /// <returns>The seed packet configuration for the specified zombie type.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the zombie type is not found in the configuration or cannot be converted to a seed type.</exception>
    internal SeedPacketConfig GetSeedPacketConfig(ZombieType zombieType)
    {
        if (!TryGetSeedPacketConfig(zombieType, out var config))
        {
            throw new KeyNotFoundException($"Seed packet configuration not found for zombie type: {zombieType}");
        }

        return config;
    }

    /// <summary>
    /// Gets the plant configuration for the specified seed type, excluding zombie seed types.
    /// </summary>
    /// <param name="seedType">The seed type to look up.</param>
    /// <returns>The plant configuration for the specified seed type.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the seed type is not found in the configuration or is a zombie type.</exception>
    internal SeedPacketConfig GetPlantConfig(SeedType seedType)
    {
        if (!TryGetPlantConfig(seedType, out var config))
        {
            throw new KeyNotFoundException($"Plant configuration not found for seed type: {seedType} (may be a zombie type or not found)");
        }

        return config;
    }

    /// <summary>
    /// Gets the zombie configuration for the specified zombie type.
    /// </summary>
    /// <param name="zombieType">The zombie type to look up.</param>
    /// <returns>The zombie configuration for the specified zombie type.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the zombie type is not found in the configuration.</exception>
    internal ZombieConfig GetZombieConfig(ZombieType zombieType)
    {
        if (!TryGetZombieConfig(zombieType, out var config))
        {
            throw new KeyNotFoundException($"Zombie configuration not found for zombie type: {zombieType}");
        }

        return config;
    }

    /// <inheritdoc/>
    internal sealed override void OnDeserialize()
    {
        _seedPacketLookup.Clear();
        _plantConfigLookup.Clear();
        _zombieConfigLookup.Clear();

        foreach (var plantConfig in PlantConfigs)
        {
            _plantConfigLookup[plantConfig.Type] = plantConfig;
            _seedPacketLookup[plantConfig.Type] = plantConfig;
        }

        foreach (var zombieConfig in ZombieConfigs)
        {
            _zombieConfigLookup[zombieConfig.Type] = zombieConfig;
            var seedType = PvZRUtils.ZombieTypeToIZombieSeedType(zombieConfig.Type);
            if (seedType != SeedType.None)
            {
                _seedPacketLookup[seedType] = zombieConfig;
            }
        }
    }

    /// <inheritdoc/>
    public void Serialize(PacketWriter packetWriter)
    {
        packetWriter.WriteInt(InitialSkyProductionRate);
        packetWriter.WriteInt(InitialProductionRateMax);
        packetWriter.WriteInt(InitialProductionRateMin);
        packetWriter.WriteInt(SkyProductionRate);
        packetWriter.WriteInt(PlantProductionRate);
        packetWriter.WriteInt(ZombieProductionRate);
        packetWriter.WriteInt(PlantShooterLaunchRateSurplus);

        packetWriter.WritePackedInt(IgnoreInitialCooldown.Count);
        foreach (var seedType in IgnoreInitialCooldown)
        {
            packetWriter.WriteEnum(seedType);
        }

        packetWriter.WritePackedInt(DisabledSeedPacketsInSuddenDeath.Count);
        foreach (var seedType in DisabledSeedPacketsInSuddenDeath)
        {
            packetWriter.WriteEnum(seedType);
        }

        CloudyDayArenaConfig.Serialize(packetWriter);

        packetWriter.WritePackedInt(PlantConfigs.Count);
        foreach (var config in PlantConfigs)
        {
            config.Serialize(packetWriter);
        }

        packetWriter.WritePackedInt(ZombieConfigs.Count);
        foreach (var config in ZombieConfigs)
        {
            config.Serialize(packetWriter);
        }
    }

    /// <inheritdoc/>
    public void Deserialize(PacketReader packetReader)
    {
        InitialSkyProductionRate = IntTime.FromGameValue(packetReader.ReadInt());
        InitialProductionRateMax = IntTime.FromGameValue(packetReader.ReadInt());
        InitialProductionRateMin = IntTime.FromGameValue(packetReader.ReadInt());
        SkyProductionRate = IntTime.FromGameValue(packetReader.ReadInt());
        PlantProductionRate = IntTime.FromGameValue(packetReader.ReadInt());
        ZombieProductionRate = IntTime.FromGameValue(packetReader.ReadInt());
        PlantShooterLaunchRateSurplus = packetReader.ReadInt();

        int ignoreCount = packetReader.ReadPackedInt();
        IgnoreInitialCooldown.Clear();
        for (int i = 0; i < ignoreCount; i++)
        {
            IgnoreInitialCooldown.Add(packetReader.ReadEnum<SeedType>());
        }

        int disabledCount = packetReader.ReadPackedInt();
        DisabledSeedPacketsInSuddenDeath.Clear();
        for (int i = 0; i < disabledCount; i++)
        {
            DisabledSeedPacketsInSuddenDeath.Add(packetReader.ReadEnum<SeedType>());
        }

        CloudyDayArenaConfig.Deserialize(packetReader);

        int seedPacketCount = packetReader.ReadPackedInt();
        PlantConfigs.Clear();
        for (int i = 0; i < seedPacketCount; i++)
        {
            var config = new PlantConfig();
            config.Deserialize(packetReader);
            PlantConfigs.Add(config);
        }

        int zombieCount = packetReader.ReadPackedInt();
        ZombieConfigs.Clear();
        for (int i = 0; i < zombieCount; i++)
        {
            var config = new ZombieConfig();
            config.Deserialize(packetReader);
            ZombieConfigs.Add(config);
        }

        OnDeserialize();
    }
}