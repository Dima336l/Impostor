# Steamworks.NET Import Steps

## Step 1: Import the Package

1. Open Unity and your Impostor project
2. Go to: **Assets > Import Package > Custom Package...**
3. Navigate to where you downloaded `Steamworks.NET_2025.163.0.unitypackage`
4. Select the file and click "Open"
5. In the import dialog, make sure **ALL** items are checked
6. Click **"Import"**
7. Wait for Unity to import (may take a minute)

## Step 2: Verify Import

After import, you should see:
- No compilation errors in the Console
- New folders in `Assets/` (Steamworks.NET related)
- All scripts should compile successfully

**Check the Console window** (Window > General > Console):
- If you see errors about `CSteamID` or Steam types, the import may not have completed
- Wait for Unity to finish compiling (check bottom-right corner)

## Step 3: Create steam_appid.txt

1. In your project root folder (same level as `Assets` folder)
2. Create a new file named: `steam_appid.txt`
3. Open it and add just the number: `480`
4. Save the file

**Note:** `480` is the Spacewar test app ID - you can use this for testing without your own Steam App ID.

## Step 4: Ensure Steam is Running

- **IMPORTANT:** Steam client must be running before testing
- Launch Steam and make sure you're logged in
- The game will not initialize without Steam running

## Step 5: Test in Unity

1. Press **Play** in Unity Editor
2. Check the Console for messages:
   - Should see: "Steam initialized successfully"
   - If you see errors, check that Steam is running

## Step 6: Create Your First Scene (If Not Done)

If you haven't created scenes yet:

### MainMenu Scene
1. Right-click in Project: `Assets/Scenes` → Create > Scene
2. Name it: `MainMenu`
3. Open the scene
4. Create Canvas: Right-click Hierarchy → UI > Canvas
5. Add MainMenuUI script to an empty GameObject

### Add Manager GameObjects
In MainMenu scene, create empty GameObjects:
- "SteamManager" → Add Component: SteamManager
- "SteamLobbyManager" → Add Component: SteamLobbyManager  
- "SteamNetworking" → Add Component: SteamNetworking
- "NetworkManager" → Add Component: NetworkManager
- "GameManager" → Add Component: GameManager
- "WordManager" → Add Component: WordManager

## Troubleshooting

### Still See Compilation Errors?
- Wait for Unity to finish compiling (check bottom-right progress bar)
- Try: Assets > Reimport All
- Check Console for specific error messages

### Steam Not Initializing?
- Make sure Steam client is running
- Verify `steam_appid.txt` exists with `480` in it
- Check you're logged into Steam

### Can't Find Import Option?
- Make sure you're in Unity Editor (not just Unity Hub)
- The option is: Assets menu → Import Package → Custom Package

## Next Steps

Once everything compiles and Steam initializes:
1. Set up your UI scenes (see QUICK_START.md)
2. Test lobby creation
3. Test multiplayer with friends
4. Configure your actual Steam App ID when ready

