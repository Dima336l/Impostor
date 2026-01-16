# Next Steps to Make The Impostor Game Work

## Current Status ✅

- ✅ All code written and compiles without errors
- ✅ Steamworks.NET imported
- ✅ TextMeshPro added to project
- ✅ All scripts ready to use

## What's Missing ❌

- ❌ Unity scenes not created yet
- ❌ UI not set up
- ❌ Manager GameObjects not created
- ❌ Prefabs not created
- ❌ Scene transitions not configured
- ❌ Testing not done

---

## Step-by-Step Implementation Plan

### Phase 1: Basic Setup (30-60 minutes)

#### 1.1 Create Folder Structure
1. In Unity Project window, create folders:
   - `Assets/Scenes/` (if doesn't exist)
   - `Assets/Prefabs/`
   - `Assets/Prefabs/UI/`
   - `Assets/Models/` (optional for now)

#### 1.2 Create MainMenu Scene
1. **Create Scene:**
   - Right-click `Assets/Scenes/` → `Create > Scene`
   - Name: `MainMenu`
   - Open the scene

2. **Create Canvas:**
   - Right-click Hierarchy → `UI > Canvas`
   - Select Canvas → Inspector:
     - Render Mode: `Screen Space - Overlay`
     - Canvas Scaler → UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`

3. **Add UI Elements:**
   - Right-click Canvas → `UI > Button - TextMeshPro`
     - Name: `CreateLobbyButton`
     - Text: "Create Lobby"
     - Position: Center, Y = 50
   - Create another Button: `JoinLobbyButton` (Text: "Join Lobby", Y = -50)
   - Create another Button: `QuitButton` (Text: "Quit", Y = -150)
   - Right-click Canvas → `UI > Text - TextMeshPro`
     - Name: `StatusText`
     - Text: "Initializing Steam..."
     - Position: Top center

4. **Add MainMenuUI Script:**
   - Create Empty GameObject → Name: `MainMenuUI`
   - Add Component → `MainMenuUI` (Impostor.UI)
   - In Inspector, drag UI elements to script fields:
     - `createLobbyButton` → CreateLobbyButton
     - `joinLobbyButton` → JoinLobbyButton
     - `quitButton` → QuitButton
     - `statusText` → StatusText

5. **Add Manager GameObjects:**
   - Create Empty GameObject → Name: `SteamManager`
     - Add Component → `SteamManager` (Impostor.Steam)
   - Create Empty GameObject → Name: `SteamLobbyManager`
     - Add Component → `SteamLobbyManager` (Impostor.Steam)
   - Create Empty GameObject → Name: `SteamNetworking`
     - Add Component → `SteamNetworking` (Impostor.Steam)
   - Create Empty GameObject → Name: `NetworkManager`
     - Add Component → `NetworkManager` (Impostor.Networking)
   - Create Empty GameObject → Name: `GameManager`
     - Add Component → `GameManager` (Impostor.Game)
   - Create Empty GameObject → Name: `WordManager`
     - Add Component → `WordManager` (Impostor.Game)

6. **Test Basic Setup:**
   - Ensure Steam is running
   - Press Play
   - Check Console for "Steam initialized successfully"
   - Status text should update

---

### Phase 2: Lobby Scene (30-45 minutes)

#### 2.1 Create Lobby Scene
1. Create new scene: `Assets/Scenes/Lobby`
2. Open the scene
3. Create Canvas (same settings as MainMenu)

#### 2.2 Add Lobby UI Elements
1. **Create Player List Container:**
   - Create Empty GameObject → Name: `PlayerListContainer`
   - Add `Vertical Layout Group` component
   - Add `Content Size Fitter` component

2. **Add Buttons:**
   - `ReadyButton` (Text: "Ready")
   - `LeaveLobbyButton` (Text: "Leave Lobby")
   - `StartGameButton` (Text: "Start Game") - Hide initially (uncheck GameObject)

3. **Add Text Elements:**
   - `LobbyInfoText` (shows lobby ID, player count)
   - `StatusText`

#### 2.3 Create LobbyPlayerSlot Prefab
1. In Lobby scene, create Button under Canvas
2. Name: `LobbyPlayerSlot`
3. Add child TextMeshPro → Name: `PlayerNameText`
4. Add child Image → Name: `ReadyIndicator` (optional)
5. Drag to `Assets/Prefabs/UI/` to create prefab
6. Delete from scene (keep prefab)

#### 2.4 Add LobbyUI Script
1. Create Empty GameObject → Name: `LobbyUI`
2. Add Component → `LobbyUI` (Impostor.UI)
3. Assign references:
   - `playerListContainer` → PlayerListContainer
   - `readyButton` → ReadyButton
   - `leaveLobbyButton` → LeaveLobbyButton
   - `startGameButton` → StartGameButton
   - `lobbyInfoText` → LobbyInfoText
   - `statusText` → StatusText
   - `playerSlotPrefab` → LobbyPlayerSlot prefab

---

### Phase 3: Game Scene (45-60 minutes)

#### 3.1 Create GameTable Scene
1. Create new scene: `Assets/Scenes/GameTable`
2. Open the scene

#### 3.2 Create Table
1. Right-click Hierarchy → `3D Object > Plane`
2. Name: `Table`
3. Position: (0, 0, 0)
4. Scale: (2, 1, 2) for a nice table size
5. Add Tag: "Table" (Edit > Project Settings > Tags and Layers)

#### 3.3 Set Up Camera
1. Select Main Camera
2. Add Component → `TableCameraController` (Impostor.Camera)
3. In Inspector:
   - `tableCenter` → Drag Table GameObject
   - Adjust `distanceFromTable` (default 0.5)
   - Adjust `cameraHeight` (default 1.6)

#### 3.4 Create Game UI
1. Create Canvas (same settings)
2. Add UI Elements:
   - `SecretWordText` (TextMeshPro) - Large, center top
   - `CurrentPlayerText` (TextMeshPro) - Shows whose turn
   - `RoundText` (TextMeshPro) - Shows round number
   - `ClueInputField` (TMP Input Field) - For entering clues
   - `SubmitClueButton` (Button) - Submit clue
   - `ClueListContainer` (Empty GameObject) - For clue list
   - `TimerText` (TextMeshPro) - Countdown timer

#### 3.5 Create ClueItem Prefab
1. Create TextMeshPro under Canvas
2. Name: `ClueItem`
3. Drag to `Assets/Prefabs/UI/` to create prefab
4. Delete from scene

#### 3.6 Add GameUI Script
1. Create Empty GameObject → Name: `GameUI`
2. Add Component → `GameUI` (Impostor.UI)
3. Assign all UI element references

#### 3.7 Add VoteUI
1. Create Panel under Canvas → Name: `VotePanel`
2. Hide initially (uncheck GameObject)
3. Add child: `PlayerButtonContainer` (Empty GameObject with Vertical Layout Group)
4. Add child: `NoVoteButton` (Button, Text: "No Vote")
5. Add child: `VoteStatusText` (TextMeshPro)
6. Create VoteButton prefab:
   - Button with TextMeshPro child
   - Drag to `Assets/Prefabs/UI/`
7. Add VoteUI script to VotePanel
8. Assign references

---

### Phase 4: Scene Management (15 minutes)

#### 4.1 Add Scenes to Build Settings
1. `File > Build Settings`
2. Click "Add Open Scenes" for each:
   - MainMenu (index 0)
   - Lobby (index 1)
   - GameTable (index 2)
3. Ensure MainMenu is first (drag if needed)

#### 4.2 Test Scene Transitions
1. In GameManager, verify scene loading code works
2. Test: MainMenu → Lobby → GameTable
3. Check that managers persist (DontDestroyOnLoad)

---

### Phase 5: Testing (30-60 minutes)

#### 5.1 Single Player Test
1. Press Play in MainMenu
2. Verify Steam initializes
3. Click "Create Lobby"
4. Verify lobby created
5. Click "Ready"
6. As host, click "Start Game"
7. Verify GameTable scene loads
8. Test clue submission
9. Test voting

#### 5.2 Multiplayer Test
1. Build game (File > Build and Run)
2. Run 2 instances (one in Editor, one built)
3. Create lobby in one
4. Join from other
5. Test full game flow with 2+ players

---

### Phase 6: Polish (Optional, ongoing)

1. **3D Assets:**
   - Add table model
   - Add player avatars
   - Add environment

2. **Audio:**
   - Add sound effects to AudioManager
   - Add background music

3. **UI Styling:**
   - Customize button styles
   - Add animations
   - Improve layout

4. **Game Balance:**
   - Adjust round timers
   - Test word difficulty
   - Balance voting mechanics

---

## Quick Start Checklist

Use this to track your progress:

- [ ] **Phase 1: Basic Setup**
  - [ ] Scenes folder created
  - [ ] MainMenu scene created
  - [ ] Canvas and UI elements added
  - [ ] MainMenuUI script attached
  - [ ] Manager GameObjects created
  - [ ] Steam initializes successfully

- [ ] **Phase 2: Lobby**
  - [ ] Lobby scene created
  - [ ] Lobby UI elements added
  - [ ] LobbyPlayerSlot prefab created
  - [ ] LobbyUI script attached
  - [ ] Can create/join lobby

- [ ] **Phase 3: Game Scene**
  - [ ] GameTable scene created
  - [ ] Table GameObject created
  - [ ] Camera controller set up
  - [ ] Game UI elements added
  - [ ] ClueItem prefab created
  - [ ] GameUI script attached
  - [ ] VoteUI set up

- [ ] **Phase 4: Scene Management**
  - [ ] Scenes added to Build Settings
  - [ ] Scene transitions work

- [ ] **Phase 5: Testing**
  - [ ] Single player test passes
  - [ ] Multiplayer test passes
  - [ ] Full game flow works

---

## Priority Order

**If you want to test quickly:**

1. **Do Phase 1 only** - This gets you a working main menu and Steam integration
2. Test Steam initialization
3. Then proceed with Phase 2 and 3

**If you want full game:**

1. Complete Phases 1-4 in order
2. Test in Phase 5
3. Polish in Phase 6

---

## Common Issues & Solutions

### "Script references not assigned"
- **Solution:** Drag GameObjects from Hierarchy to script fields in Inspector

### "Scene not found"
- **Solution:** Ensure scenes are in `Assets/Scenes/` and added to Build Settings

### "Steam not initializing"
- **Solution:** 
  - Ensure Steam client is running
  - Check `steam_appid.txt` exists with `480`
  - Verify logged into Steam

### "UI not showing"
- **Solution:**
  - Check Canvas Render Mode is "Screen Space - Overlay"
  - Verify UI elements are children of Canvas
  - Check Canvas Scaler settings

### "Prefab not found"
- **Solution:** Create prefabs in `Assets/Prefabs/UI/` folder first

---

## Estimated Time

- **Phase 1:** 30-60 minutes
- **Phase 2:** 30-45 minutes
- **Phase 3:** 45-60 minutes
- **Phase 4:** 15 minutes
- **Phase 5:** 30-60 minutes
- **Total:** ~3-4 hours for full setup

---

## Next Steps After Setup

Once everything is set up and working:

1. Test with friends
2. Gather feedback
3. Iterate on gameplay
4. Add polish and assets
5. Prepare for Steam release

Good luck! 🎮

