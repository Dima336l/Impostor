using System;
using UnityEngine;
using Steamworks;

namespace Impostor.Steam
{
    /// <summary>
    /// Manages Steam initialization, authentication, and core Steam functionality.
    /// Must be initialized before any other Steam-dependent systems.
    /// </summary>
    public class SteamManager : MonoBehaviour
    {
        private static SteamManager _instance;
        public static SteamManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SteamManager");
                    _instance = go.AddComponent<SteamManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;

        public CSteamID LocalSteamID { get; private set; }
        public string LocalPlayerName { get; private set; }

        public event Action OnSteamInitialized;
        public event Action OnSteamShutdown;

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
            InitializeSteam();
        }

        private void OnDestroy()
        {
            if (_isInitialized)
            {
                SteamAPI.Shutdown();
                OnSteamShutdown?.Invoke();
            }
        }

        private void Update()
        {
            if (_isInitialized)
            {
                SteamAPI.RunCallbacks();
            }
        }

        private void InitializeSteam()
        {
            try
            {
                if (SteamAPI.Init())
                {
                    _isInitialized = true;
                    LocalSteamID = SteamUser.GetSteamID();
                    LocalPlayerName = SteamFriends.GetPersonaName();

                    Debug.Log($"Steam initialized successfully. Player: {LocalPlayerName} (ID: {LocalSteamID})");
                    OnSteamInitialized?.Invoke();
                }
                else
                {
                    Debug.LogError("Steam initialization failed. Make sure Steam is running and you have a valid App ID.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Steam initialization exception: {e.Message}");
            }
        }

        public bool RestartAppIfNecessary(AppId_t unOwnAppID)
        {
            return SteamAPI.RestartAppIfNecessary(unOwnAppID);
        }
    }
}

