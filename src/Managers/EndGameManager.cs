using Il2CppTMPro;
using MelonLoader;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using System.Collections;
using UnityEngine;

namespace ReplantedOnline.Managers;

/// <summary>
/// Manages end-game logic and win screen display for versus matches.
/// </summary>
internal static class EndGameManager
{
    /// <summary>
    /// Ends the current game and initiates the end-game sequence.
    /// </summary>
    /// <param name="didPlantsWon">True if the plant team won, false if the zombie team won.</param>
    internal static void EndGame(bool didPlantsWon)
    {
        // Might add win streaks later on, for now set to 0
        Instances.GameplayDataProvider.m_gameplayDataModel.m_versusDataModel.m_player1Model.m_plantWinsModel.m_value = 0;
        Instances.GameplayDataProvider.m_gameplayDataModel.m_versusDataModel.m_player1Model.m_zombieWinsModel.m_value = 0;
        Instances.GameplayDataProvider.m_gameplayDataModel.m_versusDataModel.m_player2Model.m_plantWinsModel.m_value = 0;
        Instances.GameplayDataProvider.m_gameplayDataModel.m_versusDataModel.m_player2Model.m_zombieWinsModel.m_value = 0;
        Instances.GameplayActivity.GameplayService.Player1VersusWinData = new();
        Instances.GameplayActivity.GameplayService.Player2VersusWinData = new();
        Instances.GameplayActivity.Player1VersusWinData = new();
        Instances.GameplayActivity.Player2VersusWinData = new();

        MelonCoroutines.Start(CoEndGame(didPlantsWon));
    }

    /// <summary>
    /// Coroutine that handles the delayed end-game transition.
    /// </summary>
    /// <param name="didPlantsWon">True if the plant team won, false if the zombie team won.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private static IEnumerator CoEndGame(bool didPlantsWon)
    {
        yield return new WaitForSeconds(3f);

        if (!NetLobby.AmInLobby())
        {
            yield break;
        }

        Instances.GameplayActivity.VersusMode.m_focusCircleController.gameObject.SetActive(false);
        Transitions.ToGameEnd(() =>
        {
            OnWinScreen(didPlantsWon);
        });
    }

    /// <summary>
    /// Sets up and displays the win screen with winner/loser information.
    /// </summary>
    /// <param name="didPlantsWon">True if the plant team won, false if the zombie team won.</param>
    private static void OnWinScreen(bool didPlantsWon)
    {
        GameObject.Find("Panels/VersusPanels/P_VsWin/Canvas/Layout/Center/Buttons")?.SetActive(false);

        SetWinnerPanel(didPlantsWon);

        var winner = new NamePlate(1);
        var loser = new NamePlate(2);
        winner.Init();
        loser.Init();

        foreach (var netClient in NetLobby.LobbyData.AllClients.Values)
        {
            if (didPlantsWon)
            {
                if (netClient.AmPlantSide())
                {
                    winner.SetPlant();
                    winner.SetName(netClient.Name);
                }
                else
                {
                    loser.SetZombie();
                    loser.SetName(netClient.Name);
                }
            }
            else
            {
                if (netClient.AmPlantSide())
                {
                    loser.SetPlant();
                    loser.SetName(netClient.Name);
                }
                else
                {
                    winner.SetZombie();
                    winner.SetName(netClient.Name);
                }
            }
        }

        winner.DidWin();
        winner.Dispose();
        loser.Dispose();

        MelonCoroutines.Start(CoWinScreen());
    }

    /// <summary>
    /// Coroutine that handles the win screen display duration and lobby reset.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private static IEnumerator CoWinScreen()
    {
        yield return new WaitForSeconds(5f);

        if (!NetLobby.AmInLobby())
        {
            yield break;
        }

        if (NetLobby.AmLobbyHost())
        {
            NetLobby.LobbyData.Networked.ResetLobby();
        }
    }

