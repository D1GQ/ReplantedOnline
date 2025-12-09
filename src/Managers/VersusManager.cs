using Il2CppReloaded.Gameplay;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using MelonLoader;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Object.Game;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Patches.UI;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ReplantedOnline.Managers;

/// <summary>
/// Static manager class responsible for handling versus mode
/// </summary>
internal static class VersusManager
{
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

        if (VersusState.PlantSide)
        {
            Utils.SpawnZombie(ZombieType.Target, 8, 0, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 1, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 2, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 3, false, true);
            Utils.SpawnZombie(ZombieType.Target, 8, 4, false, true);
        }
    }

    internal static void EndGame(GameObject focus, bool didPlantsWon)
    {
        if (focus == null)
        {
            MelonLogger.Error("Can not end game, Focus gameobject is null!");
            return;
        }

        if (didPlantsWon)
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.PlantsWin;
        }
        else
        {
            Instances.GameplayActivity.VersusMode.Phase = VersusPhase.ZombiesWin;
        }

        Instances.GameplayActivity.VersusMode.SetFocus(focus, Vector3.zero);
        Instances.GameplayActivity.Board.mCutScene.StartZombiesWon();
        MelonCoroutines.Start(CoEndGame());
    }

    private static IEnumerator CoEndGame()
    {
        yield return new WaitForSeconds(3f);
        if (NetLobby.AmInLobby())
        {
            NetLobby.ResetLobby();
        }
    }

    // UI text components for displaying player names on each team
    private static TextMeshProUGUI zombiePlayer1;
    private static TextMeshProUGUI zombiePlayer2;
    private static TextMeshProUGUI plantPlayer1;
    private static TextMeshProUGUI plantPlayer2;
    private static TextMeshProUGUI playerList;
    private static TextMeshProUGUI pickSides;

    private static EventTrigger lobbyCodeHeaderTrigger;
    private static string defaultHeaderText => $"Lobby Code: {NetLobby.LobbyData.LobbyCode}";
    private static bool copyingLobbyCode = false;

    /// <summary>
    /// Determines whether all required UI components are initialized and ready for use.
    /// </summary>
    internal static bool IsUIReady()
    {
        return zombiePlayer1 != null && zombiePlayer2 != null && plantPlayer1 != null && plantPlayer2 != null && playerList != null && pickSides != null;
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
        zombiePlayer1.enableAutoSizing = false;
        zombiePlayer1.fontSize = 100f;
        zombiePlayer2 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SideZombies/Selected/PlayerNumber2")?.GetComponentInChildren<TextMeshProUGUI>(true);
        zombiePlayer2.enableAutoSizing = false;
        zombiePlayer2.fontSize = 100f;

        // Find and cache the plant team player name text components
        plantPlayer1 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SidePlants/Selected/PlayerNumber1")?.GetComponentInChildren<TextMeshProUGUI>(true);
        plantPlayer1.enableAutoSizing = false;
        plantPlayer1.fontSize = 100f;
        plantPlayer2 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SidePlants/Selected/PlayerNumber2")?.GetComponentInChildren<TextMeshProUGUI>(true);
        plantPlayer2.enableAutoSizing = false;
        plantPlayer2.fontSize = 100f;

        playerList = UnityEngine.Object.Instantiate(plantPlayer1, vsPanelView.transform.Find($"Canvas/Layout/Center/Panel"));
        playerList.transform.localPosition = new Vector3(-15f, 0f, 0f);
        playerList.gameObject.name = "PlayerList";
        playerList.color = Color.white;

        pickSides = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/Header/HeaderLabel")?.GetComponentInChildren<TextMeshProUGUI>(true);

        // Add event trigger to header for copying the lobby code to clipboard
        lobbyCodeHeaderTrigger = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/Header").gameObject.AddComponent<EventTrigger>();
    }

    /// <summary>
    /// Updates the versus side visuals based on the current game state and player roles.
    /// Assigns player names to appropriate positions on zombie and plant teams and manages button interactability.
    /// </summary>
    internal static void UpdateSideVisuals()
    {
        // Clear all existing text before assigning new names
        ResetAllText();

        // Handle different game states for team assignment
        if (!NetLobby.LobbyData.Networked.PickingSides && !NetLobby.LobbyData.Networked.HostIsOnPlantSide)
        {
            // When host chooses zombies, assign host to zombie team and client to plant team
            foreach (var client in NetLobby.LobbyData.AllClients.Values)
            {
                if (client.AmHost)
                {
                    // Host is assigned to the first zombie slot
                    zombiePlayer1?.SetText(client.Name);
                }
                else
                {
                    // Client is assigned to the second plant slot
                    plantPlayer2?.SetText(client.Name);
                }
            }

            playerList?.SetText(string.Empty);
        }
        else if (!NetLobby.LobbyData.Networked.PickingSides && NetLobby.LobbyData.Networked.HostIsOnPlantSide)
        {
            // When host chooses plants, assign both players to plant team
            foreach (var client in NetLobby.LobbyData.AllClients.Values)
            {
                if (client.AmHost)
                {
                    // Host is assigned to the first plant slot
                    plantPlayer1?.SetText(client.Name);
                }
                else
                {
                    // Client is assigned to the second zombie slot
                    zombiePlayer2?.SetText(client.Name);
                }
            }

            playerList?.SetText(string.Empty);
        }
        else
        {
            string list = "-----------\n";
            foreach (var player in NetLobby.LobbyData.AllClients.Values)
            {
                string displayName = player.Name.Length > 10
                    ? string.Concat(player.Name.AsSpan(0, 10), "...")
                    : player.Name;
                list += $"{displayName}\n";
            }
            playerList?.SetText(list);
        }

        // Handle button interactability for the host player
        if (NetLobby.AmLobbyHost())
        {

#if DEBUG
            VsSideChoosererPatch.SetButtonsInteractable(true);
            return;
#endif

            // Enable buttons only when game is in progress (not in lobby) and there are at least 2 players
            if (NetLobby.LobbyData?.Networked != null)
            {
                if (NetLobby.LobbyData.Networked.PickingSides && NetLobby.LobbyData.AllClients.Values.Count > 1)
                {
                    // Allow host to interact with side selection buttons when conditions are met
                    VsSideChoosererPatch.SetButtonsInteractable(true);
                }
                else
                {
                    // Disable buttons when in lobby or with only one player
                    VsSideChoosererPatch.SetButtonsInteractable(false);
                }
            }
        }
    }

    /// <summary>
    /// Resets all player name text fields to empty strings and ensures all UI elements are active.
    /// </summary>
    private static void ResetAllText()
    {
        // Shows the lobby code in the header and resets the header UI events
        pickSides?.SetText(defaultHeaderText);
        UpdateHeaderEvents();

        // Ensure all text elements are visible
        zombiePlayer1?.gameObject.SetActive(true);
        zombiePlayer2?.gameObject.SetActive(true);
        plantPlayer1?.gameObject.SetActive(true);
        plantPlayer2?.gameObject.SetActive(true);

        // Clear all text content
        zombiePlayer1?.SetText(string.Empty);
        zombiePlayer2?.SetText(string.Empty);
        plantPlayer1?.SetText(string.Empty);
        plantPlayer2?.SetText(string.Empty);
    }

    /// <summary>
    /// Updates the header text to the current lobby code and resets the events.
    /// </summary>
    private static void UpdateHeaderEvents()
    {
        EventTrigger trigger = lobbyCodeHeaderTrigger.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.triggers = new Il2CppSystem.Collections.Generic.List<EventTrigger.Entry>();
    
            // On pointer enter trigger - modify header text
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            pointerEnter.callback.AddListener((UnityAction<BaseEventData>)((eventData) =>
            {
                if (!copyingLobbyCode) pickSides?.SetText($"Click to Copy");
            }));
            trigger.triggers.Add(pointerEnter);
    
            // On pointer exit trigger - reset header text
            EventTrigger.Entry pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener((UnityAction<BaseEventData>)((eventData) =>
            {
                if (!copyingLobbyCode) pickSides?.SetText(defaultHeaderText);
            }));
            trigger.triggers.Add(pointerExit);
    
            // On pointer click trigger - copy the lobby code to clipboard
            EventTrigger.Entry pointerClick = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
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
        pickSides?.SetText($"Copied to Clipboard!");

        yield return new WaitForSeconds(1f);

        pickSides?.SetText(defaultHeaderText);
        copyingLobbyCode = false;
    }

    /// <summary>
    /// Update player input mappings based on their assigned side (zombie or plant).
    /// </summary>
    /// <param name="amZombieSide">If to update to zombie side</param>
    internal static void UpdatePlayerInputs(bool amZombieSide)
    {
        ResetPlayerInputs();

        var versusData = Instances.VersusDataModel;
        if (versusData != null)
        {
            if (amZombieSide)
            {
                Instances.GameplayActivity.VersusMode.ZombiePlayerIndex = ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX;
                Instances.GameplayActivity.VersusMode.PlantPlayerIndex = ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX;
                versusData.UpdateZombiesPlayer("input1", "input1", 0);
            }
            else
            {
                Instances.GameplayActivity.VersusMode.ZombiePlayerIndex = ReplantedOnlineMod.Constants.OPPONENT_PLAYER_INDEX;
                Instances.GameplayActivity.VersusMode.PlantPlayerIndex = ReplantedOnlineMod.Constants.LOCAL_PLAYER_INDEX;
                versusData.UpdatePlantsPlayer("input1", "input1", 0);
            }
        }
    }

    /// <summary>
    /// reset player input mappings to default values.
    /// </summary>
    internal static void ResetPlayerInputs()
    {
        var versusData = Instances.VersusDataModel;
        if (versusData != null)
        {
            Instances.GameplayActivity.VersusMode.ZombiePlayerIndex = -1;
            Instances.GameplayActivity.VersusMode.PlantPlayerIndex = -1;
            versusData.UpdateZombiesPlayer("default", "input1", -1);
            versusData.UpdatePlantsPlayer("default", "input1", -1);
        }
    }
}
