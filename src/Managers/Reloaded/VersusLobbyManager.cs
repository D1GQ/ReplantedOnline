#pragma warning disable CS0162

using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using ReplantedOnline.Enums.Versus;
using ReplantedOnline.Modules.Modded.Instance;
using ReplantedOnline.Network.Client;
using ReplantedOnline.Network.Routing.Transport;
using ReplantedOnline.Patches.Reloaded.Gameplay.UI;
using ReplantedOnline.Utilities.Unity;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static Il2CppReloaded.Constants;

namespace ReplantedOnline.Managers.Reloaded;

/// <summary>
/// Static manager class responsible for handling versus mode in lonny
/// </summary>
internal static class VersusLobbyManager
{
    // UI text components for displaying player names on each team
    private static TextMeshProUGUI ZombiePlayer1;
    private static TextMeshProUGUI ZombiePlayer2;
    private static TextMeshProUGUI PlantPlayer1;
    private static TextMeshProUGUI PlantPlayer2;
    private static TextMeshProUGUI PlayerList;
    private static TextMeshProUGUI PickSides;

    private static EventTrigger LobbyCodeHeaderTrigger;
    private static string DefaultHeaderText => ReloadedLobby.NetworkTransport is LanTransport ?
        "LAN Mode" : $"Lobby Code: {ReloadedLobby.LobbyData?.LobbyCode ?? "???"}";
    private static bool CopyingLobbyCode = false;

    /// <summary>
    /// Determines whether all required UI components are initialized and ready for use.
    /// </summary>
    internal static bool IsUIReady()
    {
        return ZombiePlayer1 != null && ZombiePlayer2 != null &&
               PlantPlayer1 != null && PlantPlayer2 != null &&
               PlayerList != null && PickSides != null;
    }

    /// <summary>
    /// Initializes the text components for versus mode UI by finding them in the panel hierarchy.
    /// This method should be called when the versus panel is created to cache references to the UI elements.
    /// </summary>
    /// <param name="vsPanelView">The root panel view containing the versus mode UI elements</param>
    internal static void SetTextComps(PanelView vsPanelView)
    {
        if (IsUIReady()) return;

        // Find and cache the zombie team player name text components
        // Using GetComponentInChildren with includeInactive = true to find components even if parent objects are disabled
        ZombiePlayer1 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SideZombies/Selected/PlayerNumber1")?.GetComponentInChildren<TextMeshProUGUI>(true);
        ZombiePlayer1.gameObject.DestroyAllTextLocalizers();
        ZombiePlayer1.enableAutoSizing = false;
        ZombiePlayer1.fontSize = 100f;
        ZombiePlayer2 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SideZombies/Selected/PlayerNumber2")?.GetComponentInChildren<TextMeshProUGUI>(true);
        ZombiePlayer2.gameObject.DestroyAllTextLocalizers();
        ZombiePlayer2.enableAutoSizing = false;
        ZombiePlayer2.fontSize = 100f;

        // Find and cache the plant team player name text components
        PlantPlayer1 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SidePlants/Selected/PlayerNumber1")?.GetComponentInChildren<TextMeshProUGUI>(true);
        PlantPlayer1.gameObject.DestroyAllTextLocalizers();
        PlantPlayer1.enableAutoSizing = false;
        PlantPlayer1.fontSize = 100f;
        PlantPlayer2 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SidePlants/Selected/PlayerNumber2")?.GetComponentInChildren<TextMeshProUGUI>(true);
        PlantPlayer2.gameObject.DestroyAllTextLocalizers();
        PlantPlayer2.enableAutoSizing = false;
        PlantPlayer2.fontSize = 100f;

        PlayerList = UnityEngine.Object.Instantiate(PlantPlayer1, vsPanelView.transform.Find($"Canvas/Layout/Center/Panel"));
        PlayerList.gameObject.DestroyAllTextLocalizers();
        PlayerList.transform.localPosition = new Vector3(-15f, 0f, 0f);
        PlayerList.gameObject.name = "PlayerList";
        PlayerList.color = Color.white;

        PickSides = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/Header/HeaderLabel")?.GetComponentInChildren<TextMeshProUGUI>(true);
        PickSides.gameObject.DestroyAllTextLocalizers();

        // Add event trigger to header for copying the lobby code to clipboard
        LobbyCodeHeaderTrigger = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/Header").gameObject.AddComponent<EventTrigger>();
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
        foreach (var client in ReloadedLobby.LobbyData.AllClients.Values)
        {
            if (client.Team is PlayerTeam.Plants)
            {
                if (client.AmHost)
                {
                    PlantPlayer1?.SetText(client.Name);
                }
                else
                {
                    PlantPlayer2?.SetText(client.Name);
                }
            }
            else if (client.Team is PlayerTeam.Zombies)
            {
                if (client.AmHost)
                {
                    ZombiePlayer1?.SetText(client.Name);
                }
                else
                {
                    ZombiePlayer2?.SetText(client.Name);
                }
            }
        }

        // Player list
        PlayerList?.SetText(string.Empty);
        var notPlaying = ReloadedLobby.LobbyData.AllClients.Values.Where(client => client.Team is PlayerTeam.None or PlayerTeam.Spectators);
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

        PlayerList?.SetText(listBuilder.ToString());
    }

