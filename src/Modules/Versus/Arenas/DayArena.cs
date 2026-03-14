using Il2CppReloaded.Gameplay;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Network.Client;

namespace ReplantedOnline.Modules.Versus.Arenas;

[RegisterArena]
internal sealed class DayArena : IArena
{
    /// <inheritdoc/>
    public ArenaTypes Type => ArenaTypes.Day;

    /// <inheritdoc/>
    public void SetupArena(VersusMode versusMode)
    {
        if (NetLobby.AmLobbyHost())
        {
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 0, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 1, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 2, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 3, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 4, false, true);

            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, SeedType.Sunflower, 0, 1, true);
            SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, SeedType.Sunflower, 0, 3, true);

            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 1, false, true);
            SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 3, false, true);
        }
    }

    /// <inheritdoc/>
    public void OnStart(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayStart(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void UpdateGameplay(VersusMode versusMode) { }

    /// <inheritdoc/>
    public void OnGameplayEnd(VersusMode versusMode, PlayerTeam winningTeam) { }
}
