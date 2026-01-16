using UnityEngine;
using Steamworks;

namespace Impostor.Steam
{
    /// <summary>
    /// Manages Steam Rich Presence to show game state to friends.
    /// </summary>
    public class SteamRichPresence : MonoBehaviour
    {
        private static SteamRichPresence _instance;
        public static SteamRichPresence Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SteamRichPresence");
                    _instance = go.AddComponent<SteamRichPresence>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

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

        public void SetStatus(string status)
        {
            if (!Impostor.Steam.SteamManager.Instance.IsInitialized)
            {
                return;
            }

            SteamFriends.SetRichPresence("status", status);
        }

        public void SetInMainMenu()
        {
            SetStatus("In Main Menu");
        }

        public void SetInLobby(int playerCount, int maxPlayers)
        {
            SetStatus($"In Lobby ({playerCount}/{maxPlayers} players)");
        }

        public void SetInGame(int round, int totalRounds)
        {
            SetStatus($"Playing Round {round}/{totalRounds}");
        }

        public void SetVoting()
        {
            SetStatus("Voting Phase");
        }

        public void ClearStatus()
        {
            if (Impostor.Steam.SteamManager.Instance.IsInitialized)
            {
                SteamFriends.ClearRichPresence();
            }
        }
    }
}

