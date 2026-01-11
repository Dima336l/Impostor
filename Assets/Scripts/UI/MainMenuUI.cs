using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Impostor.Steam;
using Impostor.Game;

namespace Impostor.UI
{
    /// <summary>
    /// Main menu UI controller with Steam integration for lobby creation and joining.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button createLobbyButton;
        [SerializeField] private Button joinLobbyButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingPanel;

        private void Start()
        {
            InitializeUI();

            // Subscribe to Steam events
            if (SteamManager.Instance != null)
            {
                SteamManager.Instance.OnSteamInitialized += OnSteamInitialized;
            }

            if (SteamLobbyManager.Instance != null)
            {
                SteamLobbyManager.Instance.OnLobbyCreated += OnLobbyCreated;
                SteamLobbyManager.Instance.OnLobbyJoined += OnLobbyJoined;
            }
        }

        private void OnDestroy()
        {
            if (SteamManager.Instance != null)
            {
                SteamManager.Instance.OnSteamInitialized -= OnSteamInitialized;
            }

            if (SteamLobbyManager.Instance != null)
            {
                SteamLobbyManager.Instance.OnLobbyCreated -= OnLobbyCreated;
                SteamLobbyManager.Instance.OnLobbyJoined -= OnLobbyJoined;
            }
        }

        private void InitializeUI()
        {
            if (createLobbyButton != null)
            {
                createLobbyButton.onClick.AddListener(CreateLobby);
            }

            if (joinLobbyButton != null)
            {
                joinLobbyButton.onClick.AddListener(JoinLobby);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }

            UpdateStatus("Initializing Steam...");
            SetButtonsInteractable(false);
        }

        private void OnSteamInitialized()
        {
            UpdateStatus("Steam initialized. Ready to play!");
            SetButtonsInteractable(true);
        }

        private void CreateLobby()
        {
            if (!SteamManager.Instance.IsInitialized)
            {
                UpdateStatus("Steam not initialized. Please wait...");
                return;
            }

            UpdateStatus("Creating lobby...");
            SetButtonsInteractable(false);
            ShowLoading(true);

            SteamLobbyManager.Instance.CreateLobby(Steamworks.ELobbyType.k_ELobbyTypeFriendsOnly, 6);
        }

        private void JoinLobby()
        {
            if (!SteamManager.Instance.IsInitialized)
            {
                UpdateStatus("Steam not initialized. Please wait...");
                return;
            }

            // Open Steam overlay to join friend's game
            UpdateStatus("Opening Steam overlay...");
            Steamworks.SteamFriends.ActivateGameOverlay("friends");
        }

        private void OnLobbyCreated(Steamworks.CSteamID lobbyID)
        {
            UpdateStatus($"Lobby created! ID: {lobbyID}");
            ShowLoading(false);
            
            // Transition to lobby scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }

        private void OnLobbyJoined(Steamworks.CSteamID lobbyID)
        {
            UpdateStatus($"Joined lobby! ID: {lobbyID}");
            ShowLoading(false);
            
            // Transition to lobby scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
        }

        private void QuitGame()
        {
            Application.Quit();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[MainMenu] {message}");
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (createLobbyButton != null)
            {
                createLobbyButton.interactable = interactable;
            }

            if (joinLobbyButton != null)
            {
                joinLobbyButton.interactable = interactable;
            }
        }

        private void ShowLoading(bool show)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(show);
            }
        }
    }
}

