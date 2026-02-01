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
        
        private float _clueTimer = 0f;
        private float _clueTimeLimit = 5f;
        private bool _clueTimerActive = false;

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
            
            // Reset timer
            _clueTimerActive = false;
            _clueTimer = 0f;
            
            // Ensure clue container is visible
            if (clueListContainer != null)
            {
                clueListContainer.gameObject.SetActive(true);
                Debug.Log("Clue container is now visible");
            }
            
            // Check if it's local player's turn to start
            if (IsMyTurn())
            {
                StartClueTimer();
            }
        }

        private void OnClueSubmitted(CSteamID playerID, string clue)
        {
            Debug.Log($"OnClueSubmitted called: Player={playerID}, Clue={clue}");
            AddClueDisplay(playerID, clue);
            UpdateTurnIndicator();
            
            // Reset clue timer
            _clueTimerActive = false;
            _clueTimer = 0f;
            
            // Check if it's now local player's turn
            bool isMyTurn = IsMyTurn();
            SetClueInputEnabled(isMyTurn);
            
            if (isMyTurn)
            {
                // Start 5-second timer for local player
                StartClueTimer();
            }
        }
        
        private void StartClueTimer()
        {
            _clueTimer = _clueTimeLimit;
            _clueTimerActive = true;
            UpdateClueTimer();
        }
        
        private void UpdateClueTimer()
        {
            if (timerText != null && _clueTimerActive)
            {
                int seconds = Mathf.CeilToInt(_clueTimer);
                timerText.text = $"Time: {seconds}s";
                
                if (timerSlider != null)
                {
                    timerSlider.value = _clueTimer / _clueTimeLimit;
                }
            }
        }

        private void OnAllCluesSubmitted()
        {
            SetClueInputEnabled(false);
            UpdateStatus("All clues submitted! Moving to voting...");
            
            // Ensure all clues are displayed in the left panel
            RefreshAllClues();
            
            // Voting should start automatically via GameManager.OnAllCluesSubmitted
            Debug.Log("All clues submitted - voting phase should start automatically");
        }
        
        private void RefreshAllClues()
        {
            if (GameManager.Instance == null || GameManager.Instance.RoundManager == null)
            {
                return;
            }
            
            // Get all clues from RoundManager
            var allClues = GameManager.Instance.RoundManager.GetAllClues();
            
            Debug.Log($"Refreshing clue display - {allClues.Count} clues total");
            
            // Clear existing display
            ClearClues();
            
            // Add all clues to the left panel
            foreach (var kvp in allClues)
            {
                AddClueDisplay(kvp.Key, kvp.Value);
            }
        }

        private void HandleWordAssigned(NetworkMessage message, CSteamID senderID)
        {
            if (message is WordAssignedMessage wordMsg)
            {
                CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;
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
                Debug.Log($"HandleClueSubmitted: Player={playerID}, Clue={clueMsg.Clue}");
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
            CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;

            if (currentPlayerText != null)
            {
                if (currentPlayer == localID)
                {
                    currentPlayerText.text = "Your Turn!";
                    currentPlayerText.color = Color.green;
                }
                else if (currentPlayer.IsValid())
                {
                    // Get player name from PlayerManager first (works for dummy players)
                    string playerName = "Unknown";
                    if (GameManager.Instance.PlayerManager != null)
                    {
                        var playerData = GameManager.Instance.PlayerManager.GetPlayer(currentPlayer);
                        if (playerData != null)
                        {
                            playerName = playerData.PlayerName;
                        }
                    }
                    
                    // Fallback to SteamLobbyManager if not found
                    if (playerName == "Unknown" && SteamLobbyManager.Instance != null)
                    {
                        playerName = SteamLobbyManager.Instance.GetPlayerName(currentPlayer);
                    }
                    
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
            // Fallback: try to find container by name if reference is missing
            if (clueListContainer == null)
            {
                GameObject containerObj = GameObject.Find("ClueListContainer");
                if (containerObj != null)
                {
                    clueListContainer = containerObj.transform;
                    Debug.Log("Found ClueListContainer by name");
                }
                else
                {
                    Debug.LogError("clueListContainer is null and couldn't find by name! Cannot display clue.");
                    return;
                }
            }
            
            if (clueItemPrefab == null)
            {
                Debug.LogError("clueItemPrefab is null! Cannot display clue.");
                return;
            }
            
            // Ensure container is active and visible
            if (!clueListContainer.gameObject.activeSelf)
            {
                clueListContainer.gameObject.SetActive(true);
                Debug.Log("Activated ClueListContainer");
            }

            // Get player name from PlayerManager first, then SteamLobbyManager
            string playerName = "Unknown";
            if (GameManager.Instance != null && GameManager.Instance.PlayerManager != null)
            {
                var playerData = GameManager.Instance.PlayerManager.GetPlayer(playerID);
                if (playerData != null)
                {
                    playerName = playerData.PlayerName;
                }
            }
            
            if (playerName == "Unknown" && SteamLobbyManager.Instance != null)
            {
                playerName = SteamLobbyManager.Instance.GetPlayerName(playerID);
            }

            // Remove existing clue if player already submitted
            if (_clueItems.TryGetValue(playerID, out GameObject existingItem))
            {
                if (existingItem != null)
                {
                    Destroy(existingItem);
                }
                _clueItems.Remove(playerID);
            }

            // Instantiate clue item
            GameObject clueItem = Instantiate(clueItemPrefab, clueListContainer);
            _clueItems[playerID] = clueItem;
            
            // Ensure it's active and visible
            clueItem.SetActive(true);
            
            // Set RectTransform for proper layout
            RectTransform clueRect = clueItem.GetComponent<RectTransform>();
            if (clueRect != null)
            {
                clueRect.localScale = Vector3.one;
                clueRect.anchoredPosition = Vector2.zero;
            }

            // Set the clue text
            TextMeshProUGUI clueText = clueItem.GetComponentInChildren<TextMeshProUGUI>();
            if (clueText != null)
            {
                clueText.text = $"{playerName}: {clue}";
                clueText.color = Color.white; // Ensure text is white and visible
                Debug.Log($"✓ Added clue to left panel: {playerName}: {clue}");
            }
            else
            {
                Debug.LogWarning($"ClueItem prefab doesn't have TextMeshProUGUI component!");
            }
            
            // Make sure the clue item GameObject is visible
            if (clueItem != null)
            {
                clueItem.SetActive(true);
                clueItem.hideFlags = HideFlags.None;
            }
            
            // Force layout update
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(clueListContainer as RectTransform);
            
            // Log detailed info
            Debug.Log($"✓✓✓ Clue display added: {playerName}: {clue}");
            Debug.Log($"   Container: {clueListContainer.name}, Active: {clueListContainer.gameObject.activeSelf}, Children: {clueListContainer.childCount}");
            Debug.Log($"   ClueItem active: {clueItem.activeSelf}, Position: {clueRect?.anchoredPosition}");
            
            // Ensure parent (WorldPanel) is also visible
            if (clueListContainer.parent != null)
            {
                clueListContainer.parent.gameObject.SetActive(true);
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

            CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;

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

            CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;
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
            
            // Continuously check if it's local player's turn and enable/disable input
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.InGame)
            {
                bool isMyTurn = IsMyTurn();
                SetClueInputEnabled(isMyTurn);
                
                // Update clue timer
                if (isMyTurn && _clueTimerActive)
                {
                    _clueTimer -= Time.deltaTime;
                    if (_clueTimer <= 0f)
                    {
                        _clueTimer = 0f;
                        _clueTimerActive = false;
                        // Auto-submit if timer runs out and input is empty
                        if (clueInputField != null && string.IsNullOrEmpty(clueInputField.text.Trim()))
                        {
                            // Generate a default clue
                            string defaultClue = "clue";
                            if (GameManager.Instance.RoundManager != null)
                            {
                                var playerData = GameManager.Instance.PlayerManager?.GetPlayer(Impostor.Steam.SteamManager.Instance.LocalSteamID);
                                if (playerData != null && playerData.Role == PlayerRole.Civilian)
                                {
                                    defaultClue = GameManager.Instance.RoundManager.CurrentSecretWord.Substring(0, Mathf.Min(3, GameManager.Instance.RoundManager.CurrentSecretWord.Length));
                                }
                            }
                            clueInputField.text = defaultClue;
                            SubmitClue();
                        }
                    }
                    UpdateClueTimer();
                }
                else if (!isMyTurn)
                {
                    _clueTimerActive = false;
                    if (timerText != null)
                    {
                        timerText.text = "";
                    }
                }
            }

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
                    timerSlider.value = timeRemaining / 5f; // 5 second voting timer
                }
            }
        }
    }
}

