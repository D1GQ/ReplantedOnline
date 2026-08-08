namespace ReplantedOnline.Data.Json.Config;

internal sealed class VersusModeConfig : JsonObject<VersusModeConfig>
{
    public List<SeedPacketConfig> SeedPacketConfigs { get; init; } = [];
    public List<ZombieConfig> ZombieConfigs { get; init; } = [];
}
