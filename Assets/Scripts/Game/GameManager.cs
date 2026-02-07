using System;
using UnityEngine;
using Steamworks;
using Impostor.Steam;
using Impostor.Networking;
using Impostor.Game;

namespace Impostor.Game
{
    /// <summary>
    /// Main game state manager. Controls the overall game flow and state transitions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public enum GameState
        {
            MainMenu,
            Lobby,
            WaitingForReady,
            GameStarting,
            InGame,
            Voting,
            RoundResults,
            GameEnd
        }

        private GameState _currentState = GameState.MainMenu;
        public GameState CurrentState => _currentState;

        private PlayerManager _playerManager;
        private RoundManager _roundManager;
        private VoteManager _voteManager;
        private bool _isHost = false;
        private int _roundsPlayed = 0;
        private int _maxRounds = 3;

        public bool IsHost => _isHost;
        public PlayerManager PlayerManager => _playerManager;
        public RoundManager RoundManager => _roundManager;
        public VoteManager VoteManager => _voteManager;

        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _playerManager = new PlayerManager();
            _roundManager = GetComponent<RoundManager>();
            if (_roundManager == null)
            {
                _roundManager = gameObject.AddComponent<RoundManager>();
            }
            _voteManager = GetComponent<VoteManager>();
            if (_voteManager == null)
            {
                _voteManager = gameObject.AddComponent<VoteManager>();
            }

            _roundManager.Initialize(_playerManager);
            _voteManager.Initialize(_playerManager);

            // Subscribe to round and vote events
            if (_roundManager != null)
            {
                _roundManager.OnAllCluesSubmitted += OnAllCluesSubmitted;
            }
            if (_voteManager != null)
            {
                _voteManager.OnVotingEnded += OnVotingEnded;
            }

            // Subscribe to Steam events
            if (SteamLobbyManager.Instance != null)
            {
                SteamLobbyManager.Instance.OnPlayerJoined += OnPlayerJoinedLobby;
                SteamLobbyManager.Instance.OnPlayerLeft += OnPlayerLeftLobby;
            }

