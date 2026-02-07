using UnityEngine;
using Steamworks;
using Impostor.Game;

namespace Impostor.Game
{
    /// <summary>
    /// Sets up the table and positions players around it in a circle (POV poker-style view).
    /// </summary>
    public class TableSetup : MonoBehaviour
    {
        [Header("Table Settings")]
        [SerializeField] private GameObject tablePrefab; // Optional prefab for table
        [SerializeField] private Vector3 tablePosition = Vector3.zero;
        [SerializeField] private float tableRadius = 2f; // Distance from center to player positions
        [SerializeField] private float tableHeight = 0.75f; // Height of table surface
        
        [Header("Player Representation")]
        [SerializeField] private GameObject playerMarkerPrefab; // Optional prefab for player markers/avatars
        [SerializeField] private float playerMarkerHeight = 0f; // Height offset for player markers above table
        
        [Header("Camera")]
        [SerializeField] private UnityEngine.Camera playerCamera; // Main camera (will be positioned at local player's position)
        
        private GameObject _table;
        private Transform _tableCenter;
        
        private void Awake()
        {
            Debug.Log("[TableSetup] Awake() called - TableSetup component is active");
        }
        
        private void Start()
        {
            Debug.Log("[TableSetup] Start() called - Beginning table setup");
            
            // Auto-find camera if not assigned
            if (playerCamera == null)
            {
                playerCamera = UnityEngine.Camera.main;
                if (playerCamera == null)
                {
                    playerCamera = FindFirstObjectByType<UnityEngine.Camera>();
                }
                if (playerCamera != null)
                {
                    Debug.Log($"[TableSetup] Auto-found camera: {playerCamera.name} at position {playerCamera.transform.position}");
                }
                else
                {
                    Debug.LogError("[TableSetup] No camera found! Cannot position camera.");
                }
            }
            else
            {
                Debug.Log($"[TableSetup] Using assigned camera: {playerCamera.name}");
            }
            
            SetupTable();
            
            // Always notify camera even if no players yet
            NotifyCameraOfTable();
            
            // Position players (will position camera at default if no players)
            PositionPlayers();
            
            Debug.Log("[TableSetup] Start() completed");
        }
        
        private void NotifyCameraOfTable()
        {
            // Find TableCameraController and set its table center reference
            Impostor.Camera.TableCameraController cameraController = FindFirstObjectByType<Impostor.Camera.TableCameraController>();
            if (cameraController != null && _tableCenter != null)
            {
                cameraController.SetTableCenter(_tableCenter);
                Debug.Log("[TableSetup] Notified TableCameraController of table center");
            }
            else
            {
                if (cameraController == null)
                {
                    Debug.LogWarning("[TableSetup] TableCameraController not found - camera won't be positioned automatically");
                }
                if (_tableCenter == null)
                {
                    Debug.LogWarning("[TableSetup] Table center is null - cannot notify camera");
                }
            }
        }
        
        private void SetupTable()
        {
            Debug.Log("[TableSetup] SetupTable() called");
            
            // Always destroy existing table and create a fresh one to ensure visibility
            GameObject existingTable = GameObject.FindGameObjectWithTag("Table");
            if (existingTable != null)
            {
                Debug.Log("[TableSetup] Found existing table - destroying it to create a fresh visible one");
                Destroy(existingTable);
            }
            
            // Always create a new visible table
            if (tablePrefab != null)
            {
                _table = Instantiate(tablePrefab, tablePosition, Quaternion.identity);
                _table.name = "Table";
                _table.tag = "Table";
                _tableCenter = _table.transform;
                Debug.Log($"[TableSetup] Created table from prefab at position {tablePosition}");
            }
            else
            {
                // Create a simple table as a placeholder (cylinder) - make it BIG and VISIBLE
                _table = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _table.name = "Table";
                _table.tag = "Table";
                _table.transform.position = tablePosition;
                _table.transform.localScale = new Vector3(3f, tableHeight, 3f); // Bigger table - 3 units radius
                
                // Set up table material (dark wood-like color) - make it very visible
                Renderer renderer = _table.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.5f, 0.3f, 0.15f); // Lighter brown for visibility
                    renderer.material = mat;
                    renderer.enabled = true;
                }
                
                // Remove collider to avoid physics issues (optional)
                Collider col = _table.GetComponent<Collider>();
                if (col != null)
                {
                    // Keep collider for now, might be useful
                }
                
