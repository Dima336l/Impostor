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
        [SerializeField] private TextMeshProUGUI myClueText; // UI text to show local player's clue (just the clue, no name)

        [Header("Turn Indicator")]
        [SerializeField] private TextMeshProUGUI currentPlayerText;
        [SerializeField] private TextMeshProUGUI roundText;

        [Header("Clue Input")]
        [SerializeField] private TMP_InputField clueInputField;
        [SerializeField] private Button submitClueButton;

        [Header("Clue Display - Dialogue Boxes")]
        [SerializeField] private Transform tableCenter; // Table center transform for positioning
        [SerializeField] private float tableRadius = 2f; // Distance from table center to player positions
        [SerializeField] private float dialogueBoxHeight = 2.5f; // Height above player position (above head)
        [SerializeField] private GameObject dialogueBoxPrefab; // Prefab for dialogue box (will create if null)
        [SerializeField] private Canvas worldSpaceCanvas; // World space canvas for dialogue boxes
        
        [Header("Manual Text Setup (Optional)")]
        [Tooltip("If you create text objects manually in Unity, assign them here. Leave empty to create automatically.")]
        [SerializeField] private TextMeshProUGUI[] manualClueTexts; // Array of text objects (one per player, in player order)

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
        private Dictionary<CSteamID, GameObject> _dialogueBoxes = new Dictionary<CSteamID, GameObject>(); // Dialogue boxes above players
        private Dictionary<CSteamID, Vector3> _playerPositions = new Dictionary<CSteamID, Vector3>(); // Player positions around table

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
            
            InitializeUI();
            InitializeDialogueBoxSystem();

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
            
            // Calculate player positions around the table for dialogue boxes
            CalculatePlayerPositions();
            
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
            
            // Always add clue display IMMEDIATELY - this creates dialogue box above player
            // This is called for every player's clue submission (local and others)
            // Must be called synchronously to appear instantly
            AddClueDisplay(playerID, clue);
            
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
            
             Debug.Log($"[GameUI] Finished refreshing clues - {_dialogueBoxes.Count} dialogue boxes in display");
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
            
            // Use dialogue box system instead of list
            CreateDialogueBox(playerID, clue, playerName);
        }

        private void ClearClues()
        {
            // Clear local player's clue UI
            if (myClueText != null)
            {
                myClueText.text = "";
                myClueText.gameObject.SetActive(false);
            }
            
            // If using manual setup, just hide the text objects
            if (manualClueTexts != null && manualClueTexts.Length > 0)
            {
                foreach (var text in manualClueTexts)
                {
                    if (text != null)
                    {
                        text.text = "";
                        text.gameObject.SetActive(false);
                    }
                }
                _dialogueBoxes.Clear();
                return;
            }
            
            // Otherwise, destroy programmatically created boxes
            foreach (var box in _dialogueBoxes.Values)
            {
                if (box != null)
                {
                    Destroy(box);
                }
            }
            _dialogueBoxes.Clear();
        }
        
        private void InitializeDialogueBoxSystem()
        {
            // Find table center - try TableSetup first, then tag, then create default
            if (tableCenter == null)
            {
                // Try to get from TableSetup component
                TableSetup tableSetup = FindFirstObjectByType<TableSetup>();
                if (tableSetup != null)
                {
                    tableCenter = tableSetup.GetTableCenter();
                    if (tableCenter != null)
                    {
                        Debug.Log("[GameUI] Found table center from TableSetup");
                    }
                }
                
                // Fallback: try to find by tag
                if (tableCenter == null)
                {
                    GameObject tableObj = GameObject.FindGameObjectWithTag("Table");
                    if (tableObj != null)
                    {
                        tableCenter = tableObj.transform;
                        Debug.Log("[GameUI] Found table center by tag");
                    }
                }
                
                // Last resort: create default
                if (tableCenter == null)
                {
                    GameObject defaultTable = new GameObject("TableCenter");
                    tableCenter = defaultTable.transform;
                    tableCenter.position = Vector3.zero;
                    Debug.Log("[GameUI] Created default table center at origin");
                }
            }
            
            // Get table radius from TableSetup if available
            TableSetup tableSetupForRadius = FindFirstObjectByType<TableSetup>();
            if (tableSetupForRadius != null)
            {
                tableRadius = tableSetupForRadius.GetTableRadius();
                Debug.Log($"[GameUI] Using table radius from TableSetup: {tableRadius}");
            }
            
            // Find or create world space canvas
            if (worldSpaceCanvas == null)
            {
                worldSpaceCanvas = FindFirstObjectByType<Canvas>();
                if (worldSpaceCanvas != null && worldSpaceCanvas.renderMode == RenderMode.WorldSpace)
                {
                    Debug.Log("[GameUI] Found existing world space canvas");
                }
                else
                {
                    // Create new world space canvas
                    GameObject canvasObj = new GameObject("WorldSpaceDialogueCanvas");
                    worldSpaceCanvas = canvasObj.AddComponent<Canvas>();
                    worldSpaceCanvas.renderMode = RenderMode.WorldSpace;
                    worldSpaceCanvas.worldCamera = UnityEngine.Camera.main;
                    
                    // Scale the canvas transform to make UI elements appear at reasonable size in world space
                    // 0.1 scale means 10px UI = 1 unit in world space
                    // With 500px text size, this gives us 50 units wide text (very visible and large)
                    canvasObj.transform.localScale = Vector3.one * 0.1f;
                    Debug.Log($"[GameUI] Created world space canvas with scale: {canvasObj.transform.localScale}");
                    
                    // Add CanvasScaler for proper sizing
                    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = 1f; // Use 1:1 pixel scale since we're scaling the transform
                    
                    // Add GraphicRaycaster
                    canvasObj.AddComponent<GraphicRaycaster>();
                    
                    Debug.Log("[GameUI] Created new world space canvas for dialogue boxes with scale 0.001");
                }
            }
        }
        
        private void CalculatePlayerPositions()
        {
            _playerPositions.Clear();
            
            if (tableCenter == null || GameManager.Instance?.PlayerManager == null)
            {
                Debug.LogWarning("[GameUI] Cannot calculate player positions - table center or player manager missing");
                return;
            }
            
            List<CSteamID> players = GameManager.Instance.PlayerManager.AllPlayers;
            int playerCount = players.Count;
            
            if (playerCount == 0)
            {
                return;
            }
            
            Vector3 tablePos = tableCenter.position;
            float angleStep = 360f / playerCount;
            
            for (int i = 0; i < playerCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 position = tablePos + new Vector3(
                    Mathf.Sin(angle) * tableRadius,
                    0f,
                    Mathf.Cos(angle) * tableRadius
                );
                
                _playerPositions[players[i]] = position;
                Debug.Log($"[GameUI] Player {players[i]} position: {position}");
            }
        }
        
        private void CreateDialogueBox(CSteamID playerID, string clue, string playerName)
        {
            Debug.Log($"[GameUI] CreateDialogueBox START: playerID={playerID}, clue='{clue}', playerName='{playerName}'");
            
            // Check if this is the local player - show clue in UI instead of above head
            CSteamID localPlayerID = Impostor.Steam.SteamManager.Instance?.LocalSteamID ?? CSteamID.Nil;
            if (playerID == localPlayerID)
            {
                // For local player, show just the clue in UI (not above head)
                if (myClueText != null)
                {
                    myClueText.text = clue; // Just the clue, no "Your Clue:" prefix
                    myClueText.gameObject.SetActive(true);
                }
                else
                {
                    // Create UI text if not assigned - place it in bottom left corner of screen
                    // Find or create a screen-space overlay canvas
                    Canvas screenCanvas = null;
                    Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    foreach (Canvas canvas in allCanvases)
                    {
                        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            screenCanvas = canvas;
                            break;
                        }
                    }
                    
                    // If no screen-space canvas found, create one
                    if (screenCanvas == null)
                    {
                        GameObject canvasObj = new GameObject("ScreenSpaceCanvas");
                        screenCanvas = canvasObj.AddComponent<Canvas>();
                        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvasObj.AddComponent<CanvasScaler>();
                        canvasObj.AddComponent<GraphicRaycaster>();
                    }
                    
                    GameObject clueUI = new GameObject("MyClueText");
                    clueUI.transform.SetParent(screenCanvas.transform, false);
                    RectTransform rect = clueUI.AddComponent<RectTransform>();
                    
                    // Anchor to bottom left corner
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0f, 0f);
                    rect.anchoredPosition = new Vector2(20f, 20f); // 20px from bottom left
                    rect.sizeDelta = new Vector2(400f, 60f);
                    
                    TextMeshProUGUI text = clueUI.AddComponent<TextMeshProUGUI>();
                    text.text = clue;
                    text.fontSize = 36f;
                    text.color = Color.yellow;
                    text.alignment = TextAlignmentOptions.Left;
                    text.fontStyle = FontStyles.Bold;
                    text.enableWordWrapping = false;
                    myClueText = text;
                    
                    Debug.Log($"[GameUI] Created MyClueText in bottom left corner on screen-space canvas");
                }
                Debug.Log($"[GameUI] Local player clue shown in UI: {clue}");
                return; // Don't create 3D text above head for local player
            }
            
            // Check if using manual text setup
            if (manualClueTexts != null && manualClueTexts.Length > 0)
            {
                UseManualTextSetup(playerID, clue, playerName);
                return;
            }
            
            // Ensure player positions are calculated
            if (_playerPositions.Count == 0)
            {
                Debug.Log("[GameUI] Player positions empty, calculating now...");
                CalculatePlayerPositions();
            }
            
            // Ensure world space canvas exists
            if (worldSpaceCanvas == null)
            {
                Debug.Log("[GameUI] World space canvas is null, initializing...");
                InitializeDialogueBoxSystem();
            }
            
            if (worldSpaceCanvas == null)
            {
                Debug.LogError("[GameUI] Cannot create dialogue box - world space canvas is null!");
                return;
            }
            
            Debug.Log($"[GameUI] Canvas found: {worldSpaceCanvas.name}, Scale: {worldSpaceCanvas.transform.localScale}, Camera: {worldSpaceCanvas.worldCamera?.name ?? "NULL"}");
            
            // Get player position for other players (local player handled above)
            Vector3 boxPosition = Vector3.zero;
            bool foundPosition = false;
            
            // For other players, try to find player marker
            if (!foundPosition)
            {
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.StartsWith("PlayerMarker_"))
                    {
                        // Check if this marker is near our calculated position
                        if (_playerPositions.ContainsKey(playerID))
                        {
                            Vector3 expectedPos = _playerPositions[playerID];
                            float distance = Vector3.Distance(obj.transform.position, expectedPos);
                            if (distance < 2f) // Within 2 units
                            {
                                // Position text container above player's head (0.8 units above marker - higher for name + clue)
                                boxPosition = obj.transform.position + Vector3.up * 0.8f;
                                foundPosition = true;
                                Debug.Log($"[GameUI] Found player marker {obj.name} at {obj.transform.position}, placing text container at {boxPosition}");
                                break;
                            }
                        }
                    }
                }
            }
            
            // Fallback to calculated position - higher for name + clue
            if (!foundPosition && _playerPositions.ContainsKey(playerID))
            {
                boxPosition = _playerPositions[playerID] + Vector3.up * 0.8f; // 0.8 units above (higher for name + clue)
                foundPosition = true;
                Debug.Log($"[GameUI] Using calculated position for player {playerID}: {boxPosition}");
            }
            
            if (!foundPosition)
            {
                Debug.LogWarning($"[GameUI] No position found for player {playerID}, cannot create dialogue box");
                return;
            }
            
            // Remove existing texts for this player
            if (_dialogueBoxes.TryGetValue(playerID, out GameObject existingText))
            {
                if (existingText != null)
                {
                    Destroy(existingText);
                }
                _dialogueBoxes.Remove(playerID);
            }
            
            // Create parent object to hold both name and clue text
            GameObject textContainer = new GameObject($"PlayerTextContainer_{playerID}");
            textContainer.transform.position = boxPosition;
            
            // Player name - positioned at the TOP (0.4 units above center)
            GameObject nameObj = new GameObject("PlayerName");
            nameObj.transform.SetParent(textContainer.transform);
            nameObj.transform.localPosition = Vector3.up * 0.4f; // TOP position
            
            TMPro.TextMeshPro nameMesh = nameObj.AddComponent<TMPro.TextMeshPro>();
            nameMesh.text = playerName;
            nameMesh.fontSize = 1.5f; // Slightly smaller for name
            nameMesh.alignment = TMPro.TextAlignmentOptions.Center;
            nameMesh.color = Color.white;
            nameMesh.fontStyle = TMPro.FontStyles.Bold;
            nameMesh.outlineWidth = 0.2f;
            nameMesh.outlineColor = Color.black;
            
            // Clue text - positioned at the BOTTOM (0.3 units below center)
            GameObject clueObj = new GameObject("ClueText");
            clueObj.transform.SetParent(textContainer.transform);
            clueObj.transform.localPosition = Vector3.down * 0.3f; // BOTTOM position
            
            TMPro.TextMeshPro clueMesh = clueObj.AddComponent<TMPro.TextMeshPro>();
            clueMesh.text = clue; // Show the clue word
            clueMesh.fontSize = 1.8f; // Slightly larger for clue
            clueMesh.alignment = TMPro.TextAlignmentOptions.Center;
            clueMesh.color = Color.yellow; // Yellow for clue
            clueMesh.fontStyle = TMPro.FontStyles.Bold;
            clueMesh.outlineWidth = 0.2f;
            clueMesh.outlineColor = Color.black;
            
            // Make container face camera (billboard effect)
            textContainer.transform.LookAt(UnityEngine.Camera.main.transform);
            textContainer.transform.Rotate(0f, 180f, 0f); // Flip to face camera
            
            _dialogueBoxes[playerID] = textContainer;
            Debug.Log($"[GameUI] ✓ Created player name '{playerName}' and clue '{clue}' above head at {boxPosition}");
        }
        
        private void UseManualTextSetup(CSteamID playerID, string clue, string playerName)
        {
            // Find player index
            List<CSteamID> players = GameManager.Instance?.PlayerManager?.AllPlayers;
            if (players == null || players.Count == 0)
            {
                Debug.LogWarning("[GameUI] Cannot use manual text setup - no players found");
                return;
            }
            
            int playerIndex = players.IndexOf(playerID);
            if (playerIndex < 0 || playerIndex >= manualClueTexts.Length)
            {
                Debug.LogWarning($"[GameUI] Player {playerID} index {playerIndex} out of range for manual texts (array size: {manualClueTexts.Length})");
                return;
            }
            
            TextMeshProUGUI textObj = manualClueTexts[playerIndex];
            if (textObj == null)
            {
                Debug.LogWarning($"[GameUI] Manual text object at index {playerIndex} is null");
                return;
            }
            
            // Update text
            textObj.text = clue;
            textObj.gameObject.SetActive(true);
            
            // Position above player marker if found
            GameObject playerMarker = GameObject.Find($"PlayerMarker_{playerIndex + 1}"); // Markers start at 1
            if (playerMarker != null)
            {
                Vector3 markerPos = playerMarker.transform.position;
                textObj.transform.position = markerPos + Vector3.up * 2.5f;
                Debug.Log($"[GameUI] Positioned manual text for {playerName} above marker at {textObj.transform.position}");
            }
            
            // Store reference
            if (!_dialogueBoxes.ContainsKey(playerID))
            {
                _dialogueBoxes[playerID] = textObj.gameObject;
            }
            
            Debug.Log($"[GameUI] ✓ Updated manual text for {playerName} with clue: '{clue}'");
        }
        
        private void UpdateDialogueBoxRotation(RectTransform rect)
        {
            if (rect == null || UnityEngine.Camera.main == null) return;
            rect.LookAt(UnityEngine.Camera.main.transform);
            rect.Rotate(0f, 180f, 0f); // Flip to face camera correctly
        }
        
        private void UpdateDialogueBoxRotation(Transform transform)
        {
            if (transform == null || UnityEngine.Camera.main == null) return;
            transform.LookAt(UnityEngine.Camera.main.transform);
            transform.Rotate(0f, 180f, 0f); // Flip to face camera correctly
        }
        
        private void UpdateDialogueBoxes()
        {
            if (UnityEngine.Camera.main == null) return;
            
            // Update 3D text above other players' heads (local player doesn't have 3D text)
            foreach (var kvp in _dialogueBoxes)
            {
                if (kvp.Value == null) continue;
                
                // Make text face camera (billboard effect)
                kvp.Value.transform.LookAt(UnityEngine.Camera.main.transform);
                kvp.Value.transform.Rotate(0f, 180f, 0f); // Flip to face camera
            }
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
        
        private System.Collections.IEnumerator AdjustDialogueBoxSize(GameObject dialogueBox, TextMeshProUGUI text)
        {
            yield return null; // Wait one frame for text to calculate
            yield return null; // Wait another frame to be sure
            
            if (dialogueBox == null || text == null) yield break;
            
            RectTransform rect = dialogueBox.GetComponent<RectTransform>();
            if (rect == null) yield break;
            
            // Get the preferred height of the text
            float preferredHeight = text.preferredHeight;
            float preferredWidth = text.preferredWidth;
            
            // Set box height to fit text (with padding) - keep it SMALL and compact
            float boxHeight = Mathf.Max(40f, preferredHeight + 16f); // Minimum 40px, add 16px padding
            float boxWidth = Mathf.Min(250f, Mathf.Max(200f, preferredWidth + 20f)); // Between 200-250px max, add 20px padding
            
            rect.sizeDelta = new Vector2(boxWidth, boxHeight);
            
            // Force layout update
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            
            Debug.Log($"[GameUI] Adjusted dialogue box size: {boxWidth}x{boxHeight} for text: {text.text}");
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
            
            UpdateClueTimer();
            UpdateTurnIndicator();
            UpdateDialogueBoxes(); // Keep dialogue boxes facing camera
            
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

