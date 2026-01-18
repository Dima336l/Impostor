# Recreate Lobby UI - Step by Step

This guide will help you recreate the Lobby UI from scratch with correct positioning.

## Step 1: Clean Up Current UI

1. In Hierarchy, select `Canvas`
2. Delete all children under Canvas EXCEPT `EventSystem` (keep EventSystem)
3. You should only have `EventSystem` left under Canvas

## Step 2: Create LobbyInfoText (Top Title)

1. Right-click `Canvas` → `UI > Text - TextMeshPro`
2. Name: `LobbyInfoText`
3. **Rect Transform:**
   - Expand "Anchors" section
   - Set: Min X: `0.5`, Min Y: `1`, Max X: `0.5`, Max Y: `1` (top-center anchor)
   - Pos X: `0`
   - Pos Y: `-50` (50 pixels down from top)
   - Pos Z: `0`
   - Width: `400`
   - Height: `50`
4. **TextMeshPro Component:**
   - Text: `Lobby`
   - Font Size: `36`
   - Alignment: Center (both horizontal and vertical)
   - Color: White (#FFFFFF)

## Step 3: Create StatusText (Below Title)

1. Right-click `Canvas` → `UI > Text - TextMeshPro`
2. Name: `StatusText`
3. **Rect Transform:**
   - Expand "Anchors" section
   - Set: Min X: `0.5`, Min Y: `1`, Max X: `0.5`, Max Y: `1` (top-center anchor)
   - Pos X: `0`
   - Pos Y: `-100` (100 pixels down from top)
   - Pos Z: `0`
   - Width: `400`
   - Height: `40`
4. **TextMeshPro Component:**
   - Text: `Waiting for players...`
   - Font Size: `24`
   - Alignment: Center (both horizontal and vertical)
   - Color: White (#FFFFFF)

## Step 4: Create PlayerListContainer (Center)

1. Right-click `Canvas` → `UI > Panel`
2. Name: `PlayerListContainer`
3. **Rect Transform:**
   - Expand "Anchors" section
   - Set: Min X: `0.5`, Min Y: `0.5`, Max X: `0.5`, Max Y: `0.5` (center anchor)
   - Pos X: `0`
   - Pos Y: `0`
   - Pos Z: `0`
   - Width: `600`
   - Height: `400`
4. **Panel Component:**
   - Color: Set Alpha to `0` (fully transparent - it's just a container)
5. **Add Component → Vertical Layout Group:**
   - Spacing: `10`
   - Padding: Left `10`, Right `10`, Top `10`, Bottom `10`
   - Child Alignment: `Upper Center`
   - Child Force Expand: Width ✓, Height ✗
6. **Add Component → Content Size Fitter:**
   - Vertical Fit: `Preferred Size`

## Step 5: Create ReadyButton (Bottom Center)

1. Right-click `Canvas` → `UI > Button - TextMeshPro`
2. Name: `ReadyButton`
3. **Rect Transform:**
   - Expand "Anchors" section
   - Set: Min X: `0.5`, Min Y: `0`, Max X: `0.5`, Max Y: `0` (bottom-center anchor)
   - Pos X: `0`
   - Pos Y: `100` (100 pixels up from bottom)
   - Pos Z: `0`
   - Width: `200`
   - Height: `50`
4. **Button Text:**
   - Select the child `Text (TMP)` under ReadyButton
   - Text: `Ready`
   - Font Size: `24`
   - Alignment: Center
   - Color: White

## Step 6: Create LeaveLobbyButton (Bottom Left)

1. Right-click `Canvas` → `UI > Button - TextMeshPro`
2. Name: `LeaveLobbyButton`
3. **Rect Transform:**
   - Expand "Anchors" section
   - Set: Min X: `0`, Min Y: `0`, Max X: `0`, Max Y: `0` (bottom-left anchor)
   - Pos X: `100` (100 pixels from left edge)
   - Pos Y: `50` (50 pixels up from bottom)
   - Pos Z: `0`
   - Width: `200`
   - Height: `50`
4. **Button Text:**
   - Select the child `Text (TMP)` under LeaveLobbyButton
   - Text: `Leave Lobby`
   - Font Size: `24`
   - Alignment: Center
   - Color: White

## Step 7: Create StartGameButton (Bottom Right)

1. Right-click `Canvas` → `UI > Button - TextMeshPro`
2. Name: `StartGameButton`
3. **Rect Transform:**
   - Expand "Anchors" section
   - Set: Min X: `1`, Min Y: `0`, Max X: `1`, Max Y: `0` (bottom-right anchor)
   - Pos X: `-100` (100 pixels from right edge - negative because measured from right)
   - Pos Y: `100` (100 pixels up from bottom)
   - Pos Z: `0`
   - Width: `200`
   - Height: `50`
4. **Button Text:**
   - Select the child `Text (TMP)` under StartGameButton
   - Text: `Start Game`
   - Font Size: `24`
   - Alignment: Center
   - Color: White
5. **IMPORTANT:** Uncheck the GameObject checkbox (top-left of Inspector) to hide it - only host will see it

## Step 8: Verify LobbyPlayerSlot Prefab Exists

1. In Project, go to `Assets/Prefabs/UI/`
2. Check if `LobbyPlayerSlot` prefab exists
3. If it doesn't exist, you'll need to create it (see Step 9)

## Step 9: Create LobbyPlayerSlot Prefab (If Needed)

1. Right-click `Canvas` → `UI > Button - TextMeshPro`
2. Name: `LobbyPlayerSlot`
3. **Rect Transform:**
   - Width: `550`
   - Height: `60`
4. **Button Component:**
   - Color: Dark gray (#2A2A2A)

### Add PlayerNameText Child:
1. Right-click `LobbyPlayerSlot` → `UI > Text - TextMeshPro`
2. Name: `PlayerNameText`
3. **Rect Transform:**
   - Expand "Anchors"
   - Set: Min X: `0`, Min Y: `0.5`, Max X: `0`, Max Y: `0.5` (left-center anchor)
   - Pos X: `20`
   - Pos Y: `0`
   - Pos Z: `0`
   - Width: `450`
   - Height: `40`
4. **TextMeshPro:**
   - Text: `Player Name`
   - Font Size: `24`
   - Alignment: Left, Middle
   - Color: White

### Add ReadyIndicator Child:
1. Right-click `LobbyPlayerSlot` → `UI > Image`
2. Name: `ReadyIndicator`
3. **Rect Transform:**
   - Expand "Anchors"
   - Set: Min X: `1`, Min Y: `0.5`, Max X: `1`, Max Y: `0.5` (right-center anchor)
   - Pos X: `-30`
   - Pos Y: `0`
   - Pos Z: `0`
   - Width: `30`
   - Height: `30`
4. **Image Component:**
   - Color: Gray (#808080)
   - Source Image: Leave empty

### Create Prefab:
1. Drag `LobbyPlayerSlot` from Hierarchy to `Assets/Prefabs/UI/` folder
2. Delete `LobbyPlayerSlot` from scene (keep prefab)

## Step 10: Create LobbyUI GameObject

1. Right-click in Hierarchy (not on Canvas) → `Create Empty`
2. Name: `LobbyUI`
3. **Add Component → LobbyUI** (search for "LobbyUI" in Add Component)

## Step 11: Assign LobbyUI References

1. Select `LobbyUI` in Hierarchy
2. In Inspector, find LobbyUI component
3. Assign each field by dragging from Hierarchy or Project:

   - **Player List Container:** Drag `PlayerListContainer` from Hierarchy
   - **Player Slot Prefab:** Drag `LobbyPlayerSlot` prefab from `Assets/Prefabs/UI/`
   - **Ready Button:** Drag `ReadyButton` from Hierarchy
   - **Leave Lobby Button:** Drag `LeaveLobbyButton` from Hierarchy
   - **Start Game Button:** Drag `StartGameButton` from Hierarchy
   - **Lobby Info Text:** Drag `LobbyInfoText` from Hierarchy
   - **Status Text:** Drag `StatusText` from Hierarchy

## Step 12: Test

1. Press Play
2. You should see:
   - "Lobby" text at top
   - "Waiting for players..." below it
   - Empty player list in center
   - "Ready" button at bottom center
   - "Leave Lobby" button at bottom left
   - "Start Game" button should be hidden (unchecked)

## Troubleshooting

### If text is still not visible:
- Check TextMeshPro is imported: `Window > TextMeshPro > Import TMP Essential Resources`
- Verify text color is White with Alpha 255
- Check you're viewing Game tab, not Scene tab

### If buttons are off-screen:
- Verify anchor values are correct (Min/Max X and Y)
- Check Pos Z is always `0`
- Make sure you're setting anchors BEFORE setting position

### If prefab doesn't work:
- Make sure LobbyPlayerSlot is in `Assets/Prefabs/UI/`
- Verify it has both PlayerNameText and ReadyIndicator as children
- Check LobbyUI script has Player Slot Prefab assigned