            // Subscribe to network messages
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.RegisterMessageHandler(NetworkMessage.MessageType.ReadyState, HandleReadyState);
                NetworkManager.Instance.RegisterMessageHandler(NetworkMessage.MessageType.ClueSubmitted, HandleClueSubmitted);
                NetworkManager.Instance.RegisterMessageHandler(NetworkMessage.MessageType.VoteSubmitted, HandleVoteSubmitted);
            }

            ChangeState(GameState.MainMenu);
        }

        public void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;

            Debug.Log($"Game state changed: {_currentState} -> {newState}");
            _currentState = newState;
            OnStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Lobby:
                    OnEnterLobby();
                    break;
                case GameState.WaitingForReady:
                    OnEnterWaitingForReady();
                    break;
                case GameState.GameStarting:
                    OnEnterGameStarting();
                    break;
                case GameState.InGame:
                    OnEnterInGame();
                    break;
                case GameState.Voting:
                    OnEnterVoting();
                    break;
                case GameState.RoundResults:
                    OnEnterRoundResults();
                    break;
                case GameState.GameEnd:
                    OnEnterGameEnd();
                    break;
            }
        }

        public void SetAsHost(bool isHost)
        {
            _isHost = isHost;
        }

        private void OnEnterLobby()
        {
            if (_isHost && SteamLobbyManager.Instance != null && SteamLobbyManager.Instance.IsInLobby)
            {
                // Initialize network connections
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.InitializeNetworkConnections();
                }
            }

            // Update rich presence
            if (SteamRichPresence.Instance != null)
            {
                int playerCount = _playerManager != null ? _playerManager.PlayerCount : 0;
                SteamRichPresence.Instance.SetInLobby(playerCount, 6);
            }
        }

        private void OnEnterWaitingForReady()
        {
            // Wait for all players to be ready
            if (_isHost)
            {
                CheckAllPlayersReady();
            }
        }

        private void OnEnterGameStarting()
        {
            if (_isHost)
            {
                // Assign roles
                int impostorCount = Math.Max(1, _playerManager.PlayerCount / 4); // 1 impostor per 4 players
                _playerManager.AssignRoles(impostorCount);

                // Start first round
                _roundsPlayed = 0;
                StartNextRound();
            }
        }

        private void OnEnterInGame()
        {
            // Round is in progress, waiting for clues
            if (SteamRichPresence.Instance != null && _roundManager != null)
            {
                SteamRichPresence.Instance.SetInGame(_roundManager.CurrentRound, _maxRounds);
            }
        }

        private void OnEnterVoting()
        {
            if (_isHost && _voteManager != null)
            {
                _voteManager.StartVoting(5f);
            }

            if (SteamRichPresence.Instance != null)
            {
                SteamRichPresence.Instance.SetVoting();
            }
        }

        private void OnEnterRoundResults()
        {
            // Show results, then decide next action
        }

        private void OnEnterGameEnd()
        {
            // Show final results
        }

        public void StartGame()
        {
            if (!_isHost)
            {
                Debug.LogWarning("Only host can start the game");
                return;
            }

            if (_playerManager.PlayerCount < 4)
            {
                Debug.LogWarning("Need at least 4 players to start");
                return;
            }

            ChangeState(GameState.GameStarting);
        }

        private void StartNextRound()
        {
            _roundsPlayed++;
            _roundManager.StartRound();
            ChangeState(GameState.InGame);
        }

        public void OnAllCluesSubmitted()
        {
            ChangeState(GameState.Voting);
            
            // Start voting timer - only the host should start it, but clients can too for local timer
            if (_voteManager != null && !_voteManager.VotingInProgress)
            {
                _voteManager.StartVoting(5f);
                Debug.Log("[GameManager] Started voting timer in OnAllCluesSubmitted");
            }
        }

        public void OnVotingEnded(CSteamID votedOut, bool wasImpostor)
        {
            ChangeState(GameState.RoundResults);

            if (_isHost)
            {
                // Check win conditions
                if (wasImpostor)
                {
                    // Impostor was found - check if any impostors remain
                    var remainingImpostors = _playerManager.GetImpostors();
                    if (remainingImpostors.Count == 0)
                    {
                        // Civilians win
                        EndGame(false);
                        return;
                    }
                }
                else if (votedOut != CSteamID.Nil)
                {
                    // Civilian was voted out - check if enough civilians remain
                    var remainingCivilians = _playerManager.GetCivilians();
                    if (remainingCivilians.Count <= 1)
                    {
                        // Impostors win
                        EndGame(true);
                        return;
                    }
                }

                // Continue to next round or end game
                if (_roundsPlayed >= _maxRounds)
                {
                    // Impostors win if not found
                    EndGame(true);
                }
                else
                {
                    // DISABLED: Don't auto-start next round - stay at round 1 for free roam
                    // Invoke(nameof(StartNextRound), 5f);
                    Debug.Log("[GameManager] Round 1 ended - staying in free roam mode (auto-round progression disabled)");
                }
            }
        }

        private void EndGame(bool impostorsWon)
        {
            var impostors = _playerManager.GetImpostors();
            ulong[] impostorIDs = new ulong[impostors.Count];
            for (int i = 0; i < impostors.Count; i++)
            {
                impostorIDs[i] = impostors[i].m_SteamID;
            }

            GameEndMessage message = new GameEndMessage
            {
                ImpostorsWon = impostorsWon,
                ImpostorSteamIDs = impostorIDs
            };
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.BroadcastMessage(message);
            }

            // Unlock achievements
            if (SteamAchievements.Instance != null)
            {
                CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;
                bool isLocalImpostor = impostors.Contains(localID);

                if (!impostorsWon && !isLocalImpostor)
                {
                    // Civilian won
                    SteamAchievements.Instance.UnlockFirstWin();
                    SteamAchievements.Instance.UnlockFindImpostor();
                }
                else if (impostorsWon && isLocalImpostor)
                {
                    // Impostor won
                    SteamAchievements.Instance.UnlockWinAsImpostor();
                }
            }

            ChangeState(GameState.GameEnd);
        }

        private void CheckAllPlayersReady()
        {
            if (_playerManager.AllPlayersReady() && _playerManager.PlayerCount >= 4)
            {
                StartGame();
            }
        }

        private void OnPlayerJoinedLobby(CSteamID steamID)
        {
            string playerName = SteamLobbyManager.Instance.GetPlayerName(steamID);
            _playerManager.AddPlayer(steamID, playerName);

            if (_isHost)
            {
                // Notify other players
                PlayerJoinedMessage message = new PlayerJoinedMessage
                {
                    PlayerSteamID = steamID.m_SteamID,
                    PlayerName = playerName
                };
                NetworkManager.Instance.BroadcastMessage(message);
            }
        }

        private void OnPlayerLeftLobby(CSteamID steamID)
        {
            _playerManager.RemovePlayer(steamID);

            if (_isHost)
            {
                PlayerLeftMessage message = new PlayerLeftMessage
                {
                    PlayerSteamID = steamID.m_SteamID
                };
                NetworkManager.Instance.BroadcastMessage(message);
            }
        }

        private void HandleReadyState(NetworkMessage message, CSteamID senderID)
        {
            if (message is ReadyStateMessage readyMsg)
            {
                _playerManager.SetReady(new CSteamID(readyMsg.PlayerSteamID), readyMsg.IsReady);
                
                if (_isHost)
                {
                    CheckAllPlayersReady();
                }
            }
        }

        private void HandleClueSubmitted(NetworkMessage message, CSteamID senderID)
        {
            if (message is ClueSubmittedMessage clueMsg && _isHost)
            {
                _roundManager.SubmitClue(new CSteamID(clueMsg.PlayerSteamID), clueMsg.Clue);
            }
        }

        private void HandleVoteSubmitted(NetworkMessage message, CSteamID senderID)
        {
            if (message is VoteSubmittedMessage voteMsg && _isHost)
            {
                _voteManager.CastVote(
                    new CSteamID(voteMsg.VoterSteamID),
                    new CSteamID(voteMsg.VotedForSteamID));
            }
        }

        public void SetReady(bool ready)
        {
            ReadyStateMessage message = new ReadyStateMessage
            {
                PlayerSteamID = Impostor.Steam.SteamManager.Instance.LocalSteamID.m_SteamID,
                IsReady = ready
            };

            if (_isHost)
            {
                _playerManager.SetReady(Impostor.Steam.SteamManager.Instance.LocalSteamID, ready);
                CheckAllPlayersReady();
            }
            else
            {
                NetworkManager.Instance.SendMessage(message, GetHostSteamID());
            }
        }

        private CSteamID GetHostSteamID()
        {
            if (SteamLobbyManager.Instance.IsInLobby)
            {
                return SteamLobbyManager.Instance.CurrentLobbyID;
            }
            return CSteamID.Nil;
        }
    }
}

