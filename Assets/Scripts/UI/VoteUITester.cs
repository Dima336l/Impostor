using System;
using UnityEngine;
using Steamworks;
using Impostor.Game;
using Impostor.Steam;

namespace Impostor.UI
{
    /// <summary>
    /// Helper script to test full game round with dummy players.
    /// Attach to any GameObject in GameTable scene.
    /// Press 'V' to start a full round (clues + voting).
    /// </summary>
    public class VoteUITester : MonoBehaviour
    {
        [Header("Testing")]
        [SerializeField] private int dummyPlayerCount = 4;
        [SerializeField] private bool autoStartRound = true;
        [SerializeField] private float clueTimePerPlayer = 5f; // 5 seconds per player to submit clue

        private void Update()
        {
            // Press 'V' to test full round
            if (Input.GetKeyDown(KeyCode.V))
            {
                TestFullRound();
            }
        }

        private void TestFullRound()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager not found!");
                return;
            }

            Debug.Log("Setting up test voting...");

            // Ensure GameManager is initialized as host for testing
            if (GameManager.Instance.PlayerManager == null)
            {
                Debug.LogError("PlayerManager not initialized!");
                return;
            }

            // Set GameManager as host for testing
            GameManager.Instance.SetAsHost(true);
            Debug.Log("Set GameManager as host for testing");

            // Add dummy players if needed
            if (GameManager.Instance.PlayerManager.PlayerCount < dummyPlayerCount)
            {
                AddDummyPlayers();
            }

            // Ensure managers are initialized
            if (GameManager.Instance.VoteManager != null)
            {
                GameManager.Instance.VoteManager.Initialize(GameManager.Instance.PlayerManager);
                Debug.Log("VoteManager initialized");
            }
            else
            {
                Debug.LogError("VoteManager is null!");
            }

            if (GameManager.Instance.RoundManager != null)
            {
                GameManager.Instance.RoundManager.Initialize(GameManager.Instance.PlayerManager);
                Debug.Log("RoundManager initialized");
            }

