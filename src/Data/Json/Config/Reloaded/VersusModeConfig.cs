using Il2CppReloaded.Gameplay;
using ReplantedOnline.Data.Json.Config.Reloaded.Arenas;
using ReplantedOnline.Data.Json.Converters;
using ReplantedOnline.Network.Reloaded.Serialization;
using ReplantedOnline.Structs;
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
    public List<SeedPacketConfig> SeedPacketConfigs { get; set; } = [];

    /// <summary>
    /// Gets the collection of zombie configurations defining health values for each zombie type.
    /// </summary>
    public List<ZombieConfig> ZombieConfigs { get; set; } = [];

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

        packetWriter.WritePackedInt(DisabledSeedPacketsInSuddenDeath.Count);
        foreach (var seedType in DisabledSeedPacketsInSuddenDeath)
        {
            packetWriter.WriteEnum(seedType);
        }

        CloudyDayArenaConfig.Serialize(packetWriter);

        packetWriter.WritePackedInt(SeedPacketConfigs.Count);
        foreach (var config in SeedPacketConfigs)
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

        int disabledCount = packetReader.ReadPackedInt();
        DisabledSeedPacketsInSuddenDeath.Clear();
        for (int i = 0; i < disabledCount; i++)
        {
            DisabledSeedPacketsInSuddenDeath.Add(packetReader.ReadEnum<SeedType>());
        }

        CloudyDayArenaConfig.Deserialize(packetReader);

        int seedPacketCount = packetReader.ReadPackedInt();
        SeedPacketConfigs.Clear();
        for (int i = 0; i < seedPacketCount; i++)
        {
            var config = new SeedPacketConfig();
            config.Deserialize(packetReader);
            SeedPacketConfigs.Add(config);
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