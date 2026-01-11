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
            if (SteamNetworking.Instance != null)
            {
                SteamNetworking.Instance.OnMessageReceived += HandleSteamMessage;
            }
        }

        private void OnDisable()
        {
            if (SteamNetworking.Instance != null)
            {
                SteamNetworking.Instance.OnMessageReceived -= HandleSteamMessage;
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
            if (!SteamManager.Instance.IsInitialized)
            {
                Debug.LogWarning("Steam not initialized. Cannot send message.");
                return;
            }

            byte[] data = message.Serialize();
            SteamNetworking.Instance.SendMessageToPlayer(targetSteamID, data);
        }

        public void BroadcastMessage(NetworkMessage message, CSteamID excludeSteamID = default)
        {
            if (!SteamManager.Instance.IsInitialized)
            {
                Debug.LogWarning("Steam not initialized. Cannot broadcast message.");
                return;
            }

            byte[] data = message.Serialize();
            SteamNetworking.Instance.BroadcastMessage(data, excludeSteamID);
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
                SteamNetworking.Instance.InitializeConnections(members);
            }
        }

        public void DisconnectAll()
        {
            SteamNetworking.Instance.CloseAllConnections();
        }
    }
}

