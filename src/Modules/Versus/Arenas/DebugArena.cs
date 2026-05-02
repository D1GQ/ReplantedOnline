#if DEBUG
using Il2CppReloaded.Data;
using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using ReplantedOnline.Attributes;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Interfaces.Versus;
using ReplantedOnline.Managers;
using ReplantedOnline.Modules.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Utilities;
using UnityEngine.InputSystem;

namespace ReplantedOnline.Modules.Versus.Arenas;

[RegisterArena]
internal sealed class DebugArena : IArena, IArenaData
{
    private enum DebugModes
    {
        Default,
        Gargantuar,
        Pogo,
        Bobsled,
        Polevaulter,
        Ladder,
        Bungee,
        Flag,
        Test
    }

    private DebugModes Mode = DebugModes.Default;

    /// <inheritdoc/>
    public ArenaTypes Type => ArenaTypes.Debug;

    /// <inheritdoc/>
    public MusicTune Music => MusicTune.None;

    /// <inheritdoc/>
    public SpawnType DefaultZombieSpawnType => SpawnType.RiseFromGround;

    /// <inheritdoc/>
    public LevelEntryData GetLevelEntryData()
    {
        return LevelEntries.GetLevel("Level-Minigame-Beghouled");
    }

    /// <inheritdoc/>
    public void SetupVersusLevel(LevelEntryData versusLevelData)
    {
        versusLevelData.m_gameArea = GameArea.Day;
        versusLevelData.m_backgroundPrefab = GetLevelEntryData().m_backgroundPrefab;
    }

    /// <inheritdoc/>
    public void InitializeArena(VersusMode versusMode)
    {
        Mode = DebugModes.Default;
        SetMode();
    }

    /// <inheritdoc/>
    public void InitializeSeedPacketCooldowns(SeedPacket[] seedPackets)
    {
    }

    /// <inheritdoc/>
    public void UpdateArena(VersusMode versusMode)
    {
        if (ReplantedLobby.AmLobbyHost())
        {
            if (VersusState.IsInGameplay && !Instances.GameplayActivity.Board.mPaused)
            {
                if (Keyboard.current.minusKey.wasPressedThisFrame)
                {
                    VersusGameplayManager.EndGame(Instances.GameplayActivity.m_boardOffset.position, PlayerTeam.Plants);
                }

                if (Keyboard.current.equalsKey.wasPressedThisFrame)
                {
                    VersusGameplayManager.EndGame(Instances.GameplayActivity.m_boardOffset.position, PlayerTeam.Zombies);
                }
            }
        }

        if (!VersusState.AmPlantSide) return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            Mode = Mode.Next();
            SetMode();
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            Mode = Mode.Previous();
            SetMode();
        }

        if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            SetMode();
        }
    }

    /// <inheritdoc/>
    public bool CanBePlacedAt(SeedType seedType, int gridX, int gridY) => true;

    private void SetMode()
    {
        if (!VersusState.AmPlantSide) return;

        foreach (var networkobject in ReplantedLobby.LobbyData.NetworkObjectsSpawned.Values)
        {
            networkobject.DespawnAndDestroy();
        }

        switch (Mode)
        {
            case DebugModes.Default:
                SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 0, true);
                SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 1, true);
                SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 2, true);
                SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 3, true);
                SeedPacketDefinitions.SpawnZombie(ZombieType.Target, 8, 4, true);

                SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 1, true);
                SeedPacketDefinitions.SpawnZombie(ZombieType.Gravestone, 8, 3, true);

                SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 1, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 0, 3, true);
                break;
            case DebugModes.Gargantuar:
                SeedPacketDefinitions.SpawnZombie(ZombieType.Gargantuar, 8, 1, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Gatlingpea, 0, 1, true);

                SeedPacketDefinitions.SpawnZombie(ZombieType.Gargantuar, 8, 3, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 0, 3, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 1, 3, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 2, 3, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 3, 3, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 4, 3, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 5, 3, true);
                break;
            case DebugModes.Pogo:
                SeedPacketDefinitions.SpawnZombie(ZombieType.Pogo, 7, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 0, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 1, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 2, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 3, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 4, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 5, 2, true);
                break;
            case DebugModes.Bobsled:
                SeedPacketDefinitions.SpawnZombie(ZombieType.Zamboni, 8, 2, true);
                break;
            case DebugModes.Polevaulter:
                SeedPacketDefinitions.SpawnZombie(ZombieType.Polevaulter, 7, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 4, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 5, 2, true);
                break;
            case DebugModes.Ladder:
                SeedPacketDefinitions.SpawnZombie(ZombieType.Ladder, 7, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 4, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Wallnut, 5, 2, true);
                break;
            case DebugModes.Bungee:
                SeedPacketDefinitions.SpawnZombie(ZombieType.Bungee, 4, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Umbrella, 4, 2, true);
                break;
            case DebugModes.Flag:
                SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 1, 0, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 2, 2, true);
                SeedPacketDefinitions.SpawnPlant(SeedType.Sunflower, 3, 4, true);
                SeedPacketDefinitions.SpawnZombie(ZombieType.Flag, 9, 2, true);
                break;
            case DebugModes.Test:
                break;
            default:
                break;
        }
    }
}
#endif