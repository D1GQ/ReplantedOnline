using Il2CppTMPro;
using MelonLoader;
using ReplantedOnline.Enums;
using ReplantedOnline.Helper;
using ReplantedOnline.Modules;
using ReplantedOnline.Network.Online;
using System.Collections;
using UnityEngine;
using static Il2CppReloaded.Constants;

namespace ReplantedOnline.Managers;

/// <summary>
/// Manages end-game logic and win screen display for versus matches.
/// </summary>
internal static class EndGameManager
{
    /// <summary>
    /// Ends the current game and initiates the end-game sequence.
    /// </summary>
    internal static void EndGame(PlayerTeam winningTeam)
    {
        // Might add win streaks later on, for now set to 0
        Instances.VersusDataModel.m_player1Model.m_winStreakModel.Value = 0;
        Instances.VersusDataModel.m_player2Model.m_winStreakModel.Value = 0;
        Instances.VersusDataModel.m_player1Model.m_hasWinStreakModel.Value = false;
        Instances.VersusDataModel.m_player2Model.m_hasWinStreakModel.Value = false;
        Instances.GameplayActivity.GameplayService.Player1VersusWinData = new();
        Instances.GameplayActivity.GameplayService.Player2VersusWinData = new();
        Instances.GameplayActivity.Player1VersusWinData = new();
        Instances.GameplayActivity.Player2VersusWinData = new();

        // Play winning or losing sound 
        foreach (var netClient in NetLobby.LobbyData.AllClients.Values)
        {
            if (netClient.AmLocal && netClient.Team is not PlayerTeam.Spectators)
            {
                if (netClient.Team == winningTeam)
                {
                    Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_WINMUSIC);
                }
                else
                {
                    Instances.GameplayActivity.m_audioService.PlaySample(Sound.SOUND_LOSEMUSIC);
                }
            }
        }

        MelonCoroutines.Start(CoEndGame(winningTeam));
    }

    /// <summary>
    /// Coroutine that handles the delayed end-game transition.
    /// </summary>
    /// <returns>IEnumerator for coroutine execution.</returns>
    private static IEnumerator CoEndGame(PlayerTeam winningTeam)
    {
        yield return new WaitForSeconds(3f);

        if (!NetLobby.AmInLobby())
        {
            yield break;
        }

        Instances.GameplayActivity.VersusMode.m_focusCircleController.gameObject.SetActive(false);
        Transitions.ToGameEnd(() =>
        {
            OnWinScreen(winningTeam);
        });
    }

    /// <summary>
    /// Sets up and displays the win screen with winner/loser information.
    /// </summary>
    private static void OnWinScreen(PlayerTeam winningTeam)
    {
        GameObject.Find("Panels/VersusPanels/P_VsWin/Canvas/Layout/Center/Buttons")?.SetActive(false);

        SetWinnerPanel(winningTeam);

        var winner = new NamePlate(1);
        var loser = new NamePlate(2);
        winner.Init();
        loser.Init();

        foreach (var netClient in NetLobby.LobbyData.AllClients.Values)
        {
            if (netClient.Team is not PlayerTeam.Spectators)
            {
                if (netClient.Team == winningTeam)
                {
                    winner.SetPortrait(netClient.Team);
                    winner.SetName(netClient.Name);
                }
                else
                {
                    loser.SetPortrait(Utils.GetOppositeTeam(winningTeam));
                    loser.SetName(netClient.Name);
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
    private static void SetWinnerPanel(PlayerTeam winningTeam)
    {
        var Plants = GameObject.Find("Panels/VersusPanels/P_VsWin/Canvas/Layout/Center/Winner").transform.Find("Plants");
        var Zombies = GameObject.Find("Panels/VersusPanels/P_VsWin/Canvas/Layout/Center/Winner").transform.Find("Zombies");
        if (Plants != null && Zombies != null)
        {
            Plants.gameObject.SetActive(winningTeam is PlayerTeam.Plants);
            Zombies.gameObject.SetActive(winningTeam is PlayerTeam.Zombies);
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
        /// Configures the Portrait visuals base of team.
        /// </summary>
        internal void SetPortrait(PlayerTeam team)
        {
            switch (team)
            {
                case PlayerTeam.Plants:
                    PlantFrame.SetActive(true);
                    PlantAvatar.SetActive(true);
                    ZombieFrame.SetActive(false);
                    ZombieAvatar.SetActive(false);
                    break;
                case PlayerTeam.Zombies:
                    ZombieFrame.SetActive(true);
                    ZombieAvatar.SetActive(true);
                    PlantFrame.SetActive(false);
                    PlantAvatar.SetActive(false);
                    break;
                default:
                    PlantFrame.SetActive(false);
                    PlantAvatar.SetActive(false);
                    ZombieFrame.SetActive(false);
                    ZombieAvatar.SetActive(false);
                    break;
            }
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