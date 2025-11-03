using Il2CppReloaded.Gameplay;
using Il2CppTekly.PanelViews;
using Il2CppTMPro;
using ReplantedOnline.Items.Enums;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using ReplantedOnline.Patches.UI;
using ReplantedOnline.Patches.Versus.NetworkSync;

namespace ReplantedOnline.Managers;

/// <summary>
/// Static manager class responsible for handling versus mode
/// </summary>
internal static class VersusManager
{
    internal static void OnStart()
    {
        Instances.GameplayActivity.VersusMode.ClearBoard();
        if (VersusState.ZombieSide)
        {
            SeedPacketSyncPatch.SpawnZombie(ZombieType.Target, 8, 0, true);
            SeedPacketSyncPatch.SpawnZombie(ZombieType.Target, 8, 1, true);
            SeedPacketSyncPatch.SpawnZombie(ZombieType.Target, 8, 2, true);
            SeedPacketSyncPatch.SpawnZombie(ZombieType.Target, 8, 3, true);
            SeedPacketSyncPatch.SpawnZombie(ZombieType.Target, 8, 4, true);
        }
    }

    // UI text components for displaying player names on each team
    private static TextMeshProUGUI zombiePlayer1;
    private static TextMeshProUGUI zombiePlayer2;
    private static TextMeshProUGUI plantPlayer1;
    private static TextMeshProUGUI plantPlayer2;

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
        zombiePlayer2 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SideZombies/Selected/PlayerNumber2")?.GetComponentInChildren<TextMeshProUGUI>(true);

        // Find and cache the plant team player name text components
        plantPlayer1 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SidePlants/Selected/PlayerNumber1")?.GetComponentInChildren<TextMeshProUGUI>(true);
        plantPlayer2 = vsPanelView.transform.Find($"Canvas/Layout/Center/Panel/SidePlants/Selected/PlayerNumber2")?.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    /// <summary>
    /// Updates the versus side visuals based on the current game state and player roles.
    /// Assigns player names to appropriate positions on zombie and plant teams and manages button interactability.
    /// </summary>
    internal static void UpdateSideVisuals()
    {
        // Exit if any UI components are missing to prevent null reference exceptions
        if (zombiePlayer1 == null || zombiePlayer2 == null || plantPlayer1 == null || plantPlayer2 == null) return;

        // Clear all existing text before assigning new names
        ResetAllText();

        // Handle different game states for team assignment
        if (NetLobby.LobbyData.LastGameState == GameState.HostChooseZombie)
        {
            // When host chooses zombies, assign host to zombie team and client to plant team
            foreach (var client in NetLobby.LobbyData.AllClients.Values)
            {
                if (client.AmHost)
                {
                    // Host is assigned to the first zombie slot
                    zombiePlayer1.SetText(client.Name);
                }
                else
                {
                    // Client is assigned to the second plant slot
                    plantPlayer2.SetText(client.Name);
                }
            }
        }
        else if (NetLobby.LobbyData.LastGameState == GameState.HostChoosePlants)
        {
            // When host chooses plants, assign both players to plant team
            foreach (var client in NetLobby.LobbyData.AllClients.Values)
            {
                if (client.AmHost)
                {
                    // Host is assigned to the first plant slot
                    plantPlayer1.SetText(client.Name);
                }
                else
                {
                    // Client is assigned to the second zombie slot
                    zombiePlayer2.SetText(client.Name);
                }
            }
        }

        // Handle button interactability for the host player
        if (NetLobby.AmLobbyHost())
        {

#if DEBUG
            VsSideChoosererPatch.SetButtonsInteractable(true);
            return;
#endif

            // Enable buttons only when game is in progress (not in lobby) and there are at least 2 players
            if (NetLobby.LobbyData.LastGameState != GameState.Lobby && NetLobby.LobbyData.AllClients.Values.Count > 1)
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

    /// <summary>
    /// Resets all player name text fields to empty strings and ensures all UI elements are active.
    /// </summary>
    private static void ResetAllText()
    {
        // Safety check for null components
        if (zombiePlayer1 == null || zombiePlayer2 == null || plantPlayer1 == null || plantPlayer2 == null) return;

        // Ensure all text elements are visible
        zombiePlayer1.gameObject.SetActive(true);
        zombiePlayer2.gameObject.SetActive(true);
        plantPlayer1.gameObject.SetActive(true);
        plantPlayer2.gameObject.SetActive(true);

        // Clear all text content
        zombiePlayer1.SetText(string.Empty);
        zombiePlayer2.SetText(string.Empty);
        plantPlayer1.SetText(string.Empty);
        plantPlayer2.SetText(string.Empty);
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
                Instances.GameplayActivity.VersusMode.ZombiePlayerIndex = 0;
                Instances.GameplayActivity.VersusMode.PlantPlayerIndex = 1;
                versusData.UpdateZombiesPlayer("input1", "input1", 0);
            }
            else
            {
                Instances.GameplayActivity.VersusMode.ZombiePlayerIndex = 1;
                Instances.GameplayActivity.VersusMode.PlantPlayerIndex = 0;
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