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
        private CSteamID _currentTimerPlayer = CSteamID.Nil; // Track which player's timer is active
        private int _lastTimerStartFrame = -1; // Track the frame when timer was last started to prevent duplicates
        private bool _roundStarting = false; // Flag to prevent timer starts during round initialization
        
        // Public property to check if timer has expired (for VoteUITester)
        public bool IsClueTimerExpired => _clueTimerActive && _clueTimer <= 0f;
        public float ClueTimerRemaining => _clueTimerActive ? _clueTimer : 0f;

        private string _localSecretWord = "";
        private bool _isImpostor = false;
        private Dictionary<CSteamID, GameObject> _clueItems = new Dictionary<CSteamID, GameObject>();

        private void Start()
        {
            // Fallback: Find timer UI elements by name if not assigned
            if (timerText == null)
            {
                GameObject timerTextObj = GameObject.Find("TimerText");
                if (timerTextObj != null)
                {
                    timerText = timerTextObj.GetComponent<TextMeshProUGUI>();
                    Debug.Log("[GameUI] Found timerText by name");
                }
                else
                {
                    Debug.LogWarning("[GameUI] timerText not assigned and couldn't find by name!");
                }
            }
            
            if (timerSlider == null)
            {
                GameObject timerSliderObj = GameObject.Find("TimerSlider");
                if (timerSliderObj != null)
                {
                    timerSlider = timerSliderObj.GetComponent<Slider>();
                    Debug.Log("[GameUI] Found timerSlider by name");
                }
                else
                {
                    Debug.LogWarning("[GameUI] timerSlider not assigned and couldn't find by name!");
                }
            }
            
            // Fallback: Find clue container and prefab by name if not assigned
            if (clueListContainer == null)
            {
                GameObject containerObj = GameObject.Find("ClueListContainer");
                if (containerObj != null)
                {
                    clueListContainer = containerObj.transform;
                    Debug.Log("[GameUI] Found clueListContainer by name");
                }
                else
                {
                    Debug.LogError("[GameUI] clueListContainer not assigned and couldn't find by name!");
                }
            }
            
            if (clueItemPrefab == null)
            {
                // Try to find the prefab in Resources
                clueItemPrefab = Resources.Load<GameObject>("Prefabs/UI/ClueItem");
                if (clueItemPrefab != null)
                {
                    Debug.Log("[GameUI] Found clueItemPrefab in Resources");
                }
                else
                {
                    Debug.LogError("[GameUI] clueItemPrefab not assigned and couldn't find in Resources!");
                }
            }
            
            InitializeUI();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }

            // Get RoundManager from GameManager - try multiple times if not ready
            SubscribeToRoundManagerEvents();

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

        private void SubscribeToRoundManagerEvents()
        {
            // Get RoundManager from GameManager
            RoundManager roundManager = null;
            if (GameManager.Instance != null)
            {
                roundManager = GameManager.Instance.RoundManager;
            }

            if (roundManager != null)
            {
                // Unsubscribe first to avoid duplicates
                roundManager.OnRoundStarted -= OnRoundStarted;
                roundManager.OnClueSubmitted -= OnClueSubmitted;
                roundManager.OnAllCluesSubmitted -= OnAllCluesSubmitted;
                
                // Subscribe
                roundManager.OnRoundStarted += OnRoundStarted;
                roundManager.OnClueSubmitted += OnClueSubmitted;
                roundManager.OnAllCluesSubmitted += OnAllCluesSubmitted;
                Debug.Log("[GameUI] Subscribed to RoundManager events");
            }
            else
            {
                Debug.LogWarning("[GameUI] RoundManager is null, will retry in Update()");
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
                    Debug.Log($"[GameUI] ========== OnGameStateChanged -> InGame (Frame: {Time.frameCount}) ==========");
                    Debug.Log($"[GameUI] Timer state BEFORE reset: Active={_clueTimerActive}, Player={_currentTimerPlayer}, Timer={_clueTimer:F2}s, Frame={_lastTimerStartFrame}, RoundStarting={_roundStarting}");
                    
                    SetClueInputEnabled(IsMyTurn());
                    // CRITICAL: Reset clue timer state completely when entering InGame
                    // This ensures a clean state for the new round, especially after voting
                    // Don't set _roundStarting here - OnRoundStarted handles that
                    _clueTimerActive = false;
                    _clueTimer = 0f;
                    _currentTimerPlayer = CSteamID.Nil;
                    _lastTimerStartFrame = -1; // Reset frame tracking
                    
                    Debug.Log($"[GameUI] Timer state AFTER reset: Active={_clueTimerActive}, Player={_currentTimerPlayer}, Timer={_clueTimer:F2}s, Frame={_lastTimerStartFrame}, RoundStarting={_roundStarting}");
                    
                    // Hide timer UI to ensure clean visual state
                    if (timerText != null)
                    {
                        timerText.gameObject.SetActive(false);
                    }
                    if (timerSlider != null)
                    {
                        timerSlider.gameObject.SetActive(false);
                    }
                    
                    CSteamID currentPlayer = GameManager.Instance?.RoundManager?.CurrentPlayer ?? CSteamID.Nil;
                    Debug.Log($"[GameUI] Entered InGame state - timer state reset. Current player: {currentPlayer}, Round starting: {_roundStarting}");
                    break;
                case GameManager.GameState.Voting:
                    SetClueInputEnabled(false);
                    // Ensure timer is visible during voting
                    if (timerText != null)
                    {
                        timerText.gameObject.SetActive(true);
                        Debug.Log("Voting state entered - timer text activated");
                    }
                    if (timerSlider != null)
                    {
                        timerSlider.gameObject.SetActive(true);
                        Debug.Log("Voting state entered - timer slider activated");
                    }
                    // Ensure VoteManager starts voting on all clients (not just host)
                    // But only if it hasn't been started yet (prevent restart)
                    if (GameManager.Instance != null && GameManager.Instance.VoteManager != null)
                    {
                        VoteManager vm = GameManager.Instance.VoteManager;
                        if (!vm.VotingInProgress)
                        {
                            // Start voting timer even on non-host clients
                            vm.StartVoting(5f);
                            Debug.Log("[GameUI] Started voting timer on client (OnGameStateChanged)");
                        }
                        else
                        {
                            Debug.Log("[GameUI] Voting already in progress, not restarting");
                        }
                    }
                    break;
            }
        }

        private void OnRoundStarted(int roundNumber, string secretWord)
        {
            Debug.Log($"[GameUI] ========== OnRoundStarted called - Round {roundNumber} ==========");
            Debug.Log($"[GameUI] Current timer state BEFORE reset: Active={_clueTimerActive}, Player={_currentTimerPlayer}, Timer={_clueTimer:F2}s, Frame={_lastTimerStartFrame}, RoundStarting={_roundStarting}");
            
            UpdateRoundDisplay(roundNumber);
            ClearClues();
            
            // CRITICAL: Reset ALL timer state completely before setting flag
            // This ensures no lingering state from previous round/voting
            _clueTimerActive = false;
            _clueTimer = 0f;
            _currentTimerPlayer = CSteamID.Nil;
            _lastTimerStartFrame = -1;
            _roundStarting = true; // Set flag AFTER resetting state
            
            Debug.Log($"[GameUI] Timer state AFTER reset: Active={_clueTimerActive}, Player={_currentTimerPlayer}, Timer={_clueTimer:F2}s, Frame={_lastTimerStartFrame}, RoundStarting={_roundStarting}");
            
            // Hide timer UI to ensure clean state
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
                Debug.Log("[GameUI] Timer text hidden");
            }
            if (timerSlider != null)
            {
                timerSlider.gameObject.SetActive(false);
                Debug.Log("[GameUI] Timer slider hidden");
            }
            
            // Ensure clue container is visible
            if (clueListContainer != null)
            {
                clueListContainer.gameObject.SetActive(true);
                Debug.Log("Clue container is now visible");
            }
            
            // Get current player for logging
            CSteamID firstPlayer = CSteamID.Nil;
            if (GameManager.Instance?.RoundManager != null)
            {
                firstPlayer = GameManager.Instance.RoundManager.CurrentPlayer;
            }
            Debug.Log($"[GameUI] Round {roundNumber} started - timer state reset. First player will be: {firstPlayer}. Waiting for state to settle...");
            
            // Clear the flag after a delay to allow all state changes to settle
            // Wait 2 frames to ensure OnGameStateChanged has also fired
            StartCoroutine(ClearRoundStartingFlag());
        }
        
        private System.Collections.IEnumerator ClearRoundStartingFlag()
        {
            Debug.Log($"[GameUI] ClearRoundStartingFlag coroutine started (Frame: {Time.frameCount})");
            // Wait 2 frames to ensure OnGameStateChanged and all other state changes have settled
            yield return null;
            Debug.Log($"[GameUI] ClearRoundStartingFlag - After frame 1 (Frame: {Time.frameCount})");
            yield return null;
            Debug.Log($"[GameUI] ClearRoundStartingFlag - After frame 2 (Frame: {Time.frameCount})");
            _roundStarting = false;
            CSteamID currentPlayer = GameManager.Instance?.RoundManager?.CurrentPlayer ?? CSteamID.Nil;
            Debug.Log($"[GameUI] ========== Round starting flag cleared (Frame: {Time.frameCount}) ==========");
            Debug.Log($"[GameUI] Timer can now start. Current player: {currentPlayer}, Timer state: Active={_clueTimerActive}, Player={_currentTimerPlayer}");
        }

        private void OnClueSubmitted(CSteamID playerID, string clue)
        {
            Debug.Log($"[GameUI] OnClueSubmitted called: Player={playerID}, Clue={clue}");
            Debug.Log($"[GameUI] clueListContainer: {(clueListContainer != null ? clueListContainer.name : "NULL")}, clueItemPrefab: {(clueItemPrefab != null ? clueItemPrefab.name : "NULL")}");
            
            // Always add clue display IMMEDIATELY - this ensures it appears in the left panel for ALL players
            // This is called for every player's clue submission (local and others)
            // Must be called synchronously to appear instantly
            AddClueDisplay(playerID, clue);
            
            // Force immediate UI update
            Canvas.ForceUpdateCanvases();
            if (clueListContainer != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(clueListContainer as RectTransform);
                Debug.Log($"[GameUI] Forced layout rebuild - Container children: {clueListContainer.childCount}");
            }
            else
            {
                Debug.LogError("[GameUI] clueListContainer is NULL in OnClueSubmitted! Cannot update layout.");
            }
            
            // Reset clue timer for the player who just submitted
            if (_currentTimerPlayer == playerID)
            {
                _clueTimerActive = false;
                _clueTimer = 0f;
                // CRITICAL: RoundManager moves to next player AFTER OnClueSubmitted is called
                // So at this point, RoundManager.CurrentPlayer is still the same player (playerID)
                // We should NOT reset _currentTimerPlayer here - let Update() detect the actual player change
                // and reset it then. This prevents Update() from seeing a change from 0 to the same player
                // and starting a duplicate timer.
                _lastTimerStartFrame = -1; // Reset frame tracking
                Debug.Log($"[GameUI] Stopped timer for player {playerID} who just submitted - keeping _currentTimerPlayer set until RoundManager moves to next player");
            }
            
            // Check if it's now local player's turn
            bool isMyTurn = IsMyTurn();
            SetClueInputEnabled(isMyTurn);
            
            // DON'T start timer here - RoundManager already moved to next player
            // The Update() method will detect the player change and start the timer once
            // This prevents duplicate timer starts
            UpdateTurnIndicator();
        }
        
        private void StartClueTimer()
        {
            // Legacy method - use StartClueTimerForCurrentPlayer instead
            StartClueTimerForCurrentPlayer();
        }
        
        private void StartClueTimerForCurrentPlayer()
        {
            Debug.Log($"[GameUI] ========== StartClueTimerForCurrentPlayer called (Frame: {Time.frameCount}) ==========");
            
            if (GameManager.Instance == null || GameManager.Instance.RoundManager == null)
            {
                Debug.LogError("[GameUI] Cannot start timer - GameManager or RoundManager is null");
                return;
            }
            
            CSteamID currentPlayer = GameManager.Instance.RoundManager.CurrentPlayer;
            Debug.Log($"[GameUI] Current player from RoundManager: {currentPlayer}");
            Debug.Log($"[GameUI] Current timer state: Active={_clueTimerActive}, Player={_currentTimerPlayer}, Timer={_clueTimer:F2}s, Frame={_lastTimerStartFrame}, RoundStarting={_roundStarting}");
            
            if (!currentPlayer.IsValid())
            {
                Debug.LogWarning("[GameUI] Cannot start timer - no valid current player");
                return;
            }
            
            // CRITICAL: Prevent starting timer if already active for THIS player
            if (_clueTimerActive && _currentTimerPlayer == currentPlayer)
            {
                Debug.LogWarning($"[GameUI] ❌ BLOCKED: Timer already active for player {currentPlayer} - not restarting (Timer: {_clueTimer:F2}s remaining)");
                return;
            }
            
            // CRITICAL: Prevent starting timer if timer is active for ANY player (shouldn't happen, but safety check)
            if (_clueTimerActive && _currentTimerPlayer != currentPlayer && _currentTimerPlayer.IsValid())
            {
                Debug.LogWarning($"[GameUI] ❌ BLOCKED: Timer already active for different player {_currentTimerPlayer} - cannot start for {currentPlayer} until current timer ends");
                return;
            }
            
            // Prevent starting timer multiple times in the same frame for the same player
            if (_lastTimerStartFrame == Time.frameCount && _currentTimerPlayer == currentPlayer)
            {
                Debug.LogWarning($"[GameUI] ❌ BLOCKED: Timer already started this frame for player {currentPlayer} - preventing duplicate");
                return;
            }
            
            // Start timer for current player
            _clueTimer = _clueTimeLimit;
            _clueTimerActive = true;
            _currentTimerPlayer = currentPlayer;
            _lastTimerStartFrame = Time.frameCount;
            Debug.Log($"[GameUI] ✓✓✓ STARTED timer for player {currentPlayer} - {_clueTimeLimit} seconds (Frame: {Time.frameCount})");
            Debug.Log($"[GameUI] Timer state AFTER start: Active={_clueTimerActive}, Player={_currentTimerPlayer}, Timer={_clueTimer:F2}s, Frame={_lastTimerStartFrame}");
            UpdateClueTimer();
        }
        
        private void UpdateClueTimer()
        {
            if (timerText != null && _clueTimerActive)
            {
                // Ensure timer is visible
                if (!timerText.gameObject.activeSelf)
                {
                    timerText.gameObject.SetActive(true);
                }
                
                int seconds = Mathf.CeilToInt(_clueTimer);
                timerText.text = $"Time: {seconds}s";
                
                if (timerSlider != null)
                {
                    // Ensure slider is visible
                    if (!timerSlider.gameObject.activeSelf)
                    {
                        timerSlider.gameObject.SetActive(true);
                    }
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
            Debug.Log("[GameUI] OnAllCluesSubmitted called - voting phase should start automatically");
            Debug.Log($"[GameUI] Current state before voting: {GameManager.Instance?.CurrentState}");
        }
        
        private void RefreshAllClues()
        {
            if (GameManager.Instance == null || GameManager.Instance.RoundManager == null)
            {
                Debug.LogWarning("[GameUI] Cannot refresh clues - GameManager or RoundManager is null");
                return;
            }
            
            // Get all clues from RoundManager
            var allClues = GameManager.Instance.RoundManager.GetAllClues();
            
            Debug.Log($"[GameUI] Refreshing clue display - {allClues.Count} clues total");
            
            if (allClues.Count == 0)
            {
                Debug.LogWarning("[GameUI] No clues found in RoundManager!");
                return;
            }
            
            // Clear existing display
            ClearClues();
            
            // Add all clues to the left panel
            foreach (var kvp in allClues)
            {
                Debug.Log($"[GameUI] Adding clue to display: Player={kvp.Key}, Clue={kvp.Value}");
                AddClueDisplay(kvp.Key, kvp.Value);
            }
            
            Debug.Log($"[GameUI] Finished refreshing clues - {_clueItems.Count} items in display");
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
                Debug.Log($"[GameUI] HandleClueSubmitted (network): Player={playerID}, Clue={clueMsg.Clue}");
                
                // Add clue display for network-received clues
                // Note: For host, this might be a duplicate, but AddClueDisplay handles that
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
            Debug.Log($"[GameUI] AddClueDisplay called: Player={playerID}, Clue={clue}");
            
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
                // Try to find prefab in Resources as fallback
                clueItemPrefab = Resources.Load<GameObject>("Prefabs/UI/ClueItem");
                if (clueItemPrefab == null)
                {
                    Debug.LogError("[GameUI] clueItemPrefab is null and couldn't find in Resources! Cannot display clue.");
                    return;
                }
                else
                {
                    Debug.Log("[GameUI] Found clueItemPrefab in Resources");
                }
            }
            
            // Ensure container is active and visible
            if (!clueListContainer.gameObject.activeSelf)
            {
                clueListContainer.gameObject.SetActive(true);
                Debug.Log("Activated ClueListContainer");
            }
            
            // Ensure VerticalLayoutGroup is enabled on container
            VerticalLayoutGroup layoutGroup = clueListContainer.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = clueListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                layoutGroup.spacing = 5f;
                layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = false; // Don't control height - let ContentSizeFitter handle it
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false; // Don't force expand - let items size to content
                Debug.Log("[GameUI] Added VerticalLayoutGroup to ClueListContainer");
            }
            else
            {
                // Ensure settings allow children to expand
                if (!layoutGroup.enabled)
                {
                    layoutGroup.enabled = true;
                    Debug.Log("[GameUI] Enabled VerticalLayoutGroup on ClueListContainer");
                }
                // Update settings to allow child expansion
                layoutGroup.childControlHeight = false; // Don't control height - let ContentSizeFitter handle it
                layoutGroup.childForceExpandHeight = false; // Don't force expand - let items size to content
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
            
            // Also check for orphaned children in container (not in dictionary)
            // This can happen if items were created but not tracked
            for (int i = clueListContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = clueListContainer.GetChild(i);
                // Check if this child is not in our dictionary
                bool foundInDict = false;
                foreach (var kvp in _clueItems)
                {
                    if (kvp.Value != null && kvp.Value.transform == child)
                    {
                        foundInDict = true;
                        break;
                    }
                }
                if (!foundInDict)
                {
                    Debug.LogWarning($"[GameUI] Found orphaned child in ClueListContainer: {child.name}, destroying it");
                    Destroy(child.gameObject);
                }
            }

            // Instantiate clue item
            GameObject clueItem = Instantiate(clueItemPrefab, clueListContainer);
            _clueItems[playerID] = clueItem;
            
            // Set RectTransform for proper layout BEFORE activating
            RectTransform clueRect = clueItem.GetComponent<RectTransform>();
            if (clueRect != null)
            {
                clueRect.localScale = Vector3.one;
                
                // CRITICAL: Set anchors and pivot FIRST, before sizeDelta
                // This ensures the layout group can properly position the item
                clueRect.anchorMin = new Vector2(0f, 1f);
                clueRect.anchorMax = new Vector2(1f, 1f);
                clueRect.pivot = new Vector2(0.5f, 1f);
                
                // Set minimum height, but allow expansion
                clueRect.sizeDelta = new Vector2(0f, 40f); // Start with 40px, but will expand if needed
                
                // CRITICAL: DON'T set anchoredPosition - let VerticalLayoutGroup handle positioning
                // The layout group will set this automatically based on child order
                // Setting it manually causes all items to overlap at the same position
            }
            
            // Configure the text component FIRST - it needs to fill the parent for ContentSizeFitter to work
            TextMeshProUGUI clueText = clueItem.GetComponentInChildren<TextMeshProUGUI>();
            if (clueText != null)
            {
                // Configure text RectTransform to fill parent FIRST
                RectTransform textRect = clueText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    // Make text fill the parent container so ContentSizeFitter can measure it
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.offsetMin = new Vector2(5f, 5f); // Small padding inside the box
                    textRect.offsetMax = new Vector2(-5f, -5f);
                }
                
                // Configure text settings for wrapping
                clueText.enableWordWrapping = true;
                clueText.overflowMode = TextOverflowModes.Overflow; // Allow wrapping, don't truncate
                clueText.color = Color.white;
                
                // Set text AFTER configuring RectTransform
                clueText.text = $"{playerName}: {clue}";
                
                Debug.Log($"✓ Added clue to left panel: {playerName}: {clue}");
            }
            else
            {
                Debug.LogWarning($"ClueItem prefab doesn't have TextMeshProUGUI component!");
            }
            
            // Add ContentSizeFitter to make the box expand to fit text content
            // Do this AFTER configuring text so it can measure properly
            ContentSizeFitter sizeFitter = clueItem.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = clueItem.AddComponent<ContentSizeFitter>();
            }
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Expand vertically to fit content
            
            // Ensure it's active and visible AFTER RectTransform is configured
            clueItem.SetActive(true);
            
            // Make sure the clue item GameObject is visible
            if (clueItem != null)
            {
                clueItem.SetActive(true);
                clueItem.hideFlags = HideFlags.None;
            }
            
            // Force immediate layout update to make clue visible right away
            // Do this AFTER all properties are set
            if (clueListContainer != null && clueRect != null)
            {
                // Set sibling index to ensure proper ordering (layout group uses child order)
                clueRect.SetAsLastSibling();
                
                // Mark the layout as dirty so it recalculates
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(clueListContainer as RectTransform);
                
                // Also mark the item itself as needing layout
                UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(clueRect);
                
                // Force canvas update to ensure layout is applied
                Canvas.ForceUpdateCanvases();
                
                // Schedule another rebuild next frame to catch any edge cases
                StartCoroutine(RebuildLayoutNextFrame(clueListContainer as RectTransform));
            }
            
            // Log detailed info
            Debug.Log($"[GameUI] ✓✓✓ Clue display added: {playerName}: {clue}");
            Debug.Log($"[GameUI]    Container: {clueListContainer.name}, Active: {clueListContainer.gameObject.activeSelf}, Children: {clueListContainer.childCount}");
            Debug.Log($"[GameUI]    ClueItem active: {clueItem.activeSelf}, Position: {clueRect?.anchoredPosition}, Size: {clueRect?.sizeDelta}");
            Debug.Log($"[GameUI]    LayoutGroup enabled: {layoutGroup?.enabled}, Spacing: {layoutGroup?.spacing}");
            
            // Ensure parent (WorldPanel) is also visible
            if (clueListContainer.parent != null)
            {
                clueListContainer.parent.gameObject.SetActive(true);
                Debug.Log($"[GameUI]    Parent ({clueListContainer.parent.name}) is now active");
            }
            
            // Double-check that the clue item is actually visible
            if (clueItem != null && !clueItem.activeSelf)
            {
                Debug.LogWarning($"[GameUI] Clue item was not active! Activating now...");
                clueItem.SetActive(true);
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
        
        private System.Collections.IEnumerator RebuildLayoutNextFrame(RectTransform container)
        {
            yield return null; // Wait one frame
            if (container != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container);
                Canvas.ForceUpdateCanvases();
            }
        }

        private void SubmitClue()
        {
            if (clueInputField == null)
            {
                Debug.LogWarning("[GameUI] clueInputField is null, cannot submit clue");
                return;
            }

            string clue = clueInputField.text.Trim();
            if (string.IsNullOrEmpty(clue))
            {
                Debug.LogWarning("[GameUI] Clue is empty, cannot submit");
                return;
            }

            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[GameUI] GameManager is null, cannot submit clue");
                return;
            }

            RoundManager roundManager = GameManager.Instance.RoundManager;
            if (roundManager == null)
            {
                Debug.LogWarning("[GameUI] RoundManager is null, cannot submit clue");
                return;
            }

            CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;

            // Clear input field immediately for better UX
            clueInputField.text = "";
            clueInputField.DeactivateInputField(); // Remove focus
            SetClueInputEnabled(false);

            Debug.Log($"[GameUI] Submitting clue: '{clue}' for player {localID}");

            if (GameManager.Instance.IsHost)
            {
                // For host, submit directly - this will trigger OnClueSubmitted event
                // The OnClueSubmitted event will call AddClueDisplay for all players
                roundManager.SubmitClue(localID, clue);
                // Note: Don't add immediately here - let the event handle it to avoid duplicates
                // The event fires synchronously, so it will appear immediately
            }
            else
            {
                // For non-host, send message to host
                ClueSubmittedMessage message = new ClueSubmittedMessage
                {
                    PlayerSteamID = localID.m_SteamID,
                    Clue = clue
                };
                NetworkManager.Instance.SendMessage(message, GetHostSteamID());
                
                // For non-host, also display immediately (host will broadcast it)
                // This ensures the clue appears right away for the submitting player
                AddClueDisplay(localID, clue);
            }
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
            // Ensure we're subscribed to RoundManager events (in case it wasn't ready at Start)
            // Only check once per frame to avoid performance issues
            if (Time.frameCount % 60 == 0 && GameManager.Instance != null && GameManager.Instance.RoundManager != null)
            {
                RoundManager roundManager = GameManager.Instance.RoundManager;
                // Try to subscribe - the method handles duplicate subscriptions safely
                SubscribeToRoundManagerEvents();
            }
            
            UpdateTurnIndicator();
            
            // Debug: Log state every 60 frames (once per second) to avoid spam
            if (Time.frameCount % 60 == 0 && GameManager.Instance != null)
            {
                var vm = GameManager.Instance.VoteManager;
                bool votingInProgress = vm != null && vm.VotingInProgress;
                Debug.Log($"[GameUI] State: {GameManager.Instance.CurrentState}, VotingInProgress: {votingInProgress}, timerText: {(timerText != null ? "OK" : "NULL")}, timerSlider: {(timerSlider != null ? "OK" : "NULL")}");
            }
            
            // CHECK VOTING STATE FIRST - this takes priority over InGame state
            // Also check if all clues are submitted (even if state hasn't changed yet)
            bool allCluesSubmitted = false;
            if (GameManager.Instance != null && GameManager.Instance.RoundManager != null)
            {
                var roundManager = GameManager.Instance.RoundManager;
                // Check if round is no longer in progress (all clues submitted)
                allCluesSubmitted = !roundManager.RoundInProgress && roundManager.CurrentRound > 0;
            }
            
            if (GameManager.Instance != null && 
                (GameManager.Instance.CurrentState == GameManager.GameState.Voting || allCluesSubmitted))
            {
                // Debug log when we enter voting check
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[GameUI] In Voting state check - State: {GameManager.Instance.CurrentState}, AllCluesSubmitted: {allCluesSubmitted}");
                }
                
                // Try to get VoteManager from GameManager first, then fallback to static instance
                VoteManager voteManager = GameManager.Instance.VoteManager;
                if (voteManager == null)
                {
                    voteManager = VoteManager.Instance;
                }
                
                if (voteManager != null && voteManager.VotingInProgress)
                {
                    float timeRemaining = voteManager.VotingTimeRemaining;
                    int seconds = Mathf.CeilToInt(timeRemaining);
                    
                    if (timerText != null)
                    {
                        // Ensure timer text is visible during voting
                        if (!timerText.gameObject.activeSelf)
                        {
                            timerText.gameObject.SetActive(true);
                            Debug.Log("[GameUI] Timer text activated in Update");
                        }
                        timerText.text = $"Time: {seconds}s";
                    }
                    else
                    {
                        Debug.LogWarning("[GameUI] timerText is NULL!");
                    }
                    
                    if (timerSlider != null)
                    {
                        // Ensure timer slider is visible during voting
                        if (!timerSlider.gameObject.activeSelf)
                        {
                            timerSlider.gameObject.SetActive(true);
                            Debug.Log("[GameUI] Timer slider activated in Update");
                        }
                        // Clamp value between 0 and 1, normalize by 5 seconds
                        float normalizedValue = Mathf.Clamp01(timeRemaining / 5f);
                        timerSlider.value = normalizedValue;
                    }
                    else
                    {
                        Debug.LogWarning("[GameUI] timerSlider is NULL!");
                    }
                }
                else
                {
                    // Voting state but VoteManager not ready yet
                    // Don't try to start it here - GameManager should handle it
                    // This prevents timer restarts when votes are cast
                    if (voteManager == null)
                    {
                        voteManager = GameManager.Instance?.VoteManager ?? VoteManager.Instance;
                    }
                    
                    // Just show placeholder timer if voting hasn't started yet
                    // Don't call StartVoting() here to prevent restarts
                    if (timerText != null)
                    {
                        timerText.gameObject.SetActive(true);
                        if (voteManager != null && voteManager.VotingInProgress)
                        {
                            float timeRemaining = voteManager.VotingTimeRemaining;
                            timerText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}s";
                        }
                        else
                        {
                            timerText.text = "Time: 5s"; // Default placeholder
                        }
                    }
                    if (timerSlider != null)
                    {
                        timerSlider.gameObject.SetActive(true);
                        if (voteManager != null && voteManager.VotingInProgress)
                        {
                            float timeRemaining = voteManager.VotingTimeRemaining;
                            timerSlider.value = Mathf.Clamp01(timeRemaining / 5f);
                        }
                        else
                        {
                            timerSlider.value = 1f; // Show full bar as placeholder
                        }
                    }
                }
            }
            // Handle InGame state (clue submission phase)
            else if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.InGame)
            {
                bool isMyTurn = IsMyTurn();
                SetClueInputEnabled(isMyTurn);
                
                // Get current player from RoundManager
                CSteamID currentPlayer = CSteamID.Nil;
                if (GameManager.Instance.RoundManager != null)
                {
                    currentPlayer = GameManager.Instance.RoundManager.CurrentPlayer;
                }
                
                // CRITICAL: Check if timer expired but player hasn't changed yet
                // This prevents duplicate timer starts when timer expires but RoundManager hasn't moved to next player
                if (currentPlayer.IsValid() && _currentTimerPlayer.IsValid() && currentPlayer == _currentTimerPlayer && !_clueTimerActive)
                {
                    // Timer expired for this player, but RoundManager hasn't moved to next player yet
                    // This is normal - just wait for clue submission or player change
                    // Don't start a new timer - the player will change when clue is submitted
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[GameUI] Timer expired for player {currentPlayer} but player hasn't changed yet - waiting for clue submission (Frame: {Time.frameCount})");
                    }
                }
                // If current player changed, start timer for new player (only once)
                // This ensures each player gets exactly one timer per turn
                else if (currentPlayer.IsValid() && currentPlayer != _currentTimerPlayer)
                {
                    // CRITICAL: If _currentTimerPlayer is set but timer is not active, this means:
                    // - Timer expired for the previous player, OR
                    // - Clue was submitted for the previous player
                    // In either case, we should reset _currentTimerPlayer now that the player has actually changed
                    if (_currentTimerPlayer.IsValid() && !_clueTimerActive)
                    {
                        Debug.Log($"[GameUI] Player changed from {_currentTimerPlayer} to {currentPlayer} - resetting _currentTimerPlayer from expired/submitted player");
                        _currentTimerPlayer = CSteamID.Nil;
                    }
                    
                    Debug.Log($"[GameUI] ========== Player change detected in Update() (Frame: {Time.frameCount}) ==========");
                    Debug.Log($"[GameUI] Previous player: {_currentTimerPlayer}, New player: {currentPlayer}");
                    Debug.Log($"[GameUI] Timer state: Active={_clueTimerActive}, Timer={_clueTimer:F2}s, Frame={_lastTimerStartFrame}, RoundStarting={_roundStarting}");
                    
                    bool canStartTimer = true;
                    string blockReason = "";
                    
                    // CRITICAL: Don't start timer if we're still initializing the round
                    if (_roundStarting)
                    {
                        canStartTimer = false;
                        blockReason = $"Round still starting (Frame: {Time.frameCount})";
                        Debug.Log($"[GameUI] ❌ BLOCKED: {blockReason} - skipping timer start for player {currentPlayer}");
                    }
                    // CRITICAL: Don't start if timer is already active (for any player)
                    // This prevents restarting the timer mid-countdown
                    else if (_clueTimerActive)
                    {
                        canStartTimer = false;
                        blockReason = $"Timer already active for player {_currentTimerPlayer} ({_clueTimer:F2}s remaining)";
                        Debug.LogWarning($"[GameUI] ❌ BLOCKED: {blockReason} - cannot start for {currentPlayer} until current timer ends");
                    }
                    // CRITICAL: Don't start if we already started a timer this frame
                    else if (_lastTimerStartFrame == Time.frameCount)
                    {
                        canStartTimer = false;
                        blockReason = $"Timer already started this frame (Frame: {Time.frameCount})";
                        Debug.LogWarning($"[GameUI] ❌ BLOCKED: {blockReason} - skipping duplicate start for {currentPlayer}");
                    }
                    
                    if (canStartTimer)
                    {
                        // Safe to start timer - all checks passed
                        Debug.Log($"[GameUI] ✓✓✓ All checks passed - Player changed from {_currentTimerPlayer} to {currentPlayer} - starting timer (Frame: {Time.frameCount}, Round starting: {_roundStarting})");
                        StartClueTimerForCurrentPlayer();
                    }
                }
                
                // Update clue timer for current player (local or other)
                // CRITICAL: Only update if timer is active AND matches the current player
                if (_clueTimerActive && currentPlayer.IsValid() && currentPlayer == _currentTimerPlayer)
                {
                    float oldTimer = _clueTimer;
                    _clueTimer -= Time.deltaTime;
                    
                    // Log timer updates every 0.5 seconds to track progress
                    if (Mathf.FloorToInt(oldTimer * 2) != Mathf.FloorToInt(_clueTimer * 2))
                    {
                        Debug.Log($"[GameUI] Timer update: Player={currentPlayer}, Time={_clueTimer:F2}s (was {oldTimer:F2}s)");
                    }
                    
                    if (_clueTimer <= 0f)
                    {
                        _clueTimer = 0f;
                        _clueTimerActive = false;
                        CSteamID expiredPlayer = _currentTimerPlayer; // Store for logging
                        // CRITICAL: Keep _currentTimerPlayer set to the expired player to prevent duplicate starts
                        // Only reset when the player actually changes (in OnClueSubmitted or when new player detected)
                        _lastTimerStartFrame = -1; // Reset frame tracking
                        Debug.Log($"[GameUI] ⏰ Timer expired for player {expiredPlayer} - keeping _currentTimerPlayer set to prevent duplicate start");
                        
                        // Hide timer UI when expired
                        if (timerText != null) timerText.gameObject.SetActive(false);
                        if (timerSlider != null) timerSlider.gameObject.SetActive(false);
                        
                        // Auto-submit only if it's local player's turn
                        if (isMyTurn && clueInputField != null && string.IsNullOrEmpty(clueInputField.text.Trim()))
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
                    else
                    {
                        // Timer is still active, update the UI
                        UpdateClueTimer();
                    }
                }
                // CRITICAL: If timer is active but player doesn't match, something is wrong - reset it
                else if (_clueTimerActive && currentPlayer.IsValid() && currentPlayer != _currentTimerPlayer)
                {
                    Debug.LogWarning($"[GameUI] ⚠️⚠️⚠️ TIMER MISMATCH! Timer active for {_currentTimerPlayer} ({_clueTimer:F2}s) but current player is {currentPlayer} - resetting timer");
                    _clueTimerActive = false;
                    _currentTimerPlayer = CSteamID.Nil;
                    _lastTimerStartFrame = -1;
                }
                else if (!currentPlayer.IsValid() || currentPlayer != _currentTimerPlayer)
                {
                    // No current player or player changed - hide timer
                    _clueTimerActive = false;
                    _currentTimerPlayer = CSteamID.Nil;
                    _lastTimerStartFrame = -1; // Reset frame tracking
                    if (timerText != null)
                    {
                        timerText.text = "";
                    }
                    if (timerSlider != null)
                    {
                        timerSlider.value = 0f;
                    }
                }
            }
            else if (GameManager.Instance != null && 
                     GameManager.Instance.CurrentState != GameManager.GameState.Voting &&
                     GameManager.Instance.CurrentState != GameManager.GameState.InGame)
            {
                // Clear timer when not in game or voting
                if (timerText != null)
                {
                    timerText.text = "";
                }
            }
        }
    }
}

