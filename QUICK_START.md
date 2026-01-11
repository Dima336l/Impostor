# Quick Start Guide - Getting The Impostor Running in Unity

## Prerequisites

1. **Unity 2021.3 LTS or newer** installed
2. **Steam** installed and running (for testing)
3. **Steamworks SDK** downloaded (optional for initial testing)

## Step 1: Open Project in Unity

1. Open Unity Hub
2. Click "Add" and select the `Impostor` project folder
3. Click "Open" to launch Unity
4. Wait for Unity to import assets (first time may take a few minutes)

## Step 2: Install Required Packages

1. **TextMeshPro** (if not already imported):
   - Go to `Window > TextMeshPro > Import TMP Essential Resources`
   - Click "Import" in the dialog

2. **Steamworks.NET** (choose one):
   - Option A: Download from [Steamworks.NET GitHub](https://github.com/rlabrecque/Steamworks.NET/releases)
   - Extract and import the `.unitypackage` file
   - OR use Package Manager if available

## Step 3: Set Up Steam for Testing

1. Create `steam_appid.txt` in the project root folder (same level as `Assets` folder)
2. Add the number `480` (Spacewar test app ID) to the file
3. Save the file

**Note:** For testing, you can use App ID 480 without needing your own Steam App ID.

## Step 4: Create Required Scenes

### Create MainMenu Scene

1. Right-click in `Assets/Scenes` folder → `Create > Scene`
2. Name it `MainMenu`
3. Open the scene
4. Create a Canvas:
   - Right-click in Hierarchy → `UI > Canvas`
   - Set Canvas Scaler to "Scale With Screen Size"
5. Add UI elements:
   - Create Button: Right-click Canvas → `UI > Button - TextMeshPro`
     - Name it "CreateLobbyButton"
     - Set text to "Create Lobby"
   - Create another Button: "JoinLobbyButton" with text "Join Lobby"
   - Create another Button: "QuitButton" with text "Quit"
   - Create Text: Right-click Canvas → `UI > Text - TextMeshPro`
     - Name it "StatusText"
     - Set text to "Initializing..."
6. Add MainMenuUI script:
   - Create empty GameObject: Right-click Hierarchy → `Create Empty`
   - Name it "MainMenuUI"
   - Add Component → Search for "MainMenuUI"
   - Drag UI elements to script fields in Inspector

### Create Lobby Scene

1. Create new scene: `Assets/Scenes/Lobby`
2. Open the scene
3. Create Canvas (same as above)
4. Add UI elements:
   - Create empty GameObject for player list container
   - Create Button: "ReadyButton" with text "Ready"
   - Create Button: "LeaveLobbyButton" with text "Leave Lobby"
   - Create Button: "StartGameButton" with text "Start Game" (hide initially)
   - Create Text: "LobbyInfoText"
   - Create Text: "StatusText"
5. Create LobbyPlayerSlot prefab:
   - Create Button in Canvas
   - Add TextMeshPro child for player name
   - Add Image child for ready indicator (optional)
   - Drag to `Assets/Prefabs/` folder to create prefab
6. Add LobbyUI script to empty GameObject and assign references

### Create GameTable Scene

1. Create new scene: `Assets/Scenes/GameTable`
2. Create Table GameObject:
   - Right-click Hierarchy → `3D Object > Plane` (or Cube)
   - Name it "Table"
   - Position at (0, 0, 0)
   - Tag it as "Table" (Add Tag if needed: Edit > Project Settings > Tags and Layers)
3. Add Camera:
   - Select Main Camera
   - Add Component → "TableCameraController"
   - Assign Table transform to script
4. Create Canvas for UI
5. Add UI elements:
   - Text: "SecretWordText" (for word display)
   - Text: "CurrentPlayerText" (for turn indicator)
   - Text: "RoundText"
   - InputField: "ClueInputField" (TMP Input Field)
   - Button: "SubmitClueButton" with text "Submit"
   - Empty GameObject for "ClueListContainer"
   - Text: "TimerText"
6. Create ClueItem prefab:
   - Create Text in Canvas
   - Drag to `Assets/Prefabs/` folder
7. Add GameUI script and assign references
8. Add VoteUI script (create vote panel with player buttons)

## Step 5: Set Up Build Settings

1. Go to `File > Build Settings`
2. Click "Add Open Scenes" for each scene:
   - MainMenu (should be index 0)
   - Lobby (index 1)
   - GameTable (index 2)
3. Ensure MainMenu is at the top (drag if needed)

## Step 6: Create Manager GameObjects

### In MainMenu Scene

1. Create empty GameObject: "SteamManager"
   - Add Component → "SteamManager"
2. Create empty GameObject: "SteamLobbyManager"
   - Add Component → "SteamLobbyManager"
3. Create empty GameObject: "SteamNetworking"
   - Add Component → "SteamNetworking"
4. Create empty GameObject: "NetworkManager"
   - Add Component → "NetworkManager"
5. Create empty GameObject: "GameManager"
   - Add Component → "GameManager"
6. Create empty GameObject: "WordManager"
   - Add Component → "WordManager"

**Note:** These managers use DontDestroyOnLoad, so they persist between scenes.

## Step 7: Test the Game

1. **Ensure Steam is running** (required!)
2. Press Play in Unity Editor
3. You should see:
   - Main menu with buttons
   - Status text showing "Steam initialized" (if Steam is running)
4. Click "Create Lobby" to test lobby creation
5. Click "Join Lobby" to open Steam overlay

## Troubleshooting

### Steam Not Initializing
- **Solution:** Make sure Steam client is running before pressing Play
- Check that `steam_appid.txt` exists with `480` in it
- Verify you're logged into Steam

### Scripts Not Found
- **Solution:** Wait for Unity to compile scripts (check bottom-right corner)
- If errors appear, check Console window (Window > General > Console)
- Ensure all scripts are in correct folders under `Assets/Scripts/`

### UI Elements Not Showing
- **Solution:** Check Canvas is set to "Screen Space - Overlay"
- Verify Canvas Scaler is set up
- Ensure UI elements are children of Canvas

### Missing References in Inspector
- **Solution:** Assign UI elements to script fields in Inspector
- Drag GameObjects from Hierarchy to script component fields
- Check that prefabs are created and assigned

### Scene Not Loading
- **Solution:** Ensure scenes are added to Build Settings
- Check scene names match exactly: "MainMenu", "Lobby", "GameTable"
- Verify scene files are in `Assets/Scenes/` folder

## Quick Test Checklist

- [ ] Unity project opens without errors
- [ ] TextMeshPro is imported
- [ ] `steam_appid.txt` exists with `480`
- [ ] Steam is running
- [ ] MainMenu scene has Canvas and UI elements
- [ ] MainMenuUI script is attached and references assigned
- [ ] Manager GameObjects created in MainMenu scene
- [ ] Press Play - no console errors
- [ ] Status text shows "Steam initialized"
- [ ] Buttons are clickable

## Next Steps

Once basic setup works:
1. Test lobby creation with friends
2. Set up GameTable scene fully
3. Test multiplayer with 2+ players
4. Add 3D table model and environment
5. Add audio clips to AudioManager
6. Customize UI styling

## Minimal Setup for Quick Testing

If you want to test just the core systems without full UI:

1. Create MainMenu scene with just Canvas and one button
2. Add all manager scripts to empty GameObjects
3. Add simple Debug.Log statements to test Steam initialization
4. Check Console for "Steam initialized successfully" message

This will verify Steam integration works before building full UI.

