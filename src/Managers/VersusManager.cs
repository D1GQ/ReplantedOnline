using Il2CppReloaded.Gameplay;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Patches.Gameplay.UI;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static Il2CppReloaded.Constants;

namespace ReplantedOnline.Managers;

/// <summary>
/// Static manager class responsible for handling versus mode
/// </summary>
internal static class VersusManager
{
    // UI text components for displaying player names on each team
    private static TextMeshProUGUI zombiePlayer1;
    private static TextMeshProUGUI zombiePlayer2;
    private static TextMeshProUGUI plantPlayer1;
    private static TextMeshProUGUI plantPlayer2;
    private static TextMeshProUGUI playerList;
    private static TextMeshProUGUI pickSides;

    private static EventTrigger lobbyCodeHeaderTrigger;
    private static string DefaultHeaderText => $"Lobby Code: {NetLobby.LobbyData?.LobbyCode ?? "???"}";
    private static bool copyingLobbyCode = false;

    /// <summary>
    /// Determines whether all required UI components are initialized and ready for use.
    /// </summary>
    internal static bool IsUIReady()
    {
        return zombiePlayer1 != null && zombiePlayer2 != null &&
               plantPlayer1 != null && plantPlayer2 != null &&
               playerList != null && pickSides != null;
    }

    /// <summary>
    /// Initializes the text components for versus mode UI by finding them in the panel hierarchy.
    /// This method should be called when the versus panel is created to cache references to the UI elements.
    /// </summary>
    /// <param name="vsPanelView">The root panel view containing the versus mode UI elements</param>
    internal static void SetTextComps(PanelView vsPanelView)
    {
        // Find and cache the zombie team player name text components
        // Using GetComponentInChildren with includeInactive = true to find components even if parent objects are disabled
        zombiePlayer1 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SideZombies/Selected/PlayerNumber1")?.GetComponentInChildren<TextMeshProUGUI>(true);
        zombiePlayer1.gameObject.DestroyAllTextLocalizers();
        zombiePlayer1.enableAutoSizing = false;
        zombiePlayer1.fontSize = 100f;
        zombiePlayer2 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SideZombies/Selected/PlayerNumber2")?.GetComponentInChildren<TextMeshProUGUI>(true);
        zombiePlayer2.gameObject.DestroyAllTextLocalizers();
        zombiePlayer2.enableAutoSizing = false;
        zombiePlayer2.fontSize = 100f;

        // Find and cache the plant team player name text components
        plantPlayer1 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SidePlants/Selected/PlayerNumber1")?.GetComponentInChildren<TextMeshProUGUI>(true);
        plantPlayer1.gameObject.DestroyAllTextLocalizers();
        plantPlayer1.enableAutoSizing = false;
        plantPlayer1.fontSize = 100f;
        plantPlayer2 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SidePlants/Selected/PlayerNumber2")?.GetComponentInChildren<TextMeshProUGUI>(true);
        plantPlayer2.gameObject.DestroyAllTextLocalizers();
        plantPlayer2.enableAutoSizing = false;
        plantPlayer2.fontSize = 100f;

        playerList = UnityEngine.Object.Instantiate(plantPlayer1, vsPanelView.transform.Find($"Canvas/Layout/Center/Panel"));
        playerList.gameObject.DestroyAllTextLocalizers();
        playerList.transform.localPosition = new Vector3(-15f, 0f, 0f);
        playerList.gameObject.name = "PlayerList";
        playerList.color = Color.white;

        pickSides = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/Header/HeaderLabel")?.GetComponentInChildren<TextMeshProUGUI>(true);
        pickSides.gameObject.DestroyAllTextLocalizers();

        // Add event trigger to header for copying the lobby code to clipboard
        lobbyCodeHeaderTrigger = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/Header").gameObject.AddComponent<EventTrigger>();
    }

    /// <summary>
    /// Updates the versus side visuals based on game state and player roles.
    /// Assigns player names to zombie/plant teams and manages button interactability.
    /// </summary>
    internal static void UpdateSideVisuals()
    {
        // Clear all text fields before assignment
        ResetAllText();
        SetNamesFromTeams();
        UpdateButtonInteractability();
    }

    /// <summary>
    /// Assigns player names to teams and player list.
    /// </summary>
    private static void SetNamesFromTeams()
    {
        foreach (var client in NetLobby.LobbyData.AllClients.Values)
        {
            if (client.Team is PlayerTeam.Plants)
            {
                if (client.AmHost)
                {
                    plantPlayer1?.SetText(client.Name);
                }
                else
                {
                    plantPlayer2?.SetText(client.Name);
                }
            }
            else if (client.Team is PlayerTeam.Zombies)
            {
                if (client.AmHost)
                {
                    zombiePlayer1?.SetText(client.Name);
                }
                else
                {
                    zombiePlayer2?.SetText(client.Name);
                }
            }
        }

        // Player list
        playerList?.SetText(string.Empty);
        var notPlaying = NetLobby.LobbyData.AllClients.Values.Where(client => client.Team is PlayerTeam.None or PlayerTeam.Spectators);
        if (!notPlaying.Any()) return;

        const int MAX_NAME_LENGTH = 10;
        const string ELLIPSIS = "...";

        var listBuilder = new StringBuilder("-----------\n");

        foreach (var client in notPlaying)
        {
            string displayName = client.Name.Length > MAX_NAME_LENGTH
                ? string.Concat(client.Name.AsSpan(0, MAX_NAME_LENGTH), ELLIPSIS)
                : client.Name;

            listBuilder.Append(displayName).AppendLine();
        }

        playerList?.SetText(listBuilder.ToString());
    }

