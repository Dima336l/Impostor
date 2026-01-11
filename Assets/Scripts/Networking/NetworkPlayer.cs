using System;
using UnityEngine;

namespace Impostor.Networking
{
    /// <summary>
    /// Represents a networked player with their Steam ID and game state.
    /// Uses ulong for SteamID to avoid requiring Steamworks types at compile time.
    /// </summary>
    [Serializable]
    public class NetworkPlayer
    {
        public ulong SteamID { get; set; }
        public string PlayerName { get; set; }
        public bool IsReady { get; set; }
        public bool IsConnected { get; set; }
        public int PlayerIndex { get; set; }

        public NetworkPlayer(ulong steamID, string playerName)
        {
            SteamID = steamID;
            PlayerName = playerName;
            IsReady = false;
            IsConnected = true;
            PlayerIndex = -1;
        }

        public override bool Equals(object obj)
        {
            if (obj is NetworkPlayer other)
            {
                return SteamID == other.SteamID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return SteamID.GetHashCode();
        }
    }
}