    /// <summary>
    /// Updates button interactability for the host player.
    /// </summary>
    private static void UpdateButtonInteractability()
    {
        if (!ReloadedLobby.AmLobbyHost())
            return;

        if (ModInfo.DEBUG)
        {
            VersusLobbyPatch.SetButtonsInteractable(true);
            return;
        }

        bool shouldEnableButtons = !ReloadedLobby.LobbyData.PickingSides.Value
            && ReloadedLobby.GetLobbyMemberCount() > 1
            && ReloadedLobby.LobbyData.AllClientsReady();

        VersusLobbyPatch.SetButtonsInteractable(shouldEnableButtons);
    }

    /// <summary>
    /// Resets all player name text fields to empty strings and ensures all UI elements are active.
    /// </summary>
    private static void ResetAllText()
    {
        // Shows the lobby code in the header and resets the header UI events
        PickSides?.SetText(DefaultHeaderText);
        UpdateHeaderEvents();

        // Clear all text content
        if (ZombiePlayer1 != null)
        {
            ZombiePlayer1.SetText(string.Empty);
            ZombiePlayer1.gameObject.SetActive(true);
        }
        if (ZombiePlayer2 != null)
        {
            ZombiePlayer2.SetText(string.Empty);
            ZombiePlayer2.gameObject.SetActive(true);
        }
        if (PlantPlayer1 != null)
        {
            PlantPlayer1.SetText(string.Empty);
            PlantPlayer1.gameObject.SetActive(true);
        }
        if (PlantPlayer2 != null)
        {
            PlantPlayer2.SetText(string.Empty);
            PlantPlayer2.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Updates the header text to the current lobby code and resets the events.
    /// </summary>
    private static void UpdateHeaderEvents()
    {
        if (ReloadedLobby.NetworkTransport is LanTransport) return;
        if (LobbyCodeHeaderTrigger == null) return;

        EventTrigger trigger = LobbyCodeHeaderTrigger.GetComponent<EventTrigger>();
        if (trigger != null)
        {
            trigger.triggers = new Il2CppSystem.Collections.Generic.List<EventTrigger.Entry>();

            // On pointer enter trigger - modify header text
            EventTrigger.Entry pointerEnter = new() { eventID = EventTriggerType.PointerEnter };
            pointerEnter.callback.AddListener((UnityAction<BaseEventData>)((eventData) =>
            {
                if (!CopyingLobbyCode)
                {
                    PickSides?.SetText($"Click to Copy");
                    Instances.GameplayActivity.SoundSystem.PlaySample(Sound.SOUND_BLEEP);
                }
            }));
            trigger.triggers.Add(pointerEnter);

            // On pointer exit trigger - reset header text
            EventTrigger.Entry pointerExit = new() { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener((UnityAction<BaseEventData>)((eventData) =>
            {
                if (!CopyingLobbyCode) PickSides?.SetText(DefaultHeaderText);
            }));
            trigger.triggers.Add(pointerExit);

            // On pointer click trigger - copy the lobby code to clipboard
            EventTrigger.Entry pointerClick = new() { eventID = EventTriggerType.PointerClick };
            pointerClick.callback.AddListener((UnityAction<BaseEventData>)((eventData) =>
            {
                if (!CopyingLobbyCode) Instances.GameplayActivity.StartCoroutine(CoCopyLobbyCode());
            }));
            trigger.triggers.Add(pointerClick);
        }
    }

    private static IEnumerator CoCopyLobbyCode()
    {
        CopyingLobbyCode = true;
        GUIUtility.systemCopyBuffer = ReloadedLobby.LobbyData.LobbyCode;
        Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_CHIME);
        PickSides?.SetText($"Copied to Clipboard!");

        yield return new WaitForSeconds(1f);

        PickSides?.SetText(DefaultHeaderText);
        CopyingLobbyCode = false;
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
                gameplayActivity.VersusMode.ZombiePlayerIndex = ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX;
                versusData.UpdateZombiesPlayer("input1", "input1", 0);
            }
            else if (team is PlayerTeam.Plants)
            {
                Instances.VersusDataModel.m_player1Model.m_isPlantsModel.Value = true;
                gameplayActivity.VersusMode.PlantPlayerIndex = ReplantedOnlineMod.Constants.Reloaded.LOCAL_PLAYER_INDEX;
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
        Instances.GameplayActivity?.VersusMode?.ZombiePlayerIndex = ReplantedOnlineMod.Constants.Reloaded.DEFAULT_PLAYER_INDEX;
        Instances.GameplayActivity?.VersusMode?.PlantPlayerIndex = ReplantedOnlineMod.Constants.Reloaded.DEFAULT_PLAYER_INDEX;
        Instances.VersusDataModel?.UpdateZombiesPlayer("default", "input1", ReplantedOnlineMod.Constants.Reloaded.DEFAULT_PLAYER_INDEX);
        Instances.VersusDataModel?.UpdatePlantsPlayer("default", "input1", ReplantedOnlineMod.Constants.Reloaded.DEFAULT_PLAYER_INDEX);
    }
}
