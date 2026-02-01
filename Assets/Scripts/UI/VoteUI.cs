using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using Impostor.Game;
using Impostor.Steam;
using Impostor.Networking;

namespace Impostor.UI
{
    /// <summary>
    /// Voting interface for selecting which player to vote out as the Impostor.
    /// </summary>
    public class VoteUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject votePanel;
        [SerializeField] private Transform playerButtonContainer;
        [SerializeField] private GameObject playerVoteButtonPrefab;
        [SerializeField] private Button noVoteButton;
        [SerializeField] private TextMeshProUGUI votingStatusText;
        [SerializeField] private TextMeshProUGUI voteCountText;

        private Dictionary<CSteamID, GameObject> _voteButtons = new Dictionary<CSteamID, GameObject>();
        private CSteamID _selectedVote = CSteamID.Nil;
        private bool _hasVoted = false;

        private void Start()
        {
            // Fallback: Find VotePanel if reference is missing
            if (votePanel == null)
            {
                FindVotePanel();
            }

            InitializeUI();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += OnGameStateChanged;
            }

            // Get VoteManager from GameManager
            VoteManager voteManager = null;
            if (GameManager.Instance != null)
            {
                voteManager = GameManager.Instance.VoteManager;
            }

            if (voteManager != null)
            {
                voteManager.OnVotingStarted += OnVotingStarted;
                voteManager.OnVoteCast += OnVoteCast;
                voteManager.OnVotingEnded += OnVotingEnded;
            }

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.RegisterMessageHandler(
                    NetworkMessage.MessageType.VoteSubmitted,
                    HandleVoteSubmitted);
            }

            SetVotePanelActive(false);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnGameStateChanged;
            }

            if (GameManager.Instance != null)
            {
                VoteManager voteManager = GameManager.Instance.VoteManager;
                if (voteManager != null)
                {
                    voteManager.OnVotingStarted -= OnVotingStarted;
                    voteManager.OnVoteCast -= OnVoteCast;
                    voteManager.OnVotingEnded -= OnVotingEnded;
                }
            }
        }

        private void InitializeUI()
        {
            if (noVoteButton != null)
            {
                noVoteButton.onClick.AddListener(() => CastVote(CSteamID.Nil));
            }
        }

        private void OnGameStateChanged(GameManager.GameState newState)
        {
            if (newState == GameManager.GameState.Voting)
            {
                SetVotePanelActive(true);
                CreateVoteButtons();
            }
            else
            {
                SetVotePanelActive(false);
            }
        }

        private void OnVotingStarted()
        {
            _hasVoted = false;
            _selectedVote = CSteamID.Nil;
            CreateVoteButtons();
            UpdateVotingStatus("Vote for who you think is the Impostor!");
        }

        private void OnVoteCast(CSteamID voterID, CSteamID targetID)
        {
            UpdateVoteCounts();
        }

        private void OnVotingEnded(CSteamID votedOut, bool wasImpostor)
        {
            string playerName = GetPlayerName(votedOut);
            UpdateVotingStatus($"Voting ended! {playerName} was voted out. (Impostor: {wasImpostor})");
            
            // Show results for a few seconds, then hide
            Invoke(nameof(HideVotePanel), 5f);
        }

        private string GetPlayerName(CSteamID playerID)
        {
            // Try to get name from PlayerManager first (works for dummy players)
            if (GameManager.Instance != null && GameManager.Instance.PlayerManager != null)
            {
                var playerData = GameManager.Instance.PlayerManager.GetPlayer(playerID);
                if (playerData != null && !string.IsNullOrEmpty(playerData.PlayerName))
                {
                    return playerData.PlayerName;
                }
            }
            
            // Fallback to SteamLobbyManager
            if (SteamLobbyManager.Instance != null)
            {
                string name = SteamLobbyManager.Instance.GetPlayerName(playerID);
                if (!string.IsNullOrEmpty(name) && name != "[unknown]")
                {
                    return name;
                }
            }
            
            // Final fallback
            return $"Player {playerID.m_SteamID}";
        }

        private void HandleVoteSubmitted(NetworkMessage message, CSteamID senderID)
        {
            if (message is VoteSubmittedMessage voteMsg)
            {
                UpdateVoteCounts();
            }
        }

        private void CreateVoteButtons()
        {
            // Clear existing buttons
            foreach (var button in _voteButtons.Values)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            _voteButtons.Clear();

            if (playerVoteButtonPrefab == null)
            {
                Debug.LogError("VoteUI: Missing playerVoteButtonPrefab reference!");
                return;
            }
            
            // Fallback: Find container by name if reference is missing
            if (playerButtonContainer == null)
            {
                GameObject containerObj = GameObject.Find("PlayerButtonContainer");
                if (containerObj != null)
                {
                    playerButtonContainer = containerObj.transform;
                    Debug.Log("Found PlayerButtonContainer by name");
                }
                else
                {
                    Debug.LogError("VoteUI: Missing playerButtonContainer reference and couldn't find by name!");
                    return;
                }
            }

            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager.Instance is null!");
                return;
            }

            CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;
            var allPlayers = GameManager.Instance.PlayerManager.AllPlayers;
            
            Debug.Log($"Creating vote buttons. Total players: {allPlayers.Count}, Local ID: {localID}");

            int buttonCount = 0;
            // Create button for each player (except self)
            foreach (CSteamID playerID in allPlayers)
            {
                if (playerID == localID)
                {
                    Debug.Log($"Skipping local player: {GetPlayerName(playerID)}");
                    continue; // Can't vote for yourself
                }

                GameObject buttonObj = Instantiate(playerVoteButtonPrefab, playerButtonContainer);
                _voteButtons[playerID] = buttonObj;
                buttonCount++;
                
                // Reset RectTransform to ensure proper layout
                RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.localScale = Vector3.one;
                    // For VerticalLayoutGroup: buttons need to be positioned by the layout
                    // Set anchors to top-left, LayoutGroup will position them
                    buttonRect.anchorMin = new Vector2(0, 1);
                    buttonRect.anchorMax = new Vector2(1, 1);
                    buttonRect.pivot = new Vector2(0.5f, 1f); // Top pivot for top-down layout
                    buttonRect.anchoredPosition = Vector2.zero; // LayoutGroup will position
                    buttonRect.sizeDelta = new Vector2(0, 50); // Fixed height (50px), stretch width
                    
                    Debug.Log($"Button #{buttonCount} RectTransform - Pos: {buttonRect.anchoredPosition}, Size: {buttonRect.sizeDelta}, Pivot: {buttonRect.pivot}");
                }
                
                // Ensure button is active
                buttonObj.SetActive(true);
                
                Debug.Log($"Created vote button #{buttonCount} for player: {GetPlayerName(playerID)} (ID: {playerID})");

                // Set player name
                TextMeshProUGUI nameText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = GetPlayerName(playerID);
                }

                // Set button click
                Button button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    CSteamID targetID = playerID; // Capture for closure
                    button.onClick.AddListener(() => CastVote(targetID));
                }
            }
            
            // Force LayoutGroup to recalculate
            if (playerButtonContainer != null)
            {
                RectTransform containerRect = playerButtonContainer as RectTransform;
                if (containerRect != null)
                {
                    // Get LayoutGroup from the GameObject, not the Transform
                    VerticalLayoutGroup layoutGroup = containerRect.gameObject.GetComponent<VerticalLayoutGroup>();
                    
                    // If not found, try to find it by name
                    if (layoutGroup == null)
                    {
                        GameObject containerObj = GameObject.Find("PlayerButtonContainer");
                        if (containerObj != null)
                        {
                            layoutGroup = containerObj.GetComponent<VerticalLayoutGroup>();
                            Debug.Log($"Found PlayerButtonContainer by name, LayoutGroup: {(layoutGroup != null ? "Found" : "Still null")}");
                        }
                    }
                    
                    // If still null, add it
                    if (layoutGroup == null)
                    {
                        layoutGroup = containerRect.gameObject.AddComponent<VerticalLayoutGroup>();
                        layoutGroup.spacing = 10;
                        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                        layoutGroup.childControlHeight = false; // Use prefab size (50px)
                        layoutGroup.childControlWidth = true; // Stretch width
                        layoutGroup.childForceExpandHeight = false;
                        layoutGroup.childForceExpandWidth = true;
                        Debug.Log("Added VerticalLayoutGroup component to container");
                    }
                    
                    Debug.Log($"Container before rebuild - Children: {containerRect.childCount}, LayoutGroup enabled: {(layoutGroup != null ? layoutGroup.enabled.ToString() : "null")}");
                    
                    // Enable and configure LayoutGroup if it exists
                    if (layoutGroup != null)
                    {
                        layoutGroup.enabled = true;
                        layoutGroup.childControlHeight = false; // Let buttons use their prefab size (50px)
                        layoutGroup.childControlWidth = true; // Stretch width
                        layoutGroup.childForceExpandHeight = false; // Don't expand height
                        layoutGroup.childForceExpandWidth = true; // Expand width
                    }
                    
                    // Force Canvas update and immediate rebuild
                    Canvas.ForceUpdateCanvases();
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
                    
                    // Also wait one frame and rebuild again (sometimes needed)
                    StartCoroutine(RebuildLayoutNextFrame(containerRect));
                    
                    Debug.Log($"Layout rebuild scheduled. Container has {containerRect.childCount} children");
                }
            }
            Debug.Log($"Total buttons created: {buttonCount}, Dictionary count: {_voteButtons.Count}");
        }

        private IEnumerator RebuildLayoutNextFrame(RectTransform container)
        {
            yield return null; // Wait one frame
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(container);
            
            // Log button positions for debugging
            for (int i = 0; i < container.childCount; i++)
            {
                RectTransform child = container.GetChild(i) as RectTransform;
                if (child != null)
                {
                    Debug.Log($"Button {i} position after layout: {child.anchoredPosition}, size: {child.sizeDelta}");
                }
            }
            
            Debug.Log($"Layout rebuilt after frame. Container now has {container.childCount} children");
        }

        private void CastVote(CSteamID targetID)
        {
            if (_hasVoted)
            {
                return;
            }

            _selectedVote = targetID;
            _hasVoted = true;

            if (GameManager.Instance == null)
            {
                return;
            }

            VoteManager voteManager = GameManager.Instance.VoteManager;
            if (voteManager == null)
            {
                return;
            }

            CSteamID localID = Impostor.Steam.SteamManager.Instance.LocalSteamID;

            if (GameManager.Instance.IsHost)
            {
                voteManager.CastVote(localID, targetID);
            }
            else
            {
                VoteSubmittedMessage message = new VoteSubmittedMessage
                {
                    VoterSteamID = localID.m_SteamID,
                    VotedForSteamID = targetID.m_SteamID
                };
                NetworkManager.Instance.SendMessage(message, GetHostSteamID());
            }

            UpdateVotingStatus("Vote submitted!");
            SetButtonsInteractable(false);
        }

        private void UpdateVoteCounts()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            VoteManager voteManager = GameManager.Instance.VoteManager;
            if (voteManager == null)
            {
                return;
            }

            var voteCounts = voteManager.GetVoteCounts();
            string voteText = "Votes: ";

            foreach (var kvp in voteCounts)
            {
                voteText += $"{GetPlayerName(kvp.Key)}: {kvp.Value} ";
            }

            if (voteCountText != null)
            {
                voteCountText.text = voteText;
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            foreach (var buttonObj in _voteButtons.Values)
            {
                if (buttonObj != null)
                {
                    Button button = buttonObj.GetComponent<Button>();
                    if (button != null)
                    {
                        button.interactable = interactable;
                    }
                }
            }

            if (noVoteButton != null)
            {
                noVoteButton.interactable = interactable;
            }
        }

        private void SetVotePanelActive(bool active)
        {
            // Safely check and find VotePanel if needed
            try
            {
                // Try to access the object - this will throw if it's destroyed
                if (votePanel == null || !votePanel)
                {
                    FindVotePanel();
                }
                
                if (votePanel != null)
                {
                    votePanel.SetActive(active);
                }
            }
            catch (MissingReferenceException)
            {
                // Object was destroyed, try to find it again
                votePanel = null;
                FindVotePanel();
                if (votePanel != null)
                {
                    votePanel.SetActive(active);
                }
            }
        }

        private void FindVotePanel()
        {
            // Try to find it as a child of this GameObject first
            Transform panelTransform = transform.Find("VotePanel");
            if (panelTransform != null)
            {
                votePanel = panelTransform.gameObject;
                Debug.Log("Found VotePanel as child");
            }
            else
            {
                // Try finding by name in scene
                GameObject foundPanel = GameObject.Find("VotePanel");
                if (foundPanel != null)
                {
                    votePanel = foundPanel;
                    Debug.Log("Found VotePanel by name");
                }
                else
                {
                    Debug.LogError("VotePanel not found! Please assign it in the Inspector.");
                }
            }
        }

        private void HideVotePanel()
        {
            SetVotePanelActive(false);
        }

        private void UpdateVotingStatus(string message)
        {
            if (votingStatusText != null)
            {
                votingStatusText.text = message;
            }
        }

        private CSteamID GetHostSteamID()
        {
            if (SteamLobbyManager.Instance != null && SteamLobbyManager.Instance.IsInLobby)
            {
                return SteamLobbyManager.Instance.CurrentLobbyID;
            }
            return CSteamID.Nil;
        }
    }
}