    /// <summary>
    /// Updates button interactability for the host player.
    /// </summary>
    private static void UpdateButtonInteractability()
    {
        if (!NetLobby.AmLobbyHost())
            return;

#if DEBUG
        VersusLobbyPatch.SetButtonsInteractable(true);
        return;
#endif

        var networked = NetLobby.LobbyData?.Networked;
        var clients = NetLobby.LobbyData?.AllClients.Values;

        bool shouldEnableButtons = networked != null
            && !networked.PickingSides
            && clients?.Count > 1
            && NetLobby.LobbyData.AllClientsReady();

        VersusLobbyPatch.SetButtonsInteractable(shouldEnableButtons);
    }

    /// <summary>
    /// Resets all player name text fields to empty strings and ensures all UI elements are active.
    /// </summary>
    private static void ResetAllText()
    {
        // Shows the lobby code in the header and resets the header UI events
        pickSides?.SetText(DefaultHeaderText);
        UpdateHeaderEvents();

        // Clear all text content
        if (zombiePlayer1 != null)
        {
            zombiePlayer1.SetText(string.Empty);
            zombiePlayer1.gameObject.SetActive(true);
        }
        if (zombiePlayer2 != null)
        {
            zombiePlayer2.SetText(string.Empty);
            zombiePlayer2.gameObject.SetActive(true);
        }
        if (plantPlayer1 != null)
        {
            plantPlayer1.SetText(string.Empty);
            plantPlayer1.gameObject.SetActive(true);
        }
        if (plantPlayer2 != null)
        {
            plantPlayer2.SetText(string.Empty);
            plantPlayer2.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Updates the header text to the current lobby code and resets the events.
    /// </summary>
    private static void UpdateHeaderEvents()
    {
        if (lobbyCodeHeaderTrigger == null) return;

        EventTrigger trigger = lobbyCodeHeaderTrigger.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.triggers = new Il2CppSystem.Collections.Generic.List<EventTrigger.Entry>();

            // On pointer enter trigger - modify header text
            EventTrigger.Entry pointerEnter = new() { eventID = EventTriggerType.PointerEnter };
            pointerEnter.callback.AddListener((UnityAction<BaseEventData>)((eventData) =>
            {
                if (!copyingLobbyCode) pickSides?.SetText($"Click to Copy");
            }));
            trigger.triggers.Add(pointerEnter);

            // On pointer exit trigger - reset header text
            EventTrigger.Entry pointerExit = new() { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener((UnityAction<BaseEventData>)((eventData) =>
            {
                if (!copyingLobbyCode) pickSides?.SetText(DefaultHeaderText);
            }));
            trigger.triggers.Add(pointerExit);

            // On pointer click trigger - copy the lobby code to clipboard
            EventTrigger.Entry pointerClick = new() { eventID = EventTriggerType.PointerClick };
            pointerClick.callback.AddListener((UnityAction<BaseEventData>)((eventData) =>
            {
                if (!copyingLobbyCode) MelonCoroutines.Start(CoCopyLobbyCode());
            }));
            trigger.triggers.Add(pointerClick);
        }
    }

    private static IEnumerator CoCopyLobbyCode()
    {
        copyingLobbyCode = true;
        GUIUtility.systemCopyBuffer = NetLobby.LobbyData.LobbyCode;
        Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_CHIME);
        pickSides?.SetText($"Copied to Clipboard!");

        yield return new WaitForSeconds(1f);

        pickSides?.SetText(DefaultHeaderText);
        copyingLobbyCode = false;
    }

    /// <summary>
    /// Set player input mappings based on their assigned side (zombie or plant).
    /// </summary>
    internal static void SetPlayerInput(PlayerTeam team)
    {
        ResetPlayerInput();

        var versusData = Instances.VersusDataModel;
        var gameplayActivity = Instances.GameplayActivity;
        if (versusData != null && gameplayActivity != null)
        {
            if (team is PlayerTeam.Zombies)
            {
                Instances.VersusDataModel.m_player1Model.m_isZombiesModel.Value = true;
                gameplayActivity.VersusMode.ZombiePlayerIndex = ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX;
                versusData.UpdateZombiesPlayer("input1", "input1", 0);
            }
            else if (team is PlayerTeam.Plants)
            {
                Instances.VersusDataModel.m_player1Model.m_isPlantsModel.Value = true;
                gameplayActivity.VersusMode.PlantPlayerIndex = ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX;
                versusData.UpdatePlantsPlayer("input1", "input1", 0);
            }
        }
    }

    /// <summary>
    /// reset player input mappings to default values.
    /// </summary>
    internal static void ResetPlayerInput()
    {
        Instances.VersusDataModel?.m_player1Model?.m_isZombiesModel?.Value = false;
        Instances.VersusDataModel?.m_player1Model?.m_isPlantsModel?.Value = false;
        Instances.GameplayActivity?.VersusMode?.ZombiePlayerIndex = ReplantedOnlineMod.Constants.DEFAULT_PLAYER_INDEX;
        Instances.GameplayActivity?.VersusMode?.PlantPlayerIndex = ReplantedOnlineMod.Constants.DEFAULT_PLAYER_INDEX;
        Instances.VersusDataModel?.UpdateZombiesPlayer("default", "input1", ReplantedOnlineMod.Constants.DEFAULT_PLAYER_INDEX);
        Instances.VersusDataModel?.UpdatePlantsPlayer("default", "input1", ReplantedOnlineMod.Constants.DEFAULT_PLAYER_INDEX);
    }

    internal static void OnStart()
    {
        VersusHudPatch.SetHuds();

        // Despawn real target zombies so they can spawn on the network
        foreach (var kvp in Instances.GameplayActivity.Board.m_zombies.m_itemLookup)
        {
            var zombie = kvp.Key;
            if (zombie.mZombieType == ZombieType.Target)
            {
                if (zombie.GetNetworked<ZombieNetworked>() == null)
                {
                    zombie.DieDeserialize();
                }
            }
        }

        if (NetLobby.AmLobbyHost())
        {
            Utils.SpawnZombie(ZombieType.Target, 8, 0, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 1, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 2, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 3, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 4, false, true);
        }

        var allSeedPackets = new List<SeedPacket>();
        allSeedPackets.AddRange(Instances.GameplayActivity.Board.SeedBanks.LocalItem().SeedPackets);
        allSeedPackets.AddRange(Instances.GameplayActivity.Board.SeedBanks.OpponentItem().SeedPackets);

        // Initial 10 second cooldown plus base cooldown
        foreach (var seedPacket in allSeedPackets)
        {
            if (seedPacket.mPacketType is SeedType.Sunflower or SeedType.ZombieGravestone) continue;

            seedPacket.Deactivate();
            seedPacket.mRefreshTime = 1200 + Instances.DataServiceActivity.Service.GetPlantDefinition(seedPacket.mPacketType).m_versusBaseRefreshTime;
            seedPacket.mRefreshing = true;
        }
    }

    internal static void EndGame(GameObject focus, PlayerTeam winningTeam)
    {
        if (focus == null)
        {
            MelonLogger.Error("Can not end game, Focus gameobject is null!");
            return;
        }

        if (winningTeam is PlayerTeam.Plants)
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.PlantsWin;
        }
        else
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ZombiesWin;
        }

