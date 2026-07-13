using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes.Register;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;

namespace ReplantedOnline.Modules.Reloaded.Versus.Configs.Plant;

[RegisterPlantConfig(SeedType.Tanglekelp)]
internal sealed class TanglekelpPlantConfig : IPlantConfig
{
    /// <inheritdoc/>
    public void SetArenaDefinition(PlantDefinition plantDefinition, ArenaTypes arena) { }

    /// <inheritdoc/>
    public bool IsAllowedInArena(ArenaTypes arena)
    {
        return arena is ArenaTypes.Pool or ArenaTypes.PoolNight;
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(ArenaTypes arena, int gridX, int gridY) => true;

    /// <inheritdoc/>
    public void OnPlanted(Il2CppReloaded.Gameplay.Plant plant, int gridX, int gridY) { }
}
