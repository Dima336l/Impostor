using System;
using Steamworks;

namespace Impostor.Networking
{
    /// <summary>
    /// Represents a networked player with their Steam ID and game state.
    /// </summary>
    [Serializable]
    public class NetworkPlayer
    {
        public CSteamID SteamID { get; set; }
        public string PlayerName { get; set; }
        public bool IsReady { get; set; }
        public bool IsConnected { get; set; }
        public int PlayerIndex { get; set; }

        public NetworkPlayer(CSteamID steamID, string playerName)
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

