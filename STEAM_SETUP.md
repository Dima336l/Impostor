# Steam Integration Setup

The project is now configured to work with Steam. Follow these steps to get it running.

## Required Steps

### 1. Import Steamworks.NET

1. Download Steamworks.NET from: https://github.com/rlabrecque/Steamworks.NET/releases
2. Download the latest `.unitypackage` file
3. In Unity: `Assets > Import Package > Custom Package`
4. Select the downloaded `.unitypackage`
5. Click "Import" and import all files

### 2. Set Up Steam App ID

1. Create `steam_appid.txt` in the project root (same level as `Assets` folder)
2. For testing, add the number `480` (Spacewar test app)
3. For production, use your actual Steam App ID from Steamworks Partner Portal

### 3. Ensure Steam is Running

- **Important:** Steam client must be running before launching the game
- The game will not initialize without Steam running

## Project Structure

All Steam integration files are in `Assets/Scripts/Steam/`:
- `SteamManager.cs` - Core Steam initialization
- `SteamLobbyManager.cs` - Lobby management
- `SteamNetworking.cs` - P2P networking
- `SteamAchievements.cs` - Achievement system
- `SteamRichPresence.cs` - Rich presence status

## Compilation

**Note:** The Steam files will show compilation errors until Steamworks.NET is imported. This is expected.

Once Steamworks.NET is imported:
- All `CSteamID` type errors will resolve
- All Steam API calls will work
- The project will compile successfully

## Testing

1. Ensure Steam is running
2. Open Unity project
3. Import Steamworks.NET (if not done)
4. Press Play
5. Check Console for "Steam initialized successfully"

## Troubleshooting

### Compilation Errors
- **Solution:** Import Steamworks.NET package
- Errors about `CSteamID`, `SteamMatchmaking`, etc. will disappear after import

### Steam Not Initializing
- Ensure Steam client is running
- Check `steam_appid.txt` exists with valid App ID
- Verify you're logged into Steam

### Network Issues
- Check firewall settings
- Ensure Steam Networking is enabled
- Verify all players are friends on Steam (for FriendsOnly lobbies)

## Next Steps

Once Steamworks.NET is imported and Steam is running:
1. Test lobby creation
2. Test joining lobbies
3. Test multiplayer gameplay
4. Configure achievements in Steamworks Partner Portal

