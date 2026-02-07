using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Impostor.Steam;
using Impostor.Networking;

namespace Impostor.Networking
{
    /// <summary>
    /// Central network manager that routes messages between Steam networking and game systems.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("NetworkManager");
                    _instance = go.AddComponent<NetworkManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Dictionary<NetworkMessage.MessageType, Action<NetworkMessage, CSteamID>> _messageHandlers = 
            new Dictionary<NetworkMessage.MessageType, Action<NetworkMessage, CSteamID>>();

        public event Action<NetworkMessage, CSteamID> OnMessageReceived;

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
            if (Impostor.Steam.SteamNetworking.Instance != null)
            {
                Impostor.Steam.SteamNetworking.Instance.OnMessageReceived += HandleSteamMessage;
            }
        }

        private void OnDisable()
        {
            if (Impostor.Steam.SteamNetworking.Instance != null)
            {
                Impostor.Steam.SteamNetworking.Instance.OnMessageReceived -= HandleSteamMessage;
            }
        }

        public void RegisterMessageHandler(NetworkMessage.MessageType type, Action<NetworkMessage, CSteamID> handler)
        {
            if (_messageHandlers.ContainsKey(type))
            {
                _messageHandlers[type] += handler;
            }
            else
            {
                _messageHandlers[type] = handler;
            }
        }

        public void UnregisterMessageHandler(NetworkMessage.MessageType type, Action<NetworkMessage, CSteamID> handler)
        {
            if (_messageHandlers.ContainsKey(type))
            {
                _messageHandlers[type] -= handler;
            }
        }

        public void SendMessage(NetworkMessage message, CSteamID targetSteamID)
        {
            if (!Impostor.Steam.SteamManager.Instance.IsInitialized)
            {
                Debug.LogWarning("Steam not initialized. Cannot send message.");
                return;
            }

            byte[] data = message.Serialize();
            Impostor.Steam.SteamNetworking.Instance.SendMessageToPlayer(targetSteamID, data);
        }

        public void BroadcastMessage(NetworkMessage message, CSteamID excludeSteamID = default)
        {
            if (!Impostor.Steam.SteamManager.Instance.IsInitialized)
            {
                Debug.LogWarning("Steam not initialized. Cannot broadcast message.");
                return;
            }

            byte[] data = message.Serialize();
            Impostor.Steam.SteamNetworking.Instance.BroadcastMessage(data, excludeSteamID);
        }

        private void HandleSteamMessage(CSteamID senderID, byte[] data, int size)
        {
            try
            {
                NetworkMessage message = NetworkMessage.Deserialize(data);
                if (message != null)
                {
                    OnMessageReceived?.Invoke(message, senderID);

                    if (_messageHandlers.TryGetValue(message.Type, out Action<NetworkMessage, CSteamID> handler))
                    {
                        handler?.Invoke(message, senderID);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error deserializing message from {senderID}: {e.Message}");
            }
        }

        public void InitializeNetworkConnections()
        {
            if (SteamLobbyManager.Instance.IsInLobby)
            {
                List<CSteamID> members = SteamLobbyManager.Instance.LobbyMembers;
                Impostor.Steam.SteamNetworking.Instance.InitializeConnections(members);
            }
        }

        public void DisconnectAll()
        {
            Impostor.Steam.SteamNetworking.Instance.CloseAllConnections();
        }

        /// <summary>
        /// Handles a message for the local player directly (without going through network).
        /// Used when the host needs to send a message to themselves.
        /// </summary>
        public void HandleMessageForLocalPlayer(NetworkMessage message)
        {
            CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;
            
            // Invoke the message handlers directly
            OnMessageReceived?.Invoke(message, localID);
            
            if (_messageHandlers.TryGetValue(message.Type, out Action<NetworkMessage, CSteamID> handler))
            {
                handler?.Invoke(message, localID);
            }
        }
    }
}

