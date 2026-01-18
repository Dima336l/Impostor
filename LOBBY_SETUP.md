# Lobby Scene Setup Guide

This guide will walk you through creating the Lobby scene for The Impostor game.

## Step 1: Create Lobby Scene

1. In Unity, right-click in `Assets/Scenes/` folder
2. Select `Create > Scene`
3. Name it `Lobby`
4. Double-click to open the scene

## Step 2: Create Canvas

1. Right-click in Hierarchy → `UI > Canvas`
2. Select the Canvas in Inspector:
   - **Render Mode:** `Screen Space - Overlay`
   - **Canvas Scaler:**
     - **UI Scale Mode:** `Scale With Screen Size`
     - **Reference Resolution:** `1920 x 1080`
     - **Match:** `0.5` (width/height)

## Step 3: Create UI Structure

**Note:** Make sure you only have the UI elements listed below. If you see any input fields or other UI elements that aren't mentioned here, delete them - they're not needed for the lobby.

### 3.1 Lobby Info Text (Top)
1. Right-click Canvas → `UI > Text - TextMeshPro`
2. Name: `LobbyInfoText`
3. **Anchor Preset:** Click anchor button → Select **Top** (top-center preset)
4. **Rect Transform:**
   - Pos X: `0` (centered horizontally)
   - Pos Y: `-50` (50 pixels down from top)
   - Width: `400` (or auto-size)
   - Height: `50`
5. **Text Component:**
   - Text: "Lobby"
   - Font Size: `36`
   - Alignment: **Center** (both horizontal and vertical)
   - Auto Size: ✓ (optional, to fit text)

### 3.2 Status Text
1. Right-click Canvas → `UI > Text - TextMeshPro`
2. Name: `StatusText`
3. **Anchor Preset:** Click anchor button → Select **Top** (top-center preset)
4. **Rect Transform:**
   - Pos X: `0` (centered horizontally)
   - Pos Y: `-100` (100 pixels down from top)
   - Width: `400` (or auto-size)
   - Height: `40`
5. **Text Component:**
   - Text: "Waiting for players..."
   - Font Size: `24`
   - Alignment: **Center** (both horizontal and vertical)
   - Auto Size: ✓ (optional, to fit text)

### 3.3 Player List Container
**This container will hold the player slots (one for each player in the lobby).**

1. Right-click Canvas → `UI > Panel`
2. Name: `PlayerListContainer`
3. **Anchor Preset:** Click anchor button → Select **Center** preset
4. **Rect Transform:**
   - Pos X: `0` (centered horizontally)
   - Pos Y: `0` (centered vertically)
   - Width: `600`
   - Height: `400`
5. **Panel Component:** 
   - Set Color to transparent or very dark (Alpha: 0-50) - this is just a container
6. Add Component → `Vertical Layout Group`:
   - **Spacing:** `10` (space between player slots)
   - **Padding:** 
     - Left: `10`
     - Right: `10`
     - Top: `10`
     - Bottom: `10`
   - **Child Alignment:** `Upper Center`
   - **Child Force Expand:** 
     - Width: ✓ (checked)
     - Height: ✗ (unchecked)
7. Add Component → `Content Size Fitter`:
   - **Vertical Fit:** `Preferred Size` (container grows with content)

### 3.4 Buttons (Bottom)

**For each button, set the anchor first, then the position:**

1. **Ready Button:**
   - Right-click Canvas → `UI > Button - TextMeshPro`
   - Name: `ReadyButton`
   - **Anchor Preset:** Click anchor button → Select **Bottom** (bottom-center preset)
   - **Rect Transform:**
     - Pos X: `0`
     - Pos Y: `100` (100 pixels up from bottom)
     - Width: `200`
     - Height: `50`
   - **Button Text:** Change to "Ready"
   - **Text Component:** Font Size: 24, Alignment: Center

2. **Leave Lobby Button:**
   - Right-click Canvas → `UI > Button - TextMeshPro`
   - Name: `LeaveLobbyButton`
   - **Anchor Preset:** Click anchor button → Select **Bottom Left** preset
   - **Rect Transform:**
     - Pos X: `100` (100 pixels from left edge)
     - Pos Y: `50` (50 pixels up from bottom)
     - Width: `200`
     - Height: `50`
   - **Button Text:** Change to "Leave Lobby"
   - **Text Component:** Font Size: 24, Alignment: Center

