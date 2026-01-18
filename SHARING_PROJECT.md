# How to Share the Unity Project with Friends

## Quick Answer
**Yes, this GitHub repo contains the entire Unity project!** Your friend can clone it and open it in Unity.

## How to Share

### Option 1: Share GitHub Repository (Recommended)
1. **Give your friend access:**
   - Go to your GitHub repo: `https://github.com/Dima336l/Impostor`
   - Go to `Settings > Collaborators`
   - Add your friend's GitHub username
   - They'll receive an invitation email

2. **Your friend clones the repo:**
   ```bash
   git clone https://github.com/Dima336l/Impostor.git
   ```

### Option 2: Share Repository Link
- Just send them: `https://github.com/Dima336l/Impostor`
- They can clone it or download as ZIP

## What Your Friend Needs to Do

### Step 1: Clone the Repository
```bash
git clone https://github.com/Dima336l/Impostor.git
cd Impostor
```

### Step 2: Open in Unity
1. Open Unity Hub
2. Click `Open` or `Add`
3. Navigate to the cloned `Impostor` folder
4. Select the project folder
5. Unity will open the project

### Step 3: Install Required Packages
Your friend needs to install:

1. **TextMeshPro:**
   - In Unity: `Window > TextMeshPro > Import TMP Essential Resources`
   - Or it should auto-import when opening the project

2. **Steamworks.NET:**
   - Download from: https://github.com/rlabrecque/Steamworks.NET/releases
   - Download the `.unitypackage` file
   - In Unity: `Assets > Import Package > Custom Package`
   - Select the downloaded `.unitypackage`
   - Import all files

### Step 4: Set Up Steam App ID
1. Create `steam_appid.txt` in the project root (same level as `Assets` folder)
2. Add the number `480` (for testing) or your actual Steam App ID
3. This file is in `.gitignore` so each person needs to create their own

### Step 5: Verify Unity Version
- Check what Unity version you're using
- Your friend should use the **same Unity version** (Unity 6.3 LTS based on your project)
- Unity Hub will prompt to install the correct version if needed

## Important Files in the Repo

✅ **Included (tracked by Git):**
- All scripts (`Assets/Scripts/`)
- All scenes (`Assets/Scenes/`)
- All prefabs (`Assets/Prefabs/`)
- Project settings
- Package manifests

❌ **Excluded (in .gitignore):**
- `Library/` - Unity cache (auto-generated)
- `Temp/` - Temporary files (auto-generated)
- `steam_appid.txt` - Each person creates their own
- Build files
- User-specific settings

## Collaboration Workflow

### When You Make Changes:
```bash
git add .
git commit -m "Description of changes"
git push
```

### When Your Friend Makes Changes:
```bash
git pull  # Get latest changes
# Make their changes
git add .
git commit -m "Their changes"
git push
```

### Avoiding Conflicts:
- **Don't edit the same scene at the same time** - Use scene locking or communicate
- **Coordinate on major changes** - Discuss before making big changes
- **Pull before starting work** - Always `git pull` first
- **Commit often** - Small, frequent commits are better

## Troubleshooting

### "Project won't open"
- Check Unity version matches (Unity 6.3 LTS)
- Make sure all files were cloned (check file count)

### "Missing packages"
- TextMeshPro: Import via `Window > TextMeshPro > Import TMP Essential Resources`
- Steamworks.NET: Download and import the `.unitypackage`

### "Compilation errors"
- Make sure Steamworks.NET is imported
- Check Unity version matches
- Try: `Assets > Reimport All`

### "Steam not working"
- Each person needs their own `steam_appid.txt` file
- Make sure Steam client is running
- Verify logged into Steam

## Best Practices

1. **Always pull before starting work:**
   ```bash
   git pull
   ```

2. **Commit frequently:**
   - Don't wait until everything is done
   - Commit working changes regularly

3. **Use descriptive commit messages:**
   ```bash
   git commit -m "Added player ready indicator"
   ```

4. **Test before pushing:**
   - Make sure the project compiles
   - Test basic functionality

5. **Communicate:**
   - Let each other know what you're working on
   - Coordinate on shared files (scenes, managers)

## Quick Setup Checklist for Your Friend

- [ ] Clone the repository
- [ ] Open in Unity (Unity 6.3 LTS)
- [ ] Import TextMeshPro resources
- [ ] Import Steamworks.NET package
- [ ] Create `steam_appid.txt` with `480`
- [ ] Ensure Steam is running
- [ ] Press Play and test

## Sharing the Repo Link

You can share this link with your friend:
```
https://github.com/Dima336l/Impostor
```

They can:
- **Clone it:** `git clone https://github.com/Dima336l/Impostor.git`
- **Download as ZIP:** Click "Code" > "Download ZIP" on GitHub
- **Fork it:** If they want their own copy

That's it! The entire Unity project is in this repository and ready to share. 🎮
