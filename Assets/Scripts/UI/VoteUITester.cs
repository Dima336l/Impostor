using UnityEngine;
using Steamworks;
using Impostor.Game;
using Impostor.Steam;

namespace Impostor.UI
{
    /// <summary>
    /// Helper script to test voting UI with dummy players.
    /// Attach to any GameObject in GameTable scene.
    /// Press 'V' to add dummy players and start voting.
    /// </summary>
    public class VoteUITester : MonoBehaviour
    {
        [Header("Testing")]
        [SerializeField] private int dummyPlayerCount = 4;
        [SerializeField] private bool autoStartVoting = true;

        private void Update()
        {
            // Press 'V' to test voting
            if (Input.GetKeyDown(KeyCode.V))
            {
                TestVoting();
            }
        }

        private void TestVoting()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager not found!");
                return;
            }

            Debug.Log("Setting up test voting...");

            // Add dummy players if needed
            if (GameManager.Instance.PlayerManager.PlayerCount < dummyPlayerCount)
            {
                AddDummyPlayers();
            }

            // Start voting
            if (autoStartVoting)
            {
                GameManager.Instance.ChangeState(GameManager.GameState.Voting);
                Debug.Log($"Voting started with {GameManager.Instance.PlayerManager.PlayerCount} players");
            }
        }

        private void AddDummyPlayers()
        {
            var playerManager = GameManager.Instance.PlayerManager;
            var localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;

            // Add local player if not already added
            if (!playerManager.HasPlayer(localID))
            {
                string localName = SteamFriends.GetPersonaName();
                playerManager.AddPlayer(localID, localName);
                Debug.Log($"Added local player: {localName}");
            }

            // Add dummy players
            for (int i = 1; i < dummyPlayerCount; i++)
            {
                // Create fake Steam IDs (using local ID + offset)
                ulong fakeSteamID = localID.m_SteamID + (ulong)(i * 1000);
                CSteamID dummyID = new CSteamID(fakeSteamID);
                
                if (!playerManager.HasPlayer(dummyID))
                {
                    string dummyName = $"DummyPlayer{i}";
                    playerManager.AddPlayer(dummyID, dummyName);
                    Debug.Log($"Added dummy player: {dummyName} (ID: {dummyID})");
                }
            }

            // Assign roles (1 impostor, rest civilians)
            if (GameManager.Instance.IsHost)
            {
                playerManager.AssignRoles(1); // 1 impostor
                Debug.Log("Assigned roles to test players");
            }
        }
    }
}
