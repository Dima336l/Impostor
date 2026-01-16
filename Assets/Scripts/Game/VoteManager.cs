using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Steamworks;
using Impostor.Game;
using Impostor.Networking;

namespace Impostor.Game
{
    /// <summary>
    /// Manages voting system for identifying the Impostor.
    /// </summary>
    public class VoteManager : MonoBehaviour
    {
        private static VoteManager _instance;
        public static VoteManager Instance
        {
            get
            {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<VoteManager>();
            }
                return _instance;
            }
        }

        private PlayerManager _playerManager;
        private Dictionary<CSteamID, CSteamID> _votes = new Dictionary<CSteamID, CSteamID>(); // Voter -> Voted For
        private bool _votingInProgress = false;
        private float _votingTimer = 0f;
        private float _votingDuration = 60f; // 60 seconds to vote

        public bool VotingInProgress => _votingInProgress;
        public float VotingTimeRemaining => Mathf.Max(0f, _votingDuration - _votingTimer);

        public event Action<CSteamID, CSteamID> OnVoteCast;
        public event Action OnVotingStarted;
        public event Action<CSteamID, bool> OnVotingEnded; // Voted out player, was impostor

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Update()
        {
            if (_votingInProgress)
            {
                _votingTimer += Time.deltaTime;
                if (_votingTimer >= _votingDuration)
                {
                    EndVoting();
                }
            }
        }

        public void Initialize(PlayerManager playerManager)
        {
            _playerManager = playerManager;
        }

        public void StartVoting(float duration = 60f)
        {
            if (_playerManager == null)
            {
                Debug.LogError("PlayerManager not initialized");
                return;
            }

            _votingInProgress = true;
            _votingTimer = 0f;
            _votingDuration = duration;
            _votes.Clear();

            // Reset vote states
            foreach (var player in _playerManager.AllPlayers)
            {
                PlayerData playerData = _playerManager.GetPlayer(player);
                if (playerData != null)
                {
                    playerData.HasVoted = false;
                    playerData.VoteTarget = CSteamID.Nil;
                }
            }

            OnVotingStarted?.Invoke();
            Debug.Log("Voting phase started");
        }

        public void CastVote(CSteamID voterID, CSteamID targetID)
        {
            if (!_votingInProgress)
            {
                Debug.LogWarning("Voting not in progress");
                return;
            }

            if (!_playerManager.HasPlayer(voterID))
            {
                Debug.LogWarning($"Voter {voterID} not in game");
                return;
            }

            if (!_playerManager.HasPlayer(targetID) && targetID != CSteamID.Nil)
            {
                Debug.LogWarning($"Target {targetID} not in game");
                return;
            }

            PlayerData voter = _playerManager.GetPlayer(voterID);
            if (voter == null || voter.HasVoted)
            {
                Debug.LogWarning("Player already voted");
                return;
            }

            // Allow voting for "No one" (CSteamID.Nil)
            voter.HasVoted = true;
            voter.VoteTarget = targetID;
            _votes[voterID] = targetID;

            // Broadcast vote
            VoteSubmittedMessage message = new VoteSubmittedMessage
            {
                VoterSteamID = voterID.m_SteamID,
                VotedForSteamID = targetID.m_SteamID
            };
            NetworkManager.Instance.BroadcastMessage(message);

            OnVoteCast?.Invoke(voterID, targetID);

            // Check if all players voted
            if (AllPlayersVoted())
            {
                EndVoting();
            }
        }

        private bool AllPlayersVoted()
        {
            foreach (CSteamID playerID in _playerManager.AllPlayers)
            {
                PlayerData player = _playerManager.GetPlayer(playerID);
                if (player == null || !player.HasVoted)
                {
                    return false;
                }
            }
            return true;
        }

        public void EndVoting()
        {
            if (!_votingInProgress)
            {
                return;
            }

            _votingInProgress = false;

            // Count votes
            Dictionary<CSteamID, int> voteCounts = new Dictionary<CSteamID, int>();
            
            foreach (CSteamID targetID in _votes.Values)
            {
                if (targetID == CSteamID.Nil) continue; // Skip "no one" votes

                if (voteCounts.ContainsKey(targetID))
                {
                    voteCounts[targetID]++;
                }
                else
                {
                    voteCounts[targetID] = 1;
                }
            }

            // Find player with most votes
            CSteamID votedOut = CSteamID.Nil;
            int maxVotes = 0;

            foreach (var kvp in voteCounts)
            {
                if (kvp.Value > maxVotes)
                {
                    maxVotes = kvp.Value;
                    votedOut = kvp.Key;
                }
            }

            // Check for ties
            List<CSteamID> tiedPlayers = voteCounts
                .Where(kvp => kvp.Value == maxVotes && maxVotes > 0)
                .Select(kvp => kvp.Key)
                .ToList();

            if (tiedPlayers.Count > 1)
            {
                // Tie - no one is voted out
                votedOut = CSteamID.Nil;
                Debug.Log("Vote resulted in a tie. No one is voted out.");
            }

            bool wasImpostor = false;
            if (votedOut != CSteamID.Nil)
            {
                PlayerData player = _playerManager.GetPlayer(votedOut);
                wasImpostor = player != null && player.Role == PlayerRole.Impostor;
            }

            // Broadcast result
            RoundEndMessage message = new RoundEndMessage
            {
                VotedOutSteamID = votedOut.m_SteamID,
                WasImpostor = wasImpostor
            };
            NetworkManager.Instance.BroadcastMessage(message);

            OnVotingEnded?.Invoke(votedOut, wasImpostor);
            Debug.Log($"Voting ended. Voted out: {votedOut} (Impostor: {wasImpostor})");
        }

        public Dictionary<CSteamID, int> GetVoteCounts()
        {
            Dictionary<CSteamID, int> voteCounts = new Dictionary<CSteamID, int>();

            foreach (CSteamID targetID in _votes.Values)
            {
                if (targetID == CSteamID.Nil) continue;

                if (voteCounts.ContainsKey(targetID))
                {
                    voteCounts[targetID]++;
                }
                else
                {
                    voteCounts[targetID] = 1;
                }
            }

            return voteCounts;
        }

        public CSteamID GetVote(CSteamID voterID)
        {
            _votes.TryGetValue(voterID, out CSteamID target);
            return target;
        }
    }
}
