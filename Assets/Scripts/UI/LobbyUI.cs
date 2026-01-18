using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using Impostor.Steam;
using Impostor.Game;
using Impostor.Networking;

namespace Impostor.UI
{
    /// <summary>
    /// Lobby UI showing connected players, ready states, and game start functionality.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform playerListContainer;
        [SerializeField] private GameObject playerSlotPrefab;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button leaveLobbyButton;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TextMeshProUGUI lobbyInfoText;
        [SerializeField] private TextMeshProUGUI statusText;

        private Dictionary<CSteamID, GameObject> _playerSlots = new Dictionary<CSteamID, GameObject>();
        private bool _isReady = false;
        private bool _isHost = false;

        private void Start()
        {
            InitializeUI();

            // Check if we're the host
            if (SteamLobbyManager.Instance != null)
            {
                _isHost = SteamLobbyManager.Instance.IsLobbyOwner();
                UpdateLobbyInfo();

                SteamLobbyManager.Instance.OnPlayerJoined += OnPlayerJoined;
                SteamLobbyManager.Instance.OnPlayerLeft += OnPlayerLeft;
                
                // Sync existing lobby members with GameManager
                SyncLobbyMembersToGameManager();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetAsHost(_isHost);
                GameManager.Instance.ChangeState(GameManager.GameState.Lobby);
            }

            RefreshPlayerList();
        }
        
        private void SyncLobbyMembersToGameManager()
        {
            if (SteamLobbyManager.Instance == null || GameManager.Instance == null)
                return;
                
            if (!SteamLobbyManager.Instance.IsInLobby)
                return;
                
            // Add all existing lobby members to GameManager
            List<CSteamID> members = SteamLobbyManager.Instance.LobbyMembers;
            foreach (CSteamID memberID in members)
            {
                if (!GameManager.Instance.PlayerManager.HasPlayer(memberID))
                {
                    string playerName = SteamLobbyManager.Instance.GetPlayerName(memberID);
                    GameManager.Instance.PlayerManager.AddPlayer(memberID, playerName);
                }
            }
        }

        private void OnDestroy()
        {
            if (SteamLobbyManager.Instance != null)
            {
                SteamLobbyManager.Instance.OnPlayerJoined -= OnPlayerJoined;
                SteamLobbyManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            }
        }

        private void InitializeUI()
        {
            if (readyButton != null)
            {
                readyButton.onClick.AddListener(ToggleReady);
            }

            if (leaveLobbyButton != null)
            {
                leaveLobbyButton.onClick.AddListener(LeaveLobby);
            }

            if (startGameButton != null)
            {
                startGameButton.onClick.AddListener(StartGame);
                startGameButton.interactable = false; // Only host can start
            }

            UpdateReadyButton();
            UpdateStartButton();
        }

        private void UpdateLobbyInfo()
        {
            if (lobbyInfoText != null)
            {
                string hostText = _isHost ? " (Host)" : "";
                lobbyInfoText.text = $"Lobby{hostText}";
            }
        }

        private void RefreshPlayerList()
        {
            // Clear existing slots
            foreach (var slot in _playerSlots.Values)
            {
                if (slot != null)
                {
                    Destroy(slot);
                }
            }
            _playerSlots.Clear();

            if (SteamLobbyManager.Instance == null || !SteamLobbyManager.Instance.IsInLobby)
            {
                return;
            }

            // Add all lobby members
            List<CSteamID> members = SteamLobbyManager.Instance.LobbyMembers;
            foreach (CSteamID memberID in members)
            {
                AddPlayerSlot(memberID);
            }

            UpdateStatus($"Players: {members.Count}/6");
        }

        private void AddPlayerSlot(CSteamID steamID)
        {
            if (_playerSlots.ContainsKey(steamID))
            {
                return;
            }

            if (playerSlotPrefab == null || playerListContainer == null)
            {
                Debug.LogWarning("Player slot prefab or container not assigned");
                return;
            }

            GameObject slot = Instantiate(playerSlotPrefab, playerListContainer);
            _playerSlots[steamID] = slot;

            // Update slot UI
            UpdatePlayerSlot(slot, steamID);
        }

        private void UpdatePlayerSlot(GameObject slot, CSteamID steamID)
        {
            TextMeshProUGUI nameText = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
            {
                string playerName = SteamLobbyManager.Instance.GetPlayerName(steamID);
                if (steamID == Impostor.Steam.SteamManager.Instance.LocalSteamID)
                {
                    playerName += " (You)";
                }
                nameText.text = playerName;
            }

            // Update ready indicator
            Image readyIndicator = slot.transform.Find("ReadyIndicator")?.GetComponent<Image>();
            if (readyIndicator != null && GameManager.Instance != null)
            {
                var player = GameManager.Instance.PlayerManager.GetPlayer(steamID);
                bool isReady = player != null && player.IsReady;
                readyIndicator.color = isReady ? Color.green : Color.gray;
            }
        }

        private void OnPlayerJoined(CSteamID steamID)
        {
            AddPlayerSlot(steamID);
            RefreshPlayerList();
        }

        private void OnPlayerLeft(CSteamID steamID)
        {
            if (_playerSlots.TryGetValue(steamID, out GameObject slot))
            {
                Destroy(slot);
                _playerSlots.Remove(steamID);
            }
            RefreshPlayerList();
        }

        private void ToggleReady()
        {
            _isReady = !_isReady;
            UpdateReadyButton();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetReady(_isReady);
            }

            UpdateStatus(_isReady ? "Ready!" : "Not Ready");
        }

        private void UpdateReadyButton()
        {
            if (readyButton != null)
            {
                TextMeshProUGUI buttonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = _isReady ? "Not Ready" : "Ready";
                }
                readyButton.GetComponent<Image>().color = _isReady ? Color.green : Color.white;
            }
        }

        private void StartGame()
        {
            if (!_isHost)
            {
                Debug.LogWarning("Only host can start the game");
                return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame();
            }
        }

        private void LeaveLobby()
        {
            if (SteamLobbyManager.Instance != null)
            {
                SteamLobbyManager.Instance.LeaveLobby();
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.DisconnectAll();
            }

            // Return to main menu
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private void Update()
        {
            // Update start button based on game state
            if (_isHost && startGameButton != null)
            {
                bool canStart = GameManager.Instance != null &&
                               GameManager.Instance.PlayerManager.PlayerCount >= 4 &&
                               GameManager.Instance.PlayerManager.AllPlayersReady();
                startGameButton.interactable = canStart;
            }

            // Refresh player slots to update ready states
            foreach (var kvp in _playerSlots)
            {
                UpdatePlayerSlot(kvp.Value, kvp.Key);
            }
        }

        private void UpdateStartButton()
        {
            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(_isHost);
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}

