using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

namespace Impostor.Steam
{
    /// <summary>
    /// Manages Steam lobby creation, joining, and player management.
    /// </summary>
    public class SteamLobbyManager : MonoBehaviour
    {
        private static SteamLobbyManager _instance;
        public static SteamLobbyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SteamLobbyManager");
                    _instance = go.AddComponent<SteamLobbyManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Lobby callbacks
        private Callback<LobbyCreated_t> _lobbyCreatedCallback;
        private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequestedCallback;
        private Callback<LobbyEnter_t> _lobbyEnteredCallback;
        private Callback<LobbyChatUpdate_t> _lobbyChatUpdateCallback;

        private CSteamID _currentLobbyID;
        public CSteamID CurrentLobbyID => _currentLobbyID;
        public bool IsInLobby => _currentLobbyID.IsValid();

        private List<CSteamID> _lobbyMembers = new List<CSteamID>();
        public List<CSteamID> LobbyMembers => new List<CSteamID>(_lobbyMembers);

        public event Action<CSteamID> OnLobbyCreated;
        public event Action<CSteamID> OnLobbyJoined;
        public event Action OnLobbyLeft;
        public event Action<CSteamID> OnPlayerJoined;
        public event Action<CSteamID> OnPlayerLeft;

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

        private void OnEnable()
        {
            if (SteamManager.Instance.IsInitialized)
            {
                RegisterCallbacks();
            }
            else
            {
                SteamManager.Instance.OnSteamInitialized += RegisterCallbacks;
            }
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            _lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreatedCallback);
            _lobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequestedCallback);
            _lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEnteredCallback);
            _lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdateCallback);
        }

        private void UnregisterCallbacks()
        {
            if (_lobbyCreatedCallback != null)
            {
                _lobbyCreatedCallback.Dispose();
            }
            if (_lobbyJoinRequestedCallback != null)
            {
                _lobbyJoinRequestedCallback.Dispose();
            }
            if (_lobbyEnteredCallback != null)
            {
                _lobbyEnteredCallback.Dispose();
            }
            if (_lobbyChatUpdateCallback != null)
            {
                _lobbyChatUpdateCallback.Dispose();
            }
        }

        // Lobby type constants: 0 = Private, 1 = FriendsOnly, 2 = Public, 3 = Invisible
        public void CreateLobby(int lobbyType = 1, int maxMembers = 6) // 1 = FriendsOnly
        {
            if (!SteamManager.Instance.IsInitialized)
            {
                Debug.LogError("Steam not initialized. Cannot create lobby.");
                return;
            }

            SteamAPICall_t handle = SteamMatchmaking.CreateLobby(lobbyType, maxMembers);
            Debug.Log($"Creating lobby (max {maxMembers} players)...");
        }

        public void JoinLobby(CSteamID lobbyID)
        {
            if (!SteamManager.Instance.IsInitialized)
            {
                Debug.LogError("Steam not initialized. Cannot join lobby.");
                return;
            }

            SteamMatchmaking.JoinLobby(lobbyID);
        }

        public void LeaveLobby()
        {
            if (_currentLobbyID.IsValid())
            {
                SteamMatchmaking.LeaveLobby(_currentLobbyID);
                _currentLobbyID = CSteamID.Nil;
                _lobbyMembers.Clear();
                OnLobbyLeft?.Invoke();
                Debug.Log("Left lobby");
            }
        }

        private void OnLobbyCreatedCallback(LobbyCreated_t callback)
        {
            if (callback.m_eResult == EResult.k_EResultOK)
            {
                _currentLobbyID = (CSteamID)callback.m_ulSteamIDLobby;
                UpdateLobbyMembers();
                OnLobbyCreated?.Invoke(_currentLobbyID);
                Debug.Log($"Lobby created successfully: {_currentLobbyID}");
            }
            else
            {
                Debug.LogError($"Failed to create lobby. Error: {callback.m_eResult}");
            }
        }

        private void OnLobbyJoinRequestedCallback(GameLobbyJoinRequested_t callback)
        {
            Debug.Log($"Lobby join requested: {callback.m_steamIDLobby}");
            JoinLobby(callback.m_steamIDLobby);
        }

        private void OnLobbyEnteredCallback(LobbyEnter_t callback)
        {
            if (callback.m_EChatRoomEnterResponse == (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                _currentLobbyID = (CSteamID)callback.m_ulSteamIDLobby;
                UpdateLobbyMembers();
                OnLobbyJoined?.Invoke(_currentLobbyID);
                Debug.Log($"Joined lobby: {_currentLobbyID}");
            }
            else
            {
                Debug.LogError($"Failed to enter lobby. Response: {callback.m_EChatRoomEnterResponse}");
            }
        }

        private void OnLobbyChatUpdateCallback(LobbyChatUpdate_t callback)
        {
            CSteamID changedUser = (CSteamID)callback.m_ulSteamIDUserChanged;
            CSteamID lobbyID = (CSteamID)callback.m_ulSteamIDLobby;

            if (lobbyID != _currentLobbyID) return;

            EChatMemberStateChange change = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;

            if (change == EChatMemberStateChange.k_EChatMemberStateChangeEntered)
            {
                if (!_lobbyMembers.Contains(changedUser))
                {
                    _lobbyMembers.Add(changedUser);
                    OnPlayerJoined?.Invoke(changedUser);
                    Debug.Log($"Player joined: {SteamFriends.GetFriendPersonaName(changedUser)}");
                }
            }
            else if (change == EChatMemberStateChange.k_EChatMemberStateChangeLeft ||
                     change == EChatMemberStateChange.k_EChatMemberStateChangeDisconnected ||
                     change == EChatMemberStateChange.k_EChatMemberStateChangeKicked ||
                     change == EChatMemberStateChange.k_EChatMemberStateChangeBanned)
            {
                if (_lobbyMembers.Remove(changedUser))
                {
                    OnPlayerLeft?.Invoke(changedUser);
                    Debug.Log($"Player left: {SteamFriends.GetFriendPersonaName(changedUser)}");
                }
            }

            UpdateLobbyMembers();
        }

        private void UpdateLobbyMembers()
        {
            _lobbyMembers.Clear();
            if (!_currentLobbyID.IsValid()) return;

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(_currentLobbyID);
            for (int i = 0; i < memberCount; i++)
            {
                CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(_currentLobbyID, i);
                _lobbyMembers.Add(memberID);
            }
        }

        public string GetPlayerName(CSteamID steamID)
        {
            return SteamFriends.GetFriendPersonaName(steamID);
        }

        public bool IsLobbyOwner()
        {
            if (!_currentLobbyID.IsValid()) return false;
            CSteamID ownerID = SteamMatchmaking.GetLobbyOwner(_currentLobbyID);
            return ownerID == SteamManager.Instance.LocalSteamID;
        }
    }
}

