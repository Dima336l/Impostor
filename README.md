# The Impostor - Steam Game

A multiplayer social deduction word game for Steam, built with Unity.

## How to Run

### Requirements
- Unity 6.3 LTS (or compatible version)
- Steam installed and running
- Git

### Setup Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/Dima336l/Impostor.git
   cd Impostor
   ```

2. **Open in Unity**
   - Open Unity Hub
   - Click `Open` → Select the `Impostor` folder
   - Wait for Unity to import assets

3. **Import TextMeshPro** (if prompted)
   - `Window > TextMeshPro > Import TMP Essential Resources`
   - Click "Import" if a dialog appears

4. **Create Steam App ID file**
   - Create a file named `steam_appid.txt` in the project root (same level as `Assets` folder)
   - Add the number `480` to the file (for testing)
   - Save the file

5. **Make sure Steam is running**
   - Launch Steam before testing the game

6. **Press Play**
   - Open the `MainMenu` scene
   - Press Play in Unity
   - You should see "Initializing Steam..." then "Steam initialized. Ready to play!"

## Game Flow

1. **Main Menu** → Create or join lobby
2. **Lobby** → Wait for players, set ready status
3. **Game** → Secret word assigned, players give clues
4. **Voting** → Vote to identify the Impostor
5. **Results** → Next round or game end

## Troubleshooting

- **Steam Not Initializing**: Make sure Steam is running and `steam_appid.txt` exists with `480` in it
- **Compilation Errors**: Make sure TextMeshPro resources are imported (`Window > TextMeshPro > Import TMP Essential Resources`)
- **UI Not Showing**: Check that Canvas Render Mode is set to "Screen Space - Overlay"

## Notes

- Steamworks.NET is already included in the project
- All scenes (MainMenu, Lobby, GameTable) are set up
- The project uses Steam Networking for multiplayer

That's it! Clone, open in Unity, create `steam_appid.txt`, and press Play.
