# Fix TextMeshPro Errors

## The Problem

You're seeing errors like:
- `The type or namespace name 'UI' does not exist in the namespace 'UnityEngine'`
- `The type or namespace name 'TMPro' could not be found`

This means **TextMeshPro** hasn't been imported yet.

## Solution: Import TextMeshPro

### Step 1: Import TextMeshPro Essential Resources

1. In Unity, go to: **Window > TextMeshPro > Import TMP Essential Resources**
2. A dialog will appear asking to import
3. Click **"Import"** or **"Import TMP Essentials"**
4. Wait for Unity to import (may take a minute)

### Step 2: Import TextMeshPro Examples & Extras (Optional but Recommended)

1. Go to: **Window > TextMeshPro > Import TMP Examples & Extras**
2. Click **"Import"**
3. This provides additional UI examples (optional but helpful)

### Step 3: Verify Import

After importing:
- Check the Console window
- All `TMPro` and `UnityEngine.UI` errors should disappear
- The project should compile successfully

## What TextMeshPro Provides

- `TextMeshProUGUI` - Modern text component
- `TMP_InputField` - Input fields
- All UI components (Button, Slider, etc.) are part of Unity's UI system

## If Import Option Doesn't Appear

If you don't see the TextMeshPro menu:
1. TextMeshPro might already be installed via Package Manager
2. Check: **Window > Package Manager**
3. Look for "TextMeshPro" in the list
4. If it's there, you may need to import resources manually:
   - Go to: `Assets/TextMesh Pro/Resources/`
   - If the folder exists, resources are already imported

## After Import

Once TextMeshPro is imported:
- All UI script errors will resolve
- You can use TextMeshPro components in your UI
- The project should compile without errors