3. **Start Game Button:**
   - Right-click Canvas → `UI > Button - TextMeshPro`
   - Name: `StartGameButton`
   - **Anchor Preset:** Click anchor button → Select **Bottom Right** preset
   - **Rect Transform:**
     - Pos X: `-100` (100 pixels from right edge - negative because it's measured from right)
     - Pos Y: `100` (100 pixels up from bottom)
     - Width: `200`
     - Height: `50`
   - **Button Text:** Change to "Start Game"
   - **Text Component:** Font Size: 24, Alignment: Center
   - **IMPORTANT:** Uncheck the GameObject checkbox (top-left of Inspector) to hide it initially - only host will see it when ready

## Step 4: Create LobbyPlayerSlot Prefab

**What is this prefab for?**
The `LobbyPlayerSlot` prefab is a template that gets **dynamically created** for each player in the lobby. When a player joins, the LobbyUI script will instantiate (create a copy of) this prefab and add it to the PlayerListContainer. Each slot shows:
- Player's name (left side)
- Ready status indicator (right side - green when ready, gray when not)

You'll create this prefab once, and the game will automatically create instances of it for each player.

### 4.1 Create Prefab in Scene
1. Right-click Canvas → `UI > Button - TextMeshPro`
2. Name: `LobbyPlayerSlot`
3. **Anchor Preset:** Center (doesn't matter, we'll delete from scene)
4. **Rect Transform:**
   - Width: `550`
   - Height: `60`
5. **Button Component:** 
   - You can disable interactivity or leave it - the button is just for styling
   - Set Color to a nice gray (e.g., #2A2A2A)

### 4.2 Add Player Name Text
1. Right-click `LobbyPlayerSlot` → `UI > Text - TextMeshPro`
2. Name: `PlayerNameText`
3. **Anchor Preset:** Click anchor → Select **Left** preset
4. **Rect Transform:**
   - Pos X: `20` (20 pixels from left)
   - Pos Y: `0` (centered vertically)
   - Width: `450` (enough space for name)
   - Height: `40`
5. **Text Component:**
   - Text: "Player Name" (placeholder)
   - Font Size: `24`
   - Alignment: **Left, Middle**
   - Color: White

### 4.3 Add Ready Indicator
1. Right-click `LobbyPlayerSlot` → `UI > Image`
2. Name: `ReadyIndicator`
3. **Anchor Preset:** Click anchor → Select **Right** preset
4. **Rect Transform:**
   - Pos X: `-30` (30 pixels from right - negative because measured from right)
   - Pos Y: `0` (centered vertically)
   - Width: `30`
   - Height: `30`
5. **Image Component:**
   - Color: Gray (#808080) - will turn green when player is ready
   - **Source Image:** Leave empty (will be a colored square) OR use a circle sprite if you have one

### 4.4 Create Prefab
1. Create folder: `Assets/Prefabs/UI/` (if doesn't exist)
2. Drag `LobbyPlayerSlot` from Hierarchy to `Assets/Prefabs/UI/` folder
3. Delete `LobbyPlayerSlot` from the scene (keep the prefab)

## Step 5: Add LobbyUI Script

1. Create Empty GameObject → Name: `LobbyUI`
2. Add Component → `LobbyUI` (Impostor.UI namespace)
3. In Inspector, assign references:
   - **Player List Container:** Drag `PlayerListContainer` from Hierarchy
   - **Player Slot Prefab:** Drag `LobbyPlayerSlot` prefab from `Assets/Prefabs/UI/`
   - **Ready Button:** Drag `ReadyButton` from Hierarchy
   - **Leave Lobby Button:** Drag `LeaveLobbyButton` from Hierarchy
   - **Start Game Button:** Drag `StartGameButton` from Hierarchy
   - **Lobby Info Text:** Drag `LobbyInfoText` from Hierarchy
   - **Status Text:** Drag `StatusText` from Hierarchy

## Step 6: Ensure Managers Exist

The managers should already exist from MainMenu (they use DontDestroyOnLoad), but verify:
- SteamManager
- SteamLobbyManager
- NetworkManager
- GameManager

If they don't exist, create them as Empty GameObjects with their respective scripts.

## Step 7: Add Scene to Build Settings

1. `File > Build Settings`
2. Click "Add Open Scenes" (or drag Lobby.unity into the list)
3. Ensure order is:
   - 0: MainMenu
   - 1: Lobby
   - (GameTable will be added later)

## Step 8: Test

1. Press Play in MainMenu scene
2. Click "Create Lobby"
3. Should transition to Lobby scene
4. You should see yourself in the player list
5. Click "Ready" - button should change
6. As host, "Start Game" button should appear when ready

## Troubleshooting

### "Player slot prefab not assigned"
- Make sure you created the prefab in `Assets/Prefabs/UI/`
- Drag the prefab (not the scene object) to the LobbyUI script

### "UI elements not showing"
- Check Canvas Render Mode is "Screen Space - Overlay"
- Verify all UI elements are children of Canvas
- Check Canvas Scaler settings

### "Scene not found" error
- Make sure Lobby scene is added to Build Settings
- Scene name must be exactly "Lobby" (case-sensitive)

### "Managers not found"
- Managers from MainMenu should persist (DontDestroyOnLoad)
- If missing, create them as Empty GameObjects with scripts

## Next Steps

After Lobby scene works:
- Test with multiple players (create lobby, have friend join)
- Verify ready states sync between players
- Test "Start Game" button (will transition to GameTable scene next)
