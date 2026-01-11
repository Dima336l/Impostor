using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

namespace Impostor.Steam
{
    /// <summary>
    /// Handles P2P networking using Steam Networking Sockets.
    /// Manages connections and message routing between players.
    /// </summary>
    public class SteamNetworking : MonoBehaviour
    {
        private static SteamNetworking _instance;
        public static SteamNetworking Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SteamNetworking");
                    _instance = go.AddComponent<SteamNetworking>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Dictionary<CSteamID, HSteamNetConnection> _connections = new Dictionary<CSteamID, HSteamNetConnection>();
        private Dictionary<HSteamNetConnection, CSteamID> _connectionToSteamID = new Dictionary<HSteamNetConnection, CSteamID>();

        // Callbacks
        private Callback<SteamNetConnectionStatusChangedCallback_t> _connectionStatusChangedCallback;

        public event Action<CSteamID> OnPlayerConnected;
        public event Action<CSteamID> OnPlayerDisconnected;
        public event Action<CSteamID, byte[], int> OnMessageReceived;

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

        private void Update()
        {
            if (!SteamManager.Instance.IsInitialized) return;

            ReceiveMessages();
        }

        private void RegisterCallbacks()
        {
            _connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(OnConnectionStatusChanged);
        }

        private void UnregisterCallbacks()
        {
            if (_connectionStatusChangedCallback != null)
            {
                _connectionStatusChangedCallback.Dispose();
            }
        }

        public void ConnectToPlayer(CSteamID targetSteamID)
        {
            if (!SteamManager.Instance.IsInitialized)
            {
                Debug.LogError("Steam not initialized. Cannot connect.");
                return;
            }

            if (_connections.ContainsKey(targetSteamID))
            {
                Debug.LogWarning($"Already connected to {targetSteamID}");
                return;
            }

            SteamNetworkingIdentity identity = new SteamNetworkingIdentity();
            identity.SetSteamID(targetSteamID);

            HSteamNetConnection connection = SteamNetworkingSockets.ConnectP2P(ref identity, 0, 0, null);
            
            if (connection != HSteamNetConnection.Invalid)
            {
                _connections[targetSteamID] = connection;
                _connectionToSteamID[connection] = targetSteamID;
                Debug.Log($"Connecting to {targetSteamID}...");
            }
            else
            {
                Debug.LogError($"Failed to create connection to {targetSteamID}");
            }
        }

        public void DisconnectFromPlayer(CSteamID targetSteamID)
        {
            if (_connections.TryGetValue(targetSteamID, out HSteamNetConnection connection))
            {
                SteamNetworkingSockets.CloseConnection(connection, 0, null, false);
                _connections.Remove(targetSteamID);
                _connectionToSteamID.Remove(connection);
                Debug.Log($"Disconnected from {targetSteamID}");
            }
        }

        public void SendMessageToPlayer(CSteamID targetSteamID, byte[] data)
        {
            if (!_connections.TryGetValue(targetSteamID, out HSteamNetConnection connection))
            {
                Debug.LogWarning($"No connection to {targetSteamID}. Attempting to connect...");
                ConnectToPlayer(targetSteamID);
                return;
            }

            EResult result = SteamNetworkingSockets.SendMessageToConnection(
                connection,
                data,
                (uint)data.Length,
                (int)ESteamNetworkingSendFlags.k_nSteamNetworkingSend_Reliable,
                out _);

            if (result != EResult.k_EResultOK)
            {
                Debug.LogError($"Failed to send message to {targetSteamID}. Error: {result}");
            }
        }

        public void BroadcastMessage(byte[] data, CSteamID excludeSteamID = default)
        {
            foreach (var kvp in _connections)
            {
                if (kvp.Key != excludeSteamID)
                {
                    SendMessageToPlayer(kvp.Key, data);
                }
            }
        }

        private void ReceiveMessages()
        {
            IntPtr[] messages = new IntPtr[32];
            int numMessages = SteamNetworkingSockets.ReceiveMessagesOnConnection(
                HSteamNetConnection.Invalid,
                messages,
                32);

            for (int i = 0; i < numMessages; i++)
            {
                IntPtr messagePtr = messages[i];
                SteamNetworkingMessage_t message = (SteamNetworkingMessage_t)System.Runtime.InteropServices.Marshal.PtrToStructure(
                    messagePtr, 
                    typeof(SteamNetworkingMessage_t));
                HandleMessage(message);
                message.Release();
            }
        }

        private void HandleMessage(SteamNetworkingMessage_t message)
        {
            CSteamID senderID = message.m_identityPeer.GetSteamID();

            byte[] data = new byte[message.m_cbSize];
            System.Runtime.InteropServices.Marshal.Copy(message.m_pData, data, 0, (int)message.m_cbSize);

            OnMessageReceived?.Invoke(senderID, data, (int)message.m_cbSize);
        }

        private void OnConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t callback)
        {
            HSteamNetConnection connection = callback.m_hConn;
            CSteamID steamID = default;

            if (_connectionToSteamID.TryGetValue(connection, out steamID))
            {
                // Existing connection
            }
            else if (callback.m_info.m_identityRemote.GetSteamID().IsValid())
            {
                // New incoming connection
                steamID = callback.m_info.m_identityRemote.GetSteamID();
                _connections[steamID] = connection;
                _connectionToSteamID[connection] = steamID;
            }

            switch (callback.m_info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    OnPlayerConnected?.Invoke(steamID);
                    Debug.Log($"Connected to {steamID}");
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    OnPlayerDisconnected?.Invoke(steamID);
                    if (_connections.ContainsKey(steamID))
                    {
                        _connections.Remove(steamID);
                    }
                    if (_connectionToSteamID.ContainsKey(connection))
                    {
                        _connectionToSteamID.Remove(connection);
                    }
                    Debug.Log($"Disconnected from {steamID}");
                    break;
            }
        }

        public void InitializeConnections(List<CSteamID> lobbyMembers)
        {
            CSteamID localID = SteamManager.Instance.LocalSteamID;

            foreach (CSteamID memberID in lobbyMembers)
            {
                if (memberID != localID && !_connections.ContainsKey(memberID))
                {
                    ConnectToPlayer(memberID);
                }
            }
        }

        public void CloseAllConnections()
        {
            foreach (var kvp in _connections)
            {
                SteamNetworkingSockets.CloseConnection(kvp.Value, 0, null, false);
            }
            _connections.Clear();
            _connectionToSteamID.Clear();
        }
    }
}