        Instances.GameplayActivity.VersusMode.SetFocus(focus, Vector3.zero);
        Instances.GameplayActivity.m_audioService.StopAllMusic();
        Instances.GameplayActivity.Board.Pause(true);
        EndGameManager.EndGame(winningTeam);
    }

    /// <summary>
    /// Calculates the new brain spawn counter.
    /// </summary>
    /// <param name="currentCounter">The current brain spawn counter value.</param>
    internal static int MultiplyBrainSpawnCounter(int currentCounter)
    {
        int plantMultiplier = 25 * Instances.GameplayActivity.Board.m_plants.m_itemLookup.Keys.Count;
        return currentCounter + plantMultiplier;
    }

    /// <summary>
    /// Calculates the new grave counter.
    /// </summary>
    /// <param name="currentCounter">The current grave counter value.</param>
    internal static int MultiplyGraveCounter(int currentCounter)
    {
        int zombieMultiplier = 0;
        foreach (var zombie in Instances.GameplayActivity.Board.m_zombies.m_itemLookup.Keys)
        {
            zombieMultiplier += zombie.mZombieType switch
            {
                ZombieType.Target => 300,
                ZombieType.Gargantuar => 250,
                ZombieType.Gravestone => 100,
                ZombieType.Zamboni => 50,
                ZombieType.Zombatar => 50,
                ZombieType.Catapult => 30,
                ZombieType.Football => 30,
                ZombieType.Dancer => 25,
                ZombieType.Pogo => 15,
                ZombieType.Pail => 15,
                ZombieType.Polevaulter => 15,
                ZombieType.BackupDancer => 0,
                _ => 10,
            };
        }

        int plantMultiplier = 5 * Instances.GameplayActivity.Board.m_plants.m_itemLookup.Keys.Count;

        return Mathf.FloorToInt((currentCounter * 0.8f)) + zombieMultiplier - plantMultiplier;
    }
}
