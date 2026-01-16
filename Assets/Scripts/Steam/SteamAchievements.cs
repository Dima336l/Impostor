using System;
using UnityEngine;
using Steamworks;

namespace Impostor.Steam
{
    /// <summary>
    /// Manages Steam achievements for the game.
    /// </summary>
    public class SteamAchievements : MonoBehaviour
    {
        private static SteamAchievements _instance;
        public static SteamAchievements Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SteamAchievements");
                    _instance = go.AddComponent<SteamAchievements>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Achievement IDs - these should match your Steamworks partner portal configuration
        private const string ACHIEVEMENT_FIRST_WIN = "ACH_FIRST_WIN";
        private const string ACHIEVEMENT_FIND_IMPOSTOR = "ACH_FIND_IMPOSTOR";
        private const string ACHIEVEMENT_WIN_AS_IMPOSTOR = "ACH_WIN_AS_IMPOSTOR";
        private const string ACHIEVEMENT_PLAY_10_GAMES = "ACH_PLAY_10_GAMES";
        private const string ACHIEVEMENT_PERFECT_GAME = "ACH_PERFECT_GAME";

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

        public void UnlockAchievement(string achievementID)
        {
            if (!Impostor.Steam.SteamManager.Instance.IsInitialized)
            {
                Debug.LogWarning("Steam not initialized. Cannot unlock achievement.");
                return;
            }

            bool success = SteamUserStats.SetAchievement(achievementID);
            if (success)
            {
                SteamUserStats.StoreStats();
                Debug.Log($"Achievement unlocked: {achievementID}");
            }
            else
            {
                Debug.LogError($"Failed to unlock achievement: {achievementID}");
            }
        }

        public void UnlockFirstWin()
        {
            UnlockAchievement(ACHIEVEMENT_FIRST_WIN);
        }

        public void UnlockFindImpostor()
        {
            UnlockAchievement(ACHIEVEMENT_FIND_IMPOSTOR);
        }

        public void UnlockWinAsImpostor()
        {
            UnlockAchievement(ACHIEVEMENT_WIN_AS_IMPOSTOR);
        }

        public void UnlockPlay10Games()
        {
            UnlockAchievement(ACHIEVEMENT_PLAY_10_GAMES);
        }

        public void UnlockPerfectGame()
        {
            UnlockAchievement(ACHIEVEMENT_PERFECT_GAME);
        }

        public bool IsAchievementUnlocked(string achievementID)
        {
            if (!Impostor.Steam.SteamManager.Instance.IsInitialized)
            {
                return false;
            }

            bool achieved = false;
            SteamUserStats.GetAchievement(achievementID, out achieved);
            return achieved;
        }
    }
}

