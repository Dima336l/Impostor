# Phase 1: Basic Setup - Step-by-Step Checklist

## Step 1: Create Folder Structure ✅

**In Unity Project Window:**
- [ ] Right-click `Assets` → `Create > Folder` → Name: `Scenes`
- [ ] Right-click `Assets` → `Create > Folder` → Name: `Prefabs`
- [ ] Right-click `Prefabs` → `Create > Folder` → Name: `UI`

**Status:** Ready to proceed

---

## Step 2: Create MainMenu Scene

**2.1 Create the Scene:**
- [ ] Right-click `Assets/Scenes/` → `Create > Scene`
- [ ] Name it: `MainMenu`
- [ ] Double-click to open the scene

**2.2 Create Canvas:**
- [ ] Right-click in Hierarchy → `UI > Canvas`
- [ ] Select Canvas in Hierarchy
- [ ] In Inspector, set:
  - [ ] Render Mode: `Screen Space - Overlay` (should be default)
  - [ ] Find "Canvas Scaler" component
  - [ ] UI Scale Mode: `Scale With Screen Size`
  - [ ] Reference Resolution: `X: 1920, Y: 1080`

**2.3 Add UI Buttons:**
- [ ] Right-click Canvas in Hierarchy → `UI > Button - TextMeshPro`
  - [ ] Name it: `CreateLobbyButton`
  - [ ] In Inspector, find "TextMeshPro - Text (UI)" child
  - [ ] Change text to: "Create Lobby"
  - [ ] In RectTransform, set position: X=0, Y=50
- [ ] Right-click Canvas → `UI > Button - TextMeshPro`
  - [ ] Name: `JoinLobbyButton`
  - [ ] Text: "Join Lobby"
  - [ ] Position: X=0, Y=-50
- [ ] Right-click Canvas → `UI > Button - TextMeshPro`
  - [ ] Name: `QuitButton`
  - [ ] Text: "Quit"
  - [ ] Position: X=0, Y=-150

**2.4 Add Status Text:**
- [ ] Right-click Canvas → `UI > Text - TextMeshPro`
  - [ ] Name: `StatusText`
  - [ ] Text: "Initializing Steam..."
  - [ ] Position: X=0, Y=400 (top center)
  - [ ] Font Size: 24
  - [ ] Alignment: Center

---

## Step 3: Add MainMenuUI Script

- [ ] Right-click Hierarchy → `Create Empty`
- [ ] Name it: `MainMenuUI`
- [ ] Select MainMenuUI in Hierarchy
- [ ] Click "Add Component" button
- [ ] Search for: `MainMenuUI`
- [ ] Click to add component
- [ ] In Inspector, you'll see fields that need references:
  - [ ] Drag `CreateLobbyButton` from Hierarchy to `Create Lobby Button` field
  - [ ] Drag `JoinLobbyButton` to `Join Lobby Button` field
  - [ ] Drag `QuitButton` to `Quit Button` field
  - [ ] Drag `StatusText` to `Status Text` field
  - [ ] Leave `Loading Panel` empty for now (optional)

---

## Step 4: Create Manager GameObjects

**4.1 SteamManager:**
- [ ] Right-click Hierarchy → `Create Empty`
- [ ] Name: `SteamManager`
- [ ] Add Component → Search: `SteamManager` (Impostor.Steam)
- [ ] ✅ Done

**4.2 SteamLobbyManager:**
- [ ] Right-click Hierarchy → `Create Empty`
- [ ] Name: `SteamLobbyManager`
- [ ] Add Component → `SteamLobbyManager` (Impostor.Steam)
- [ ] ✅ Done

**4.3 SteamNetworking:**
- [ ] Right-click Hierarchy → `Create Empty`
- [ ] Name: `SteamNetworking`
- [ ] Add Component → `SteamNetworking` (Impostor.Steam)
- [ ] ✅ Done

**4.4 NetworkManager:**
- [ ] Right-click Hierarchy → `Create Empty`
- [ ] Name: `NetworkManager`
- [ ] Add Component → `NetworkManager` (Impostor.Networking)
- [ ] ✅ Done

**4.5 GameManager:**
- [ ] Right-click Hierarchy → `Create Empty`
- [ ] Name: `GameManager`
- [ ] Add Component → `GameManager` (Impostor.Game)
- [ ] ✅ Done

**4.6 WordManager:**
- [ ] Right-click Hierarchy → `Create Empty`
- [ ] Name: `WordManager`
- [ ] Add Component → `WordManager` (Impostor.Game)
- [ ] ✅ Done

---

## Step 5: Verify steam_appid.txt

- [ ] Check if `steam_appid.txt` exists in project root (same level as Assets folder)
- [ ] If not, create it with content: `480`
- [ ] Save the file

---

## Step 6: Test!

**Before testing:**
- [ ] Ensure Steam client is running
- [ ] Make sure you're logged into Steam

**Test:**
- [ ] Press Play button in Unity
- [ ] Check Console window (Window > General > Console)
- [ ] Look for: "Steam initialized successfully"
- [ ] Check StatusText in Game view - should update
- [ ] Try clicking "Create Lobby" button

**Expected Results:**
- ✅ No errors in Console
- ✅ Status text shows "Steam initialized" or similar
- ✅ Buttons are clickable
- ✅ Console shows lobby creation messages

---

## Troubleshooting

**If Steam doesn't initialize:**
- Check Steam is running
- Verify `steam_appid.txt` exists with `480`
- Check Console for error messages

**If buttons don't work:**
- Verify MainMenuUI script has all references assigned
- Check Console for errors
- Ensure buttons are children of Canvas

**If scripts not found:**
- Wait for Unity to finish compiling (check bottom-right)
- Check Console for compilation errors

---

## Next Steps

Once Phase 1 works:
- Proceed to Phase 2: Lobby Scene
- Or test lobby creation with friends