            // Start full round (clues then voting)
            if (autoStartRound)
            {
                StartCoroutine(SimulateFullRound());
            }
        }
        
        private System.Collections.IEnumerator SimulateFullRound()
        {
            Debug.Log("=== Starting Full Round Simulation ===");
            
            // Step 1: Assign roles and enter GameStarting state (which triggers DraftPhase)
            if (GameManager.Instance.IsHost)
            {
                int impostorCount = Math.Max(1, GameManager.Instance.PlayerManager.PlayerCount / 4);
                GameManager.Instance.PlayerManager.AssignRoles(impostorCount);
                Debug.Log("Roles assigned, entering draft phase...");
            }
            
            // Step 2: Enter GameStarting state (which will transition to DraftPhase)
            GameManager.Instance.ChangeState(GameManager.GameState.GameStarting);
            yield return new UnityEngine.WaitForSeconds(0.5f);
            
            // Step 3: Wait for draft phase to be active
            int waitCount = 0;
            while (GameManager.Instance.CurrentState != GameManager.GameState.DraftPhase && waitCount < 50)
            {
                yield return new UnityEngine.WaitForSeconds(0.1f);
                waitCount++;
            }
            
            if (GameManager.Instance.CurrentState != GameManager.GameState.DraftPhase)
            {
                Debug.LogError("Draft phase did not start!");
                yield break;
            }
            
            Debug.Log("Draft phase active - showing word/role to players");
            
            // Step 4: Auto-acknowledge for all players (including dummy players)
            yield return StartCoroutine(AutoAcknowledgeDraft());
            
            // Step 5: Wait for draft phase to complete and transition to InGame
            waitCount = 0;
            while (GameManager.Instance.CurrentState != GameManager.GameState.InGame && waitCount < 100)
            {
                yield return new UnityEngine.WaitForSeconds(0.1f);
                waitCount++;
            }
            
            if (GameManager.Instance.CurrentState != GameManager.GameState.InGame)
            {
                Debug.LogError("Did not transition to InGame state after draft phase!");
                yield break;
            }
            
            Debug.Log("Draft phase complete, starting clue submission phase...");
            
            // Step 6: Auto-submit clues for all players (5 seconds per player)
            yield return StartCoroutine(AutoSubmitClues());
            
            // Step 7: Wait for voting phase to start
            yield return new UnityEngine.WaitForSeconds(1f);
            
            Debug.Log("=== Full Round Simulation Complete ===");
        }
        
        private System.Collections.IEnumerator AutoAcknowledgeDraft()
        {
            var playerManager = GameManager.Instance.PlayerManager;
            var localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;
            
            if (playerManager == null)
            {
                Debug.LogError("PlayerManager is null!");
                yield break;
            }
            
            Debug.Log("Auto-acknowledging draft for all players...");
            
            // Wait a bit for word assignment messages to be received and UI to update
            yield return new UnityEngine.WaitForSeconds(1f);
            
            // Acknowledge for dummy players first (quickly)
            var allPlayers = playerManager.AllPlayers;
            foreach (var playerID in allPlayers)
            {
                if (playerID == localID) continue; // Skip local player for now
                
                var playerData = playerManager.GetPlayer(playerID);
                if (playerData != null && !playerData.HasAcknowledgedDraft)
                {
                    if (GameManager.Instance.IsHost)
                    {
                        // Host can directly acknowledge
                        GameManager.Instance.OnDraftAcknowledged(playerID);
                        Debug.Log($"Auto-acknowledged draft for {playerData.PlayerName}");
                    }
                    
                    // Small delay between acknowledgments
                    yield return new UnityEngine.WaitForSeconds(0.2f);
                }
            }
            
            // Wait a bit longer for local player to see their word/role (3 seconds)
            Debug.Log("Waiting 3 seconds for local player to see their word/role...");
            yield return new UnityEngine.WaitForSeconds(3f);
            
            // Now acknowledge for local player
            var localPlayerData = playerManager.GetPlayer(localID);
            if (localPlayerData != null && !localPlayerData.HasAcknowledgedDraft)
            {
                if (GameManager.Instance.IsHost)
                {
                    GameManager.Instance.OnDraftAcknowledged(localID);
                    Debug.Log($"Auto-acknowledged draft for local player: {localPlayerData.PlayerName}");
                }
            }
            
            Debug.Log("All players have acknowledged draft phase");
        }
        
        private System.Collections.IEnumerator AutoSubmitClues()
        {
            var roundManager = GameManager.Instance.RoundManager;
            var playerManager = GameManager.Instance.PlayerManager;
            var localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;
            
            if (roundManager == null || playerManager == null)
            {
                Debug.LogError("RoundManager or PlayerManager is null!");
                yield break;
            }
            
            Debug.Log("Starting auto-clue submission phase...");
            
            // Wait for round to be ready
            int waitCount = 0;
            while (!roundManager.RoundInProgress && waitCount < 50) // Wait up to 5 seconds
            {
                yield return new UnityEngine.WaitForSeconds(0.1f);
                waitCount++;
            }
            
            if (!roundManager.RoundInProgress)
            {
                Debug.LogError("Round did not start! RoundInProgress is still false.");
                yield break;
            }
            
            Debug.Log($"Round is in progress. Current player: {roundManager.CurrentPlayer}");
            
            // Get turn order - use the turn order from RoundManager
            var allPlayers = playerManager.AllPlayers;
            Debug.Log($"Total players: {allPlayers.Count}, Round turn order should be set");
            
            // Process each player's turn - wait for each turn
            int maxTurns = allPlayers.Count;
            int turnCount = 0;
            
            while (roundManager.RoundInProgress && turnCount < maxTurns)
            {
                CSteamID currentPlayerID = roundManager.CurrentPlayer;
                
                if (!currentPlayerID.IsValid())
                {
                    Debug.LogWarning("Current player is invalid, waiting...");
                    yield return new UnityEngine.WaitForSeconds(0.1f);
                    continue;
                }
                
                var playerData = playerManager.GetPlayer(currentPlayerID);
                if (playerData == null)
                {
                    Debug.LogError($"Player data not found for {currentPlayerID}");
                    yield return new UnityEngine.WaitForSeconds(0.1f);
                    continue;
                }
                
                if (playerData.HasSubmittedClue)
                {
                    Debug.Log($"{playerData.PlayerName} already submitted, waiting for next turn...");
                    yield return new UnityEngine.WaitForSeconds(0.1f);
                    continue;
                }
                
                Debug.Log($"It's {playerData.PlayerName}'s turn to submit clue (5 seconds)");
                
                // Generate a random clue based on role
                string clue = GenerateClue(playerData.Role, roundManager.CurrentSecretWord);
                
                // Wait for timer to expire (exactly 5 seconds) then submit immediately
                if (currentPlayerID == localID)
                {
                    Debug.Log($"Local player's turn - you have {clueTimePerPlayer} seconds to input your clue!");
                    // Wait the full 5 seconds - timer will show countdown
                    yield return new UnityEngine.WaitForSeconds(clueTimePerPlayer);
                    
                    // Check if local player submitted manually
                    if (!playerData.HasSubmittedClue && GameManager.Instance.IsHost)
                    {
                        // Auto-submit for local player if they didn't
                        roundManager.SubmitClue(currentPlayerID, clue);
                        Debug.Log($"Auto-submitted clue for local player: {clue}");
                    }
                }
                else
                {
                    // For dummy players: Wait for timer to expire then submit IMMEDIATELY
                    // Poll every 0.05 seconds (50ms) to check timer remaining, submit instantly when <= 0
                    float waitTime = 0f;
                    GameUI gameUI = FindFirstObjectByType<GameUI>();
                    
                    while (waitTime < clueTimePerPlayer + 0.2f && !playerData.HasSubmittedClue) // Small buffer
                    {
                        yield return new UnityEngine.WaitForSeconds(0.05f); // Check every 50ms for instant response
                        waitTime += 0.05f;
                        
                        // Check timer remaining time - submit when timer reaches 0 or is very close
                        if (gameUI != null)
                        {
                            float timerRemaining = gameUI.ClueTimerRemaining;
                            
                            // Submit immediately when timer is at or below 0.1s (essentially expired)
                            if (timerRemaining <= 0.1f)
                            {
                                // Timer expired or about to expire - submit IMMEDIATELY, no additional delay
                                if (GameManager.Instance.IsHost && !playerData.HasSubmittedClue)
                                {
                                    roundManager.SubmitClue(currentPlayerID, clue);
                                    Debug.Log($"Auto-submitted clue for {playerData.PlayerName}: {clue} (instant submit when timer at {timerRemaining:F2}s, waited {waitTime:F2}s)");
                                }
                                break; // Exit loop immediately after submission
                            }
                        }
                    }
                    
                    // Fallback: If timer check didn't work, submit after 5 seconds anyway
                    if (!playerData.HasSubmittedClue && GameManager.Instance.IsHost)
                    {
                        roundManager.SubmitClue(currentPlayerID, clue);
                        Debug.Log($"Auto-submitted clue for {playerData.PlayerName}: {clue} (fallback after {waitTime:F2}s)");
                    }
                }
                
                // Wait a bit before next turn
                yield return new UnityEngine.WaitForSeconds(0.5f);
            }
            
            Debug.Log("All clues submitted! Moving to voting phase...");
        }
        
        private string GenerateClue(PlayerRole role, string secretWord)
        {
            if (role == PlayerRole.Impostor)
            {
                // Impostor doesn't know the word, so give a generic clue
                string[] impostorClues = { "suspicious", "unclear", "maybe", "possibly", "could be", "not sure" };
                return impostorClues[UnityEngine.Random.Range(0, impostorClues.Length)];
            }
            else
            {
                // Civilian knows the word, give a related clue
                // Simple clue generation - just use first 3 letters or a related word
                if (secretWord.Length >= 3)
                {
                    return secretWord.Substring(0, Mathf.Min(3, secretWord.Length)).ToUpper();
                }
                return secretWord;
            }
        }

        private void AddDummyPlayers()
        {
            var playerManager = GameManager.Instance.PlayerManager;
            var localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;

            // Add local player if not already added
            if (!playerManager.HasPlayer(localID))
            {
                string localName = SteamFriends.GetPersonaName();
                playerManager.AddPlayer(localID, localName);
                Debug.Log($"Added local player: {localName}");
            }

            // Add dummy players
            for (int i = 1; i < dummyPlayerCount; i++)
            {
                // Create fake Steam IDs (using local ID + offset)
                ulong fakeSteamID = localID.m_SteamID + (ulong)(i * 1000);
                CSteamID dummyID = new CSteamID(fakeSteamID);
                
                if (!playerManager.HasPlayer(dummyID))
                {
                    string dummyName = $"DummyPlayer{i}";
                    playerManager.AddPlayer(dummyID, dummyName);
                    Debug.Log($"Added dummy player: {dummyName} (ID: {dummyID})");
                }
            }

            // Assign roles (1 impostor, rest civilians)
            if (GameManager.Instance.IsHost)
            {
                playerManager.AssignRoles(1); // 1 impostor
                Debug.Log("Assigned roles to test players");
            }
        }
        
        // Auto-vote for dummy players after local player votes
        private void OnEnable()
        {
            if (GameManager.Instance != null && GameManager.Instance.VoteManager != null)
            {
                GameManager.Instance.VoteManager.OnVoteCast += OnVoteCast;
            }
        }
        
        private void OnDisable()
        {
            if (GameManager.Instance != null && GameManager.Instance.VoteManager != null)
            {
                GameManager.Instance.VoteManager.OnVoteCast -= OnVoteCast;
            }
        }
        
        private void OnVoteCast(Steamworks.CSteamID voterID, Steamworks.CSteamID targetID)
        {
            // If local player voted, auto-vote for dummy players after a short delay
            var localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;
            if (voterID == localID)
            {
                StartCoroutine(AutoVoteDummyPlayers());
            }
        }
        
        private System.Collections.IEnumerator AutoVoteDummyPlayers()
        {
            yield return new UnityEngine.WaitForSeconds(1f); // Wait 1 second
            
            var voteManager = GameManager.Instance.VoteManager;
            var playerManager = GameManager.Instance.PlayerManager;
            var localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;
            
            if (voteManager == null || playerManager == null || !voteManager.VotingInProgress)
            {
                yield break;
            }
            
            // Get all dummy players (not local player)
            var allPlayers = playerManager.AllPlayers;
            var dummyPlayers = new System.Collections.Generic.List<Steamworks.CSteamID>();
            
            Debug.Log($"Total players: {allPlayers.Count}");
            
            foreach (var playerID in allPlayers)
            {
                if (playerID != localID)
                {
                    var playerData = playerManager.GetPlayer(playerID);
                    if (playerData != null && !playerData.HasVoted)
                    {
                        dummyPlayers.Add(playerID);
                        Debug.Log($"Found dummy player to vote: {playerData.PlayerName}");
                    }
                }
            }
            
            Debug.Log($"Auto-voting for {dummyPlayers.Count} dummy players");
            
            // Auto-vote for dummy players (randomly vote for other players or no vote)
            foreach (var dummyID in dummyPlayers)
            {
                if (!voteManager.VotingInProgress)
                {
                    Debug.Log("Voting ended, stopping auto-vote");
                    break;
                }
                
                // Random vote: 70% chance to vote for a random player, 30% no vote
                Steamworks.CSteamID voteTarget = Steamworks.CSteamID.Nil;
                
                if (UnityEngine.Random.Range(0f, 1f) < 0.7f && allPlayers.Count > 1)
                {
                    // Vote for a random other player (not self)
                    var candidates = new System.Collections.Generic.List<Steamworks.CSteamID>();
                    foreach (var candidateID in allPlayers)
                    {
                        if (candidateID != dummyID)
                        {
                            candidates.Add(candidateID);
                        }
                    }
                    
                    if (candidates.Count > 0)
                    {
                        voteTarget = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    }
                }
                
                // Cast vote for dummy player
                if (GameManager.Instance.IsHost)
                {
                    voteManager.CastVote(dummyID, voteTarget);
                    var playerData = playerManager.GetPlayer(dummyID);
                    string targetName = voteTarget == Steamworks.CSteamID.Nil ? "No Vote" : playerManager.GetPlayer(voteTarget)?.PlayerName ?? "Unknown";
                    Debug.Log($"Auto-voted for dummy player {playerData?.PlayerName}: {targetName}");
                }
                else
                {
                    Debug.LogWarning("Not host, cannot cast votes for dummy players");
                }
                
                yield return new UnityEngine.WaitForSeconds(0.5f); // Space out votes
            }
            
            Debug.Log("Finished auto-voting for all dummy players");
        }
    }
}
