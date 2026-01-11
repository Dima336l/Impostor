# Compilation Notes

## Steam Integration Files Require Steamworks.NET

The following files in `Assets/Scripts/Steam/` **require Steamworks.NET to be imported** before they will compile:

- `SteamManager.cs`
- `SteamLobbyManager.cs`
- `SteamNetworking.cs`
- `SteamAchievements.cs`
- `SteamRichPresence.cs`

These files use Steamworks types like `CSteamID`, `Callback<>`, `SteamMatchmaking`, etc., which are only available when Steamworks.NET is imported into the Unity project.

## Files That Compile Without Steamworks

The following files will compile without Steamworks.NET:

- `Assets/Scripts/Networking/NetworkPlayer.cs` - Uses `ulong` instead of `CSteamID`
- `Assets/Scripts/Game/*.cs` - Core game logic (may reference Steam types but can be made conditional)
- `Assets/Scripts/UI/*.cs` - UI controllers (may reference Steam types)
- `Assets/Scripts/Camera/*.cs` - Camera controllers
- `Assets/Scripts/Audio/*.cs` - Audio management

## Solution

**To compile the entire project:**

1. Import Steamworks.NET into Unity:
   - Download from [Steamworks.NET GitHub](https://github.com/rlabrecque/Steamworks.NET/releases)
   - Import the `.unitypackage` file
   - Unity will automatically recompile

2. **OR** exclude Steam files from compilation until Steamworks is imported:
   - Move `Assets/Scripts/Steam/` folder temporarily
   - Compile the rest of the project
   - Move folder back when ready to test Steam integration

## Testing Compilation Locally

If you want to test compilation without Unity/Steamworks, you would need to:

1. Create mock/stub types for Steamworks types
2. Use conditional compilation directives (`#if STEAMWORKS_NET`)
3. Or accept that Steam files won't compile until Steamworks.NET is imported

**Recommendation:** Import Steamworks.NET in Unity - it's required for the game to function anyway.