                _tableCenter = _table.transform;
                _table.SetActive(true);
                
                Debug.Log($"[TableSetup] ✓ Created BIG visible table:");
                Debug.Log($"  - Position: {tablePosition}");
                Debug.Log($"  - Scale: {_table.transform.localScale}");
                Debug.Log($"  - Active: {_table.activeSelf}");
            }
            
            if (_tableCenter == null)
            {
                Debug.LogError("[TableSetup] Failed to create or find table center!");
            }
            else
            {
                Debug.Log($"[TableSetup] ✓ Table center confirmed at: {_tableCenter.position}, active: {_tableCenter.gameObject.activeSelf}, visible: {_tableCenter.gameObject.activeInHierarchy}");
            }
        }
        
        private void PositionPlayers()
        {
            if (_tableCenter == null)
            {
                Debug.LogError("[TableSetup] Cannot position players - table center is null!");
                return;
            }
            
            // Always create 4 players for visualization (you + 3 others)
            int playerCount = 4;
            if (GameManager.Instance?.PlayerManager != null && GameManager.Instance.PlayerManager.PlayerCount > 0)
            {
                playerCount = GameManager.Instance.PlayerManager.PlayerCount;
            }
            else
            {
                Debug.Log("[TableSetup] No real players yet - creating 4 test player markers for visualization");
            }
            
            float angleStep = 360f / playerCount;
            
            // Position camera at player 0's seat (first person POV)
            // Camera should be close enough to see the table clearly
            float cameraAngle = 0f * angleStep * Mathf.Deg2Rad;
            Vector3 cameraPos = _tableCenter.position + new Vector3(
                Mathf.Sin(cameraAngle) * tableRadius,
                0f,
                Mathf.Cos(cameraAngle) * tableRadius
            );
            
            if (playerCamera != null)
            {
                // Position camera at table edge, looking at center
                playerCamera.transform.position = cameraPos + Vector3.up * 1.6f; // Eye height (1.6m)
                playerCamera.transform.LookAt(_tableCenter.position + Vector3.up * 0.4f); // Look at table surface
                
                // Make sure camera can see the table
                playerCamera.nearClipPlane = 0.1f;
                playerCamera.farClipPlane = 100f;
                playerCamera.fieldOfView = 75f; // Wide FOV to see more
                
                Debug.Log($"[TableSetup] ✓ Camera positioned:");
                Debug.Log($"  - Position: {playerCamera.transform.position}");
                Debug.Log($"  - Looking at: {_tableCenter.position}");
                Debug.Log($"  - Distance to table: {Vector3.Distance(playerCamera.transform.position, _tableCenter.position)}");
                Debug.Log($"  - Table should be visible at center!");
            }
            
            // Create player markers for the other 3 players (skip player 0 where camera is)
            for (int i = 1; i < playerCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                
                Vector3 playerPos = _tableCenter.position + new Vector3(
                    Mathf.Sin(angle) * tableRadius,
                    tableHeight + playerMarkerHeight + 0.5f, // Slightly above table
                    Mathf.Cos(angle) * tableRadius
                );
                
                // Create visible player marker
                GameObject marker = null;
                if (playerMarkerPrefab != null)
                {
                    marker = Instantiate(playerMarkerPrefab, playerPos, Quaternion.identity);
                    marker.name = $"PlayerMarker_{i}";
                    marker.transform.LookAt(_tableCenter.position);
                }
                else
                {
                    // Create a BIG visible colored sphere
                    marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marker.name = $"PlayerMarker_{i}";
                    marker.transform.position = playerPos;
                    marker.transform.localScale = Vector3.one * 0.8f; // Big visible size
                    marker.transform.LookAt(_tableCenter.position);
                    
                    // Color based on player index
                    Renderer renderer = marker.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Material mat = new Material(Shader.Find("Standard"));
                        Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };
                        mat.color = colors[i % colors.Length];
                        renderer.material = mat;
                        renderer.enabled = true;
                    }
                }
                
                Debug.Log($"[TableSetup] Created player marker {i} at {playerPos}");
            }
            
        }
        
        public Transform GetTableCenter()
        {
            return _tableCenter;
        }
        
        public float GetTableRadius()
        {
            return tableRadius;
        }
    }
}
