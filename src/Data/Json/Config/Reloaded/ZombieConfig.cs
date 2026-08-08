using Il2CppReloaded.Gameplay;
using System.Text.Json.Serialization;

namespace ReplantedOnline.Data.Json.Config.Reloaded;

/// <summary>
/// Configuration data for a specific zombie type in the versus mode.
/// </summary>
internal sealed class ZombieConfig : JsonObject<ZombieConfig>
{
    /// <summary>
    /// Gets the type of zombie this configuration applies to.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ZombieType Type { get; init; }

    /// <summary>
    /// Gets the base health of the zombie's body.
    /// </summary>
    public int BodyHealth { get; init; }

    /// <summary>
    /// Gets the health of the zombie's armor (if any).
    /// </summary>
    public int ArmorHealth { get; init; }
}