    /// <summary>
    /// Sets the active winner panel based on which team won.
    /// </summary>
    /// <param name="didPlantsWon">True to show plant winner panel, false to show zombie winner panel.</param>
    private static void SetWinnerPanel(bool didPlantsWon)
    {
        var Plants = GameObject.Find("Panels/VersusPanels/P_VsWin/Canvas/Layout/Center/Winner").transform.Find("Plants");
        var Zombies = GameObject.Find("Panels/VersusPanels/P_VsWin/Canvas/Layout/Center/Winner").transform.Find("Zombies");
        if (Plants != null && Zombies != null)
        {
            if (didPlantsWon)
            {
                Plants.gameObject.SetActive(true);
                Zombies.gameObject.SetActive(false);
            }
            else
            {
                Zombies.gameObject.SetActive(true);
                Plants.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Represents a player nameplate on the win screen with team affiliation and win status.
    /// </summary>
    internal class NamePlate(int playerIndex) : IDisposable
    {
        /// <summary>
        /// The GameObject path for this nameplate.
        /// </summary>
        private readonly string path = $"Panels/VersusPanels/P_VsWin/Canvas/Layout/Center/NamePlates/Player{playerIndex}";

        /// <summary>
        /// Text display for the player's name.
        /// </summary>
        private TextMeshProUGUI NameDisplay;

        /// <summary>
        /// Plant team frame GameObject.
        /// </summary>
        private GameObject PlantFrame;

        /// <summary>
        /// Zombie team frame GameObject.
        /// </summary>
        private GameObject ZombieFrame;

        /// <summary>
        /// Plant avatar GameObject.
        /// </summary>
        private GameObject PlantAvatar;

        /// <summary>
        /// Zombie avatar GameObject.
        /// </summary>
        private GameObject ZombieAvatar;

        /// <summary>
        /// Winner trophy/indicator GameObject.
        /// </summary>
        private GameObject Winner;

        /// <summary>
        /// Initializes the nameplate by finding and caching all relevant GameObjects.
        /// </summary>
        internal void Init()
        {
            var player = GameObject.Find(path);
            NameDisplay = player.transform.Find("HeaderLabel").GetComponentInChildren<TextMeshProUGUI>();
            PlantFrame = player.transform.Find("PlantFrame").gameObject;
            ZombieFrame = player.transform.Find("ZombieFrame").gameObject;
            PlantAvatar = player.transform.Find("PlayerAvatar/PlantAvatar").gameObject;
            ZombieAvatar = player.transform.Find("PlayerAvatar/ZombieAvatar").gameObject;
            Winner = player.transform.Find("TrophyCount/Winner/Animator").gameObject;
            SetName("???");
        }

        /// <summary>
        /// Sets the displayed name on the nameplate.
        /// </summary>
        /// <param name="name">The name to display.</param>
        internal void SetName(string name)
        {
            NameDisplay.text = name;
        }

        /// <summary>
        /// Configures the nameplate to display plant team visuals.
        /// </summary>
        internal void SetPlant()
        {
            PlantFrame.SetActive(true);
            PlantAvatar.SetActive(true);
            ZombieFrame.SetActive(false);
            ZombieAvatar.SetActive(false);
        }

        /// <summary>
        /// Configures the nameplate to display zombie team visuals.
        /// </summary>
        internal void SetZombie()
        {
            ZombieFrame.SetActive(true);
            ZombieAvatar.SetActive(true);
            PlantFrame.SetActive(false);
            PlantAvatar.SetActive(false);
        }

        /// <summary>
        /// Activates the winner indicator on the nameplate.
        /// </summary>
        internal void DidWin()
        {
            Winner.SetActive(true);
        }

        /// <summary>
        /// Cleans up references to GameObjects.
        /// </summary>
        public void Dispose()
        {
            NameDisplay = null;
            PlantFrame = null;
            ZombieFrame = null;
            PlantAvatar = null;
            ZombieAvatar = null;
            Winner = null;
        }
    }
}