using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using Impostor.Game;
using Impostor.Steam;

namespace Impostor.UI
{
    /// <summary>
    /// In-game HUD showing word display, timer, turn indicator, and clue submissions.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Word Display")]
        [SerializeField] private TextMeshProUGUI secretWordText;
        [SerializeField] private GameObject wordPanel;

        [Header("Turn Indicator")]
        [SerializeField] private TextMeshProUGUI currentPlayerText;
        [SerializeField] private TextMeshProUGUI roundText;

        [Header("Clue Input")]
        [SerializeField] private TMP_InputField clueInputField;
        [SerializeField] private Button submitClueButton;

        [Header("Clue Display")]
        [SerializeField] private Transform clueListContainer;
        [SerializeField] private GameObject clueItemPrefab;

        [Header("Timer")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Slider timerSlider;

        private string _localSecretWord = "";
        private bool _isImpostor = false;
        private Dictionary<CSteamID, GameObject> _clueItems = new Dictionary<CSteamID, GameObject>();

        private void Start()
        {
            InitializeUI();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }

            // Get RoundManager from GameManager
            RoundManager roundManager = null;
            if (GameManager.Instance != null)
            {
                roundManager = GameManager.Instance.RoundManager;
            }

            if (roundManager != null)
            {
                roundManager.OnRoundStarted += OnRoundStarted;
                roundManager.OnClueSubmitted += OnClueSubmitted;
                roundManager.OnAllCluesSubmitted += OnAllCluesSubmitted;
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.RegisterMessageHandler(
                    NetworkMessage.MessageType.WordAssigned,
                    HandleWordAssigned);
                NetworkManager.Instance.RegisterMessageHandler(
                    NetworkMessage.MessageType.ClueSubmitted,
                    HandleClueSubmitted);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
                
                RoundManager roundManager = GameManager.Instance.RoundManager;
                if (roundManager != null)
                {
                    roundManager.OnRoundStarted -= OnRoundStarted;
                    roundManager.OnClueSubmitted -= OnClueSubmitted;
                    roundManager.OnAllCluesSubmitted -= OnAllCluesSubmitted;
                }
            }
        }

        private void InitializeUI()
        {
            if (submitClueButton != null)
            {
                submitClueButton.onClick.AddListener(SubmitClue);
            }

            if (clueInputField != null)
            {
                clueInputField.onSubmit.AddListener(_ => SubmitClue());
            }

            UpdateWordDisplay();
            SetClueInputEnabled(false);
        }

        private void OnGameStateChanged(GameManager.GameState newState)
        {
            switch (newState)
            {
                case GameManager.GameState.InGame:
                    SetClueInputEnabled(IsMyTurn());
                    break;
                case GameManager.GameState.Voting:
                    SetClueInputEnabled(false);
                    break;
            }
        }

        private void OnRoundStarted(int roundNumber, string secretWord)
        {
            UpdateRoundDisplay(roundNumber);
            ClearClues();
        }

        private void OnClueSubmitted(CSteamID playerID, string clue)
        {
            AddClueDisplay(playerID, clue);
            UpdateTurnIndicator();
            SetClueInputEnabled(IsMyTurn());
        }

        private void OnAllCluesSubmitted()
        {
            SetClueInputEnabled(false);
            UpdateStatus("All clues submitted! Moving to voting...");
        }

        private void HandleWordAssigned(NetworkMessage message, CSteamID senderID)
        {
            if (message is WordAssignedMessage wordMsg)
            {
                CSteamID localID = SteamManager.Instance.LocalSteamID;
                if (wordMsg.PlayerSteamID == localID.m_SteamID)
                {
                    _localSecretWord = wordMsg.Word;
                    _isImpostor = wordMsg.IsImpostor;
                    UpdateWordDisplay();
                }
            }
        }

        private void HandleClueSubmitted(NetworkMessage message, CSteamID senderID)
        {
            if (message is ClueSubmittedMessage clueMsg)
            {
                CSteamID playerID = new CSteamID(clueMsg.PlayerSteamID);
                AddClueDisplay(playerID, clueMsg.Clue);
            }
        }

