using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using Impostor.Game;
using Impostor.Steam;
using Impostor.Networking;

namespace Impostor.UI
{
    /// <summary>
    /// Voting interface for selecting which player to vote out as the Impostor.
    /// </summary>
    public class VoteUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject votePanel;
        [SerializeField] private Transform playerButtonContainer;
        [SerializeField] private GameObject playerVoteButtonPrefab;
        [SerializeField] private Button noVoteButton;
        [SerializeField] private TextMeshProUGUI votingStatusText;
        [SerializeField] private TextMeshProUGUI voteCountText;

        private Dictionary<CSteamID, GameObject> _voteButtons = new Dictionary<CSteamID, GameObject>();
        private CSteamID _selectedVote = CSteamID.Nil;
        private bool _hasVoted = false;

        private void Start()
        {
            InitializeUI();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }

            // Get VoteManager from GameManager
            VoteManager voteManager = null;
            if (GameManager.Instance != null)
            {
                voteManager = GameManager.Instance.VoteManager;
            }

            if (voteManager != null)
            {
                voteManager.OnVotingStarted += OnVotingStarted;
                voteManager.OnVoteCast += OnVoteCast;
                voteManager.OnVotingEnded += OnVotingEnded;
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.RegisterMessageHandler(
                    NetworkMessage.MessageType.VoteSubmitted,
                    HandleVoteSubmitted);
            }

            SetVotePanelActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
            }

            if (GameManager.Instance != null)
            {
                VoteManager voteManager = GameManager.Instance.VoteManager;
                if (voteManager != null)
                {
                    voteManager.OnVotingStarted -= OnVotingStarted;
                    voteManager.OnVoteCast -= OnVoteCast;
                    voteManager.OnVotingEnded -= OnVotingEnded;
                }
            }
        }

        private void InitializeUI()
        {
            if (noVoteButton != null)
            {
                noVoteButton.onClick.AddListener(() => CastVote(CSteamID.Nil));
            }
        }

        private void OnGameStateChanged(GameManager.GameState newState)
        {
            if (newState == GameManager.GameState.Voting)
            {
                SetVotePanelActive(true);
                CreateVoteButtons();
            }
            else
            {
                SetVotePanelActive(false);
            }
        }

        private void OnVotingStarted()
        {
            _hasVoted = false;
            _selectedVote = CSteamID.Nil;
            CreateVoteButtons();
            UpdateVotingStatus("Vote for who you think is the Impostor!");
        }

        private void OnVoteCast(CSteamID voterID, CSteamID targetID)
        {
            UpdateVoteCounts();
        }

        private void OnVotingEnded(CSteamID votedOut, bool wasImpostor)
        {
            UpdateVotingStatus($"Voting ended! {SteamLobbyManager.Instance.GetPlayerName(votedOut)} was voted out. (Impostor: {wasImpostor})");
            
            // Show results for a few seconds, then hide
            Invoke(nameof(HideVotePanel), 5f);
        }

        private void HandleVoteSubmitted(NetworkMessage message, CSteamID senderID)
        {
            if (message is VoteSubmittedMessage voteMsg)
            {
                UpdateVoteCounts();
            }
        }

        private void CreateVoteButtons()
        {
            // Clear existing buttons
            foreach (var button in _voteButtons.Values)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            _voteButtons.Clear();

            if (playerButtonContainer == null || playerVoteButtonPrefab == null)
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                return;
            }

            CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;

            // Create button for each player (except self)
            foreach (CSteamID playerID in GameManager.Instance.PlayerManager.AllPlayers)
            {
                if (playerID == localID)
                {
                    continue; // Can't vote for yourself
                }

                GameObject buttonObj = Instantiate(playerVoteButtonPrefab, playerButtonContainer);
                _voteButtons[playerID] = buttonObj;

                // Set player name
                TextMeshProUGUI nameText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = SteamLobbyManager.Instance.GetPlayerName(playerID);
                }

                // Set button click
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    CSteamID targetID = playerID; // Capture for closure
                    button.onClick.AddListener(() => CastVote(targetID));
                }
            }
        }

        private void CastVote(CSteamID targetID)
        {
            if (_hasVoted)
            {
                return;
            }

            _selectedVote = targetID;
            _hasVoted = true;

            if (GameManager.Instance == null)
            {
                return;
            }

            VoteManager voteManager = GameManager.Instance.VoteManager;
            if (voteManager == null)
            {
                return;
            }

            CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;

            if (GameManager.Instance.IsHost)
            {
                voteManager.CastVote(localID, targetID);
            }
            else
            {
                VoteSubmittedMessage message = new VoteSubmittedMessage
                {
                    VoterSteamID = localID.m_SteamID,
                    VotedForSteamID = targetID.m_SteamID
                };
                NetworkManager.Instance.SendMessage(message, GetHostSteamID());
            }

            UpdateVotingStatus("Vote submitted!");
            SetButtonsInteractable(false);
        }

        private void UpdateVoteCounts()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            VoteManager voteManager = GameManager.Instance.VoteManager;
            if (voteManager == null)
            {
                return;
            }

            var voteCounts = voteManager.GetVoteCounts();
            string voteText = "Votes: ";

            foreach (var kvp in voteCounts)
            {
                string playerName = SteamLobbyManager.Instance.GetPlayerName(kvp.Key);
                voteText += $"{playerName}: {kvp.Value} ";
            }

            if (voteCountText != null)
            {
                voteCountText.text = voteText;
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            foreach (var buttonObj in _voteButtons.Values)
            {
                if (buttonObj != null)
                {
                    Button button = buttonObj.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = interactable;
                    }
                }
            }

            if (noVoteButton != null)
            {
                noVoteButton.interactable = interactable;
            }
        }

        private void SetVotePanelActive(bool active)
        {
            if (votePanel != null)
            {
                votePanel.SetActive(active);
            }
        }

        private void HideVotePanel()
        {
            SetVotePanelActive(false);
        }

        private void UpdateVotingStatus(string message)
        {
            if (votingStatusText != null)
            {
                votingStatusText.text = message;
            }
        }

        private CSteamID GetHostSteamID()
        {
            if (SteamLobbyManager.Instance != null && SteamLobbyManager.Instance.IsInLobby)
            {
                return SteamLobbyManager.Instance.CurrentLobbyID;
            }
            return CSteamID.Nil;
        }
    }
}

