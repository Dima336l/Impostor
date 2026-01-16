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
    /// Manages clue rounds, turn order, and round progression.
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        private static RoundManager _instance;
        public static RoundManager Instance
        {
            get
            {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<RoundManager>();
            }
                return _instance;
            }
        }

        private GameManager _gameManager;
        private PlayerManager _playerManager;
        private string _currentSecretWord;
        private int _currentRound = 0;
        private int _currentPlayerIndex = 0;
        private List<CSteamID> _turnOrder = new List<CSteamID>();
        private Dictionary<CSteamID, string> _clues = new Dictionary<CSteamID, string>();
        private bool _roundInProgress = false;

        public int CurrentRound => _currentRound;
        public string CurrentSecretWord => _currentSecretWord;
        public CSteamID CurrentPlayer => _turnOrder.Count > 0 && _currentPlayerIndex < _turnOrder.Count 
            ? _turnOrder[_currentPlayerIndex] 
            : CSteamID.Nil;
        public bool RoundInProgress => _roundInProgress;

        public event Action<int, string> OnRoundStarted;
        public event Action<CSteamID, string> OnClueSubmitted;
        public event Action OnAllCluesSubmitted;
        public event Action OnRoundEnded;

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

            _gameManager = GetComponent<GameManager>();
            if (_gameManager == null)
            {
                _gameManager = FindFirstObjectByType<GameManager>();
            }
        }

        public void Initialize(PlayerManager playerManager)
        {
            _playerManager = playerManager;
        }

        public void StartRound()
        {
            if (_playerManager == null || _playerManager.PlayerCount < 4)
            {
                Debug.LogError("Not enough players to start a round");
                return;
            }

            _currentRound++;
            _roundInProgress = true;
            _clues.Clear();
            _currentPlayerIndex = 0;

            // Get secret word
            _currentSecretWord = WordManager.Instance.GetRandomWord();

            // Set up turn order (randomize)
            _turnOrder = new List<CSteamID>(_playerManager.AllPlayers);
            Shuffle(_turnOrder);

            // Reset player states
            _playerManager.ResetRoundState();

            // Distribute words to players
            DistributeWords();

            OnRoundStarted?.Invoke(_currentRound, _currentSecretWord);
            Debug.Log($"Round {_currentRound} started. Secret word: {_currentSecretWord}");
        }

        private void DistributeWords()
        {
            List<CSteamID> impostors = _playerManager.GetImpostors();
            List<CSteamID> civilians = _playerManager.GetCivilians();

            // Send word to civilians
            foreach (CSteamID civilianID in civilians)
            {
                WordAssignedMessage message = new WordAssignedMessage
                {
                    PlayerSteamID = civilianID.m_SteamID,
                    Word = _currentSecretWord,
                    IsImpostor = false
                };
                NetworkManager.Instance.SendMessage(message, civilianID);
            }

            // Send "IMPOSTOR" to impostors
            foreach (CSteamID impostorID in impostors)
            {
                WordAssignedMessage message = new WordAssignedMessage
                {
                    PlayerSteamID = impostorID.m_SteamID,
                    Word = "IMPOSTOR",
                    IsImpostor = true
                };
                NetworkManager.Instance.SendMessage(message, impostorID);
            }
        }

        public void SubmitClue(CSteamID playerID, string clue)
        {
            if (!_roundInProgress)
            {
                Debug.LogWarning("Round not in progress. Cannot submit clue.");
                return;
            }

            if (CurrentPlayer != playerID)
            {
                Debug.LogWarning($"Not {playerID}'s turn. Current player: {CurrentPlayer}");
                return;
            }

            if (string.IsNullOrEmpty(clue) || clue.Length > 50) // Basic validation
            {
                Debug.LogWarning("Invalid clue. Must be 1-50 characters.");
                return;
            }

            PlayerData player = _playerManager.GetPlayer(playerID);
            if (player == null || player.HasSubmittedClue)
            {
                Debug.LogWarning("Player already submitted clue or doesn't exist.");
                return;
            }

            player.HasSubmittedClue = true;
            player.Clue = clue;
            _clues[playerID] = clue;

            // Broadcast clue to all players
            ClueSubmittedMessage message = new ClueSubmittedMessage
            {
                PlayerSteamID = playerID.m_SteamID,
                Clue = clue
            };
            NetworkManager.Instance.BroadcastMessage(message);

            OnClueSubmitted?.Invoke(playerID, clue);

            // Move to next player
            _currentPlayerIndex++;

            // Check if all clues submitted
            if (_currentPlayerIndex >= _turnOrder.Count)
            {
                EndCluePhase();
            }
        }

        private void EndCluePhase()
        {
            _roundInProgress = false;
            OnAllCluesSubmitted?.Invoke();
            Debug.Log("All clues submitted. Moving to voting phase.");
        }

        public void EndRound()
        {
            _roundInProgress = false;
            _clues.Clear();
            OnRoundEnded?.Invoke();
        }

        public Dictionary<CSteamID, string> GetAllClues()
        {
            return new Dictionary<CSteamID, string>(_clues);
        }

        private void Shuffle<T>(List<T> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