        private void UpdateWordDisplay()
        {
            if (secretWordText != null)
            {
                if (_isImpostor)
                {
                    secretWordText.text = "IMPOSTOR";
                    secretWordText.color = Color.red;
                }
                else
                {
                    secretWordText.text = _localSecretWord;
                    secretWordText.color = Color.white;
                }
            }
        }

        private void UpdateRoundDisplay(int roundNumber)
        {
            if (roundText != null)
            {
                roundText.text = $"Round {roundNumber}";
            }
        }

        private void UpdateTurnIndicator()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            RoundManager roundManager = GameManager.Instance.RoundManager;
            if (roundManager == null)
            {
                return;
            }

            CSteamID currentPlayer = roundManager.CurrentPlayer;
            CSteamID localID = SteamManager.Instance.LocalSteamID;

            if (currentPlayerText != null)
            {
                if (currentPlayer == localID)
                {
                    currentPlayerText.text = "Your Turn!";
                    currentPlayerText.color = Color.green;
                }
                else if (currentPlayer.IsValid())
                {
                    string playerName = SteamLobbyManager.Instance.GetPlayerName(currentPlayer);
                    currentPlayerText.text = $"{playerName}'s Turn";
                    currentPlayerText.color = Color.white;
                }
                else
                {
                    currentPlayerText.text = "Waiting...";
                    currentPlayerText.color = Color.gray;
                }
            }
        }

        private void AddClueDisplay(CSteamID playerID, string clue)
        {
            if (clueListContainer == null || clueItemPrefab == null)
            {
                return;
            }

            // Remove existing clue if player already submitted
            if (_clueItems.TryGetValue(playerID, out GameObject existingItem))
            {
                Destroy(existingItem);
            }

            GameObject clueItem = Instantiate(clueItemPrefab, clueListContainer);
            _clueItems[playerID] = clueItem;

            TextMeshProUGUI clueText = clueItem.GetComponentInChildren<TextMeshProUGUI>();
            if (clueText != null)
            {
                string playerName = SteamLobbyManager.Instance.GetPlayerName(playerID);
                clueText.text = $"{playerName}: {clue}";
            }
        }

        private void ClearClues()
        {
            foreach (var item in _clueItems.Values)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            _clueItems.Clear();
        }

        private void SubmitClue()
        {
            if (clueInputField == null)
            {
                return;
            }

            string clue = clueInputField.text.Trim();
            if (string.IsNullOrEmpty(clue))
            {
                return;
            }

            if (GameManager.Instance == null)
            {
                return;
            }

            RoundManager roundManager = GameManager.Instance.RoundManager;
            if (roundManager == null)
            {
                return;
            }

            CSteamID localID = SteamManager.Instance.LocalSteamID;

            if (GameManager.Instance.IsHost)
            {
                roundManager.SubmitClue(localID, clue);
            }
            else
            {
                ClueSubmittedMessage message = new ClueSubmittedMessage
                {
                    PlayerSteamID = localID.m_SteamID,
                    Clue = clue
                };
                NetworkManager.Instance.SendMessage(message, GetHostSteamID());
            }

            clueInputField.text = "";
            SetClueInputEnabled(false);
        }

        private bool IsMyTurn()
        {
            if (GameManager.Instance == null)
            {
                return false;
            }

            RoundManager roundManager = GameManager.Instance.RoundManager;
            if (roundManager == null)
            {
                return false;
            }

            CSteamID localID = SteamManager.Instance.LocalSteamID;
            return roundManager.CurrentPlayer == localID;
        }

        private void SetClueInputEnabled(bool enabled)
        {
            if (clueInputField != null)
            {
                clueInputField.interactable = enabled;
            }

            if (submitClueButton != null)
            {
                submitClueButton.interactable = enabled;
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

        private void UpdateStatus(string message)
        {
            Debug.Log($"[GameUI] {message}");
        }

        private void Update()
        {
            UpdateTurnIndicator();

            // Update timer if voting
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState == GameManager.GameState.Voting &&
                GameManager.Instance.VoteManager != null)
            {
                float timeRemaining = GameManager.Instance.VoteManager.VotingTimeRemaining;
                if (timerText != null)
                {
                    timerText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}s";
                }
                if (timerSlider != null)
                {
                    timerSlider.value = timeRemaining / 60f;
                }
            }
        }
    }
}

