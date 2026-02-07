# Manual UI Setup for Player Clue Text

Instead of creating text programmatically, you can set it up manually in Unity Editor. Here's how:

## Step 1: Create World Space Canvas

1. In Unity Hierarchy, right-click → **UI → Canvas**
2. Select the Canvas in Inspector
3. Set **Render Mode** to **World Space**
4. Set **Rect Transform**:
   - **Width**: 100
   - **Height**: 100
   - **Scale**: X=0.1, Y=0.1, Z=0.1 (this makes UI elements appear at reasonable size)
   - **Position**: X=0, Y=0, Z=0 (at table center)

5. In Canvas component:
   - **Render Camera**: Drag Main Camera here
   - **Event Camera**: Drag Main Camera here

6. Rename it to **"WorldSpaceDialogueCanvas"**

## Step 2: Create Text Objects for Each Player

For each player (4 players = 4 text objects):

1. Right-click on **WorldSpaceDialogueCanvas** → **UI → Text - TextMeshPro**
   - If prompted, import TMP Essentials (click "Import TMP Essentials")

2. For each text object:
   - **Name**: `ClueText_Player1`, `ClueText_Player2`, etc.
   - **Rect Transform**:
     - **Width**: 800
     - **Height**: 150
     - **Position**: Position above where each player marker will be
       - Player 1: X=0, Y=2.5, Z=2 (above player at table position)
       - Player 2: X=2, Y=2.5, Z=0
       - Player 3: X=0, Y=2.5, Z=-2
       - Player 4: X=-2, Y=2.5, Z=0

3. **TextMeshProUGUI** settings:
   - **Text**: Leave empty (will be set by script)
   - **Font Size**: 120
   - **Alignment**: Center (both horizontal and vertical)
   - **Color**: White
   - **Font Style**: Bold
   - **Outline**: 
     - **Width**: 0.3
     - **Color**: Black
   - **Word Wrapping**: OFF
   - **Overflow**: Overflow

4. **RectTransform** settings:
   - **Anchor**: Center (0.5, 0.5)
   - **Pivot**: Center (0.5, 0.5)

## Step 3: Update the Script

The script needs to be modified to:
1. Find these text objects instead of creating them
2. Update their text when clues are submitted
3. Position them above player markers dynamically

## Alternative: Use a Simpler Approach

You could also:
1. Create ONE text prefab
2. Instantiate it 4 times in code
3. Position each instance above the corresponding player marker

Would you like me to modify the script to work with manually created UI elements, or would you prefer to keep the programmatic approach but make it easier to configure?
