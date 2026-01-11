using System;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Impostor.Networking;

namespace Impostor.Game
{
    /// <summary>
    /// Manages player data, roles, and player state throughout the game.
    /// </summary>
    public class PlayerManager
    {
        private Dictionary<CSteamID, PlayerData> _players = new Dictionary<CSteamID, PlayerData>();
        private List<CSteamID> _playerOrder = new List<CSteamID>();

        public int PlayerCount => _players.Count;
        public List<CSteamID> AllPlayers => new List<CSteamID>(_playerOrder);

        public event Action<CSteamID> OnPlayerAdded;
        public event Action<CSteamID> OnPlayerRemoved;
        public event Action<CSteamID, PlayerRole> OnRoleAssigned;

        public void AddPlayer(CSteamID steamID, string playerName)
        {
            if (!_players.ContainsKey(steamID))
            {
                PlayerData player = new PlayerData
                {
                    SteamID = steamID,
                    PlayerName = playerName,
                    Role = PlayerRole.None,
                    IsReady = false,
                    HasSubmittedClue = false,
                    HasVoted = false,
                    VoteTarget = CSteamID.Nil,
                    Clue = string.Empty
                };

                _players[steamID] = player;
                _playerOrder.Add(steamID);
                OnPlayerAdded?.Invoke(steamID);
            }
        }

        public void RemovePlayer(CSteamID steamID)
        {
            if (_players.Remove(steamID))
            {
                _playerOrder.Remove(steamID);
                OnPlayerRemoved?.Invoke(steamID);
            }
        }

        public PlayerData GetPlayer(CSteamID steamID)
        {
            _players.TryGetValue(steamID, out PlayerData player);
            return player;
        }

        public bool HasPlayer(CSteamID steamID)
        {
            return _players.ContainsKey(steamID);
        }

        public void AssignRoles(int impostorCount = 1)
        {
            if (_playerOrder.Count < impostorCount + 1)
            {
                throw new InvalidOperationException("Not enough players to assign roles");
            }

            // Reset all roles
            foreach (var player in _players.Values)
            {
                player.Role = PlayerRole.Civilian;
            }

            // Randomly assign impostors
            List<CSteamID> availablePlayers = new List<CSteamID>(_playerOrder);
            System.Random random = new System.Random();

            for (int i = 0; i < impostorCount; i++)
            {
                int index = random.Next(availablePlayers.Count);
                CSteamID impostorID = availablePlayers[index];
                availablePlayers.RemoveAt(index);

                _players[impostorID].Role = PlayerRole.Impostor;
                OnRoleAssigned?.Invoke(impostorID, PlayerRole.Impostor);
            }

            // Assign civilians
            foreach (CSteamID civilianID in availablePlayers)
            {
                OnRoleAssigned?.Invoke(civilianID, PlayerRole.Civilian);
            }
        }

        public List<CSteamID> GetImpostors()
        {
            return _players.Where(kvp => kvp.Value.Role == PlayerRole.Impostor)
                          .Select(kvp => kvp.Key)
                          .ToList();
        }

        public List<CSteamID> GetCivilians()
        {
            return _players.Where(kvp => kvp.Value.Role == PlayerRole.Civilian)
                          .Select(kvp => kvp.Key)
                          .ToList();
        }

        public void SetReady(CSteamID steamID, bool ready)
        {
            if (_players.TryGetValue(steamID, out PlayerData player))
            {
                player.IsReady = ready;
            }
        }

        public bool AllPlayersReady()
        {
            return _players.Values.All(p => p.IsReady);
        }

        public void ResetRoundState()
        {
            foreach (var player in _players.Values)
            {
                player.HasSubmittedClue = false;
                player.HasVoted = false;
                player.VoteTarget = CSteamID.Nil;
                player.Clue = string.Empty;
            }
        }

        public void Clear()
        {
            _players.Clear();
            _playerOrder.Clear();
        }
    }

    public enum PlayerRole
    {
        None,
        Civilian,
        Impostor
    }

    [Serializable]
    public class PlayerData
    {
        public CSteamID SteamID { get; set; }
        public string PlayerName { get; set; }
        public PlayerRole Role { get; set; }
        public bool IsReady { get; set; }
        public bool HasSubmittedClue { get; set; }
        public bool HasVoted { get; set; }
        public CSteamID VoteTarget { get; set; }
        public string Clue { get; set; }
    }
}

