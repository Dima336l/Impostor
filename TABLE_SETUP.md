# Table Setup Guide

This guide explains how to set up the 3D table scene with players positioned around it in a POV poker-style view.

## Quick Setup

1. **Open the GameTable Scene**
   - Open `Assets/Scenes/GameTable.unity`

2. **Add TableSetup Component**
   - Create an empty GameObject in the scene (right-click in Hierarchy → Create Empty)
   - Name it "TableSetup"
   - Add the `TableSetup` component to it (Add Component → Scripts → Game → TableSetup)

3. **Configure TableSetup**
   - **Table Settings:**
     - `Table Position`: (0, 0, 0) - Center of the scene
     - `Table Radius`: 2.0 - Distance from table center to player positions
     - `Table Height`: 0.75 - Height of table surface
   - **Player Representation:**
     - `Player Marker Prefab`: (Optional) Leave empty to use simple spheres
     - `Player Marker Height`: 0.0 - Height offset above table
   - **Camera:**
     - `Player Camera`: Drag your Main Camera here (or it will be found automatically)

4. **Optional: Table Prefab**
   - You can create a custom table prefab and assign it to `Table Prefab`
   - If left empty, a simple cylinder will be created as a placeholder
   - The table GameObject must have the tag "Table" for the camera system to find it

5. **Camera Setup**
   - The `TableCameraController` component should be on your Main Camera
   - It will automatically find the table by tag "Table"
   - The camera will be positioned at the local player's seat around the table

## How It Works

- **Table Creation**: The script creates a table at the center (or uses a prefab if provided)
- **Player Positioning**: Players are positioned in a circle around the table, evenly spaced
- **Camera Position**: The local player's camera is positioned at their seat, looking at the table center
- **Player Markers**: Simple colored spheres (or prefabs) mark each player's position

## Customization

### Table Appearance
- Create a custom table model/prefab
- Assign it to `Table Prefab` in TableSetup
- Make sure it has the tag "Table"

### Player Representations
- Create player avatar prefabs
- Assign them to `Player Marker Prefab` in TableSetup
- They will be instantiated at each player's position

### Table Size
- Adjust `Table Radius` to change how far players sit from the table
- Adjust `Table Height` to change the table surface height
- The dialogue boxes will automatically use the same radius

## Integration with Dialogue Boxes

The `GameUI` script automatically:
- Finds the table center from `TableSetup` or by tag "Table"
- Uses the same `tableRadius` for positioning dialogue boxes
- Positions dialogue boxes above each player's position

## Scene Structure

```
GameTable Scene
├── Main Camera (with TableCameraController)
├── TableSetup (GameObject with TableSetup component)
├── Table (created automatically or from prefab)
└── Player Markers (created automatically for each player)
```

## Notes

- The table is created/positioned when the scene starts
- Player positions are calculated based on the number of players
- The local player's camera is positioned at their seat automatically
- All players are evenly spaced around the table in a circle
