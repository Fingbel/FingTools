using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

#if UNITY_EDITOR
namespace FingTools.Internal
{
public class ActorEditorWindow : EditorWindow
{
    public static event Action OnActorAvailableUpdated;
    public static event Action<Actor_SO> OnActorUpdated;
    private string actorName;
    private SpritePart_SO body;
    private SpritePart_SO outfit;
    private SpritePart_SO eyes;
    private SpritePart_SO hairstyle;
    private SpritePart_SO accessory;
    private string tempActorName;
    
    private Actor_SO selectedActor;

    // Snapshot to track changes
    private Actor_SO originalactorData;

    // CharacterPartsManager instance
    private SpriteManager spriteManager;

    // Lists and indexes for sprite sheets
    private List<SpritePart_SO> bodySheets = new List<SpritePart_SO>();
    private List<SpritePart_SO> outfitSheets = new List<SpritePart_SO>();
    private List<SpritePart_SO> eyesSheets = new List<SpritePart_SO>();
    private List<SpritePart_SO> hairstyleSheets = new List<SpritePart_SO>();
    private List<SpritePart_SO> accessorySheets = new List<SpritePart_SO>();

    private int bodySheetIndex = 0;
    private int outfitSheetIndex = 0;
    private int eyesSheetIndex = 0;
    private int hairstyleSheetIndex = 0;
    private int accessorySheetIndex = 0;

    private int globalIndex = 3;
    private int maxIndex = 3;
    string actorsFolderPath = "Assets/Resources/FingTools/Actors";
    private Vector2 scrollPosition = Vector2.zero;

    [MenuItem("FingTools/Actor Editor")]
    public static void ShowWindow()
    {
        ActorEditorWindow window = GetWindow<ActorEditorWindow>();
        window.titleContent = new GUIContent("Actor Editor");

        window.Show();
    }

    public static void ShowWindow(string _actorName,NPCSpawner npcSpawner )
    {
        ActorEditorWindow window = GetWindow<ActorEditorWindow>();
        window.titleContent = new GUIContent(_actorName);
        window.CreateNewActor(_actorName,npcSpawner);        
        window.Show();

    }

    public static void SetActorToPreview(Actor_SO actor)
    {
        if (actor != null)
        {
            ActorEditorWindow window = GetWindow<ActorEditorWindow>();
            window.selectedActor = actor;
            window.LoadActorData(actor);
        }
    }

    [MenuItem("FingTools/Actor Editor",true)]
    public static bool ValidateActorEditWindow()
    {
        if(Directory.Exists("Assets/Resources/FingTools"))
        {
            if(Resources.Load<SpriteManager>("FingTools/SpriteManager").HasAssetsImported() == true)
            {
                return true;
            }
        }
        return false;
    }

    private void OnEnable()
    {
        spriteManager = Resources.Load<SpriteManager>("FingTools/SpriteManager");
        if (spriteManager == null)
        {
            Debug.LogError("Sprite Manager not found in Resources folder.");
        }
        LoadSpriteSheets();
        if(!Directory.Exists("Assets/Resources/FingTools/Actors"))
        {
            Directory.CreateDirectory("Assets/Resources/FingTools/Actors");
        }
        if (selectedActor != null)
        {
            LoadActorData(selectedActor);
        }
    }

    private void LoadSpriteSheets()
    {
        if (spriteManager == null)
        {
            return;
        }

        bodySheets = spriteManager.bodyParts;
        outfitSheets = spriteManager.outfitParts;
        eyesSheets = spriteManager.eyeParts;
        hairstyleSheets = spriteManager.hairstyleParts;
        accessorySheets = spriteManager.accessoryParts;

        bodySheetIndex = bodySheets.Count > 0 ? 0 : -1;
        outfitSheetIndex = outfitSheets.Count > 0 ? 0 : -1;
        eyesSheetIndex = eyesSheets.Count > 0 ? 0 : -1;
        hairstyleSheetIndex = hairstyleSheets.Count > 0 ? 0 : -1;
        accessorySheetIndex = accessorySheets.Count > 0 ? 0 : -1;

        globalIndex = Mathf.Clamp(globalIndex, 0, maxIndex);
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();  
        GUILayout.BeginVertical(GUILayout.Width(200));        
        
        DrawActorList();
        GUILayout.EndVertical();

        if (selectedActor == null)
        {
            // No Actor selected: Show message and input field for new Actor
            GUILayout.BeginVertical(GUILayout.Width(200));

            GUILayout.Label("No Actor selected", EditorStyles.boldLabel);
            GUILayout.Label("Select an Actor from the list or create a new one.", EditorStyles.label);
            GUILayout.Space(10);
        
            // Button to create a new Actor
            if (GUILayout.Button("Create New Actor", GUILayout.Width(150)))
            {
                CreateNewActor("NewActor");
            }
        GUILayout.EndVertical();       
        }
        else
        {
            DrawActorInfoAndPreview();
            DrawPartSelectors();
            
        }        
        GUILayout.EndHorizontal();

        // Handle Enter key press for the name input field
        HandleEnterKeyPress();
    }


    private void HandleEnterKeyPress()
    {
        Event e = Event.current;

        // Check for Enter key press when in the GUI
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
        {
            // Only act if a text field is focused
            if (GUI.GetNameOfFocusedControl() == "TempNameField")
            {
                // Trigger the same action as the checkmark button
                UpdateActorName();

                // Remove focus from the text field
                GUI.FocusControl(null);

                // Consume the event to prevent further processing
                e.Use();
            }
        }
    }

    private void UpdateActorName()
    {
        var actors = Resources.LoadAll<Actor_SO>("FingTools/Actors");
        foreach (var actorName in actors)
        {
            if(tempActorName == actorName.name)
            {
                EditorUtility.DisplayDialog("Error", "An Actor with this name already exists.", "OK");          
                tempActorName = string.Empty;  
                GUI.FocusControl(null);
                return;
            }
        }
        if (selectedActor != null)
        {        
            actorName = tempActorName;        
            // Rename the asset to reflect the updated name
            RenameSelectedActorAsset(actorName);
            
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(selectedActor);
        }
    }
    
    private void DrawActorList()
        {
            GUILayout.BeginVertical(GUILayout.Width(200));            
            GUILayout.Label("Select Actor", EditorStyles.boldLabel);

            string[] actorGUIDs = AssetDatabase.FindAssets("t:Actor_SO", new[] { actorsFolderPath });
            List<Actor_SO> actorAssets = actorGUIDs
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<Actor_SO>(path))
                .Where(actor => actor != null)
                .ToList();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var actor in actorAssets)
            {
                if (GUILayout.Button(actor.name, GUILayout.Height(30)))
                {
                    tempActorName = string.Empty;
                    GUI.FocusControl(null);
                    selectedActor = actor;
                    LoadActorData(selectedActor);

                }
            }
            GUILayout.EndScrollView();



            GUILayout.EndVertical();
        }

        private void DrawSaveDiscardButtons()
        {
            // Check if changes have been made
            bool hasChanges = CheckForChanges();

            // Set GUI.enabled based on whether changes are detected
            bool originalGUIState = GUI.enabled;
            GUI.enabled = hasChanges;

            GUILayout.BeginHorizontal();

            // Discard Changes Button
            GUIStyle customStyle = new GUIStyle(EditorStyles.radioButton);
            customStyle.fontSize = 14;
            customStyle.normal.textColor = Color.red;
            
            if (GUILayout.Button("Discard Changes", GUILayout.Height(30), GUILayout.Width(120)))
            {
                DiscardActorChanges();
            }
            GUILayout.Space(20);
            // Save modifications Button
            if (GUILayout.Button("Save Actor", GUILayout.Height(30), GUILayout.Width(120)))
            {
                if (body != null)
                    SaveActor();
                else
                    EditorUtility.DisplayDialog("Error", "An actor canno't be saved without a body", "OK");
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            // Restore original GUI.enabled state
            GUI.enabled = originalGUIState;
        }

        private void LoadActorData(Actor_SO actor)
    {
        actorName = actor.name;    
        body = actor.body;
        outfit = actor.outfit;
        eyes = actor.eyes;
        hairstyle = actor.hairstyle;
        accessory = actor.accessory;

        // Update sheet indices, default to -1 if part is null
        bodySheetIndex = body != null ? bodySheets.IndexOf(body) : -1;
        outfitSheetIndex = outfit != null ? outfitSheets.IndexOf(outfit) : -1;
        eyesSheetIndex = eyes != null ? eyesSheets.IndexOf(eyes) : -1;
        hairstyleSheetIndex = hairstyle != null ? hairstyleSheets.IndexOf(hairstyle) : -1;
        accessorySheetIndex = accessory != null ? accessorySheets.IndexOf(accessory) : -1;

        // Create a snapshot of the current data
        originalactorData = CreateSnapshot(actor);

        Repaint();
    }

    private Actor_SO CreateSnapshot(Actor_SO actor)
    {
        Actor_SO snapshot = ScriptableObject.CreateInstance<Actor_SO>();
        snapshot.body = actor.body;
        snapshot.outfit = actor.outfit;
        snapshot.eyes = actor.eyes;
        snapshot.hairstyle = actor.hairstyle;
        snapshot.accessory = actor.accessory;
        return snapshot;
    }

    private void ClearActorData(string _actorName)
    {
        actorName = _actorName;
        body = null;
        outfit = null;
        eyes = null;
        hairstyle = null;
        accessory = null;

        bodySheetIndex = -1;
        outfitSheetIndex = -1;
        eyesSheetIndex = -1;
        hairstyleSheetIndex = -1;
        accessorySheetIndex = -1;

        // Reset maxIndex as needed
        Repaint();
    }

    private void DrawActorInfoAndPreview() 
    {
        if(!Directory.Exists(actorsFolderPath))
        {
            Directory.CreateDirectory(actorsFolderPath);
        }
        GUILayout.BeginVertical(GUILayout.Width(300));

        GUILayout.BeginHorizontal();
        // "Create New Actor" Button
        if (GUILayout.Button("Create New Actor", GUILayout.Width(150)))
        {
            CreateNewActor("NewActor");
        }

        // Delete Selected Actor Button
        if (selectedActor != null)
        {
            if (GUILayout.Button("Delete this Actor",GUILayout.Width(150)))
            {
                DeleteSelectedActor();
            }
        }
        GUILayout.EndHorizontal();

        // Actor Info
        GUILayout.Label("Actor Information", EditorStyles.boldLabel);
        
        GUILayout.BeginHorizontal();

        // Display current Actor name
        GUILayout.Label("Current Name: " + selectedActor?.name, EditorStyles.label, GUILayout.Width(150));

        // Input field for new name    
        tempActorName = EditorGUILayout.TextField(tempActorName);

        // Checkmark button to confirm the name change
        if (GUILayout.Button("✔", GUILayout.Width(30)))
        {
            actorName = tempActorName;            
            
            // Call the method to rename the asset
            UpdateActorName();
            tempActorName = string.Empty;
            GUI.FocusControl(null);

            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(selectedActor);        
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Actor Preview
        GUILayout.Label("Actor Preview", EditorStyles.boldLabel);
        Rect previewRect = GUILayoutUtility.GetRect(200, 200);
        DrawActorPreview(previewRect);

        GUILayout.Space(10);

        // Navigation buttons
        DrawNavigationButtons();

        GUILayout.Space(20);
        GUILayout.EndVertical();

        // Force repaint to ensure GUI state is updated
        Repaint();
    }

private void DeleteSelectedActor()
    {
    if (selectedActor == null)
    {
        Debug.LogError("No Actor selected to delete.");
        return;
    }

    // Confirm deletion
    if (EditorUtility.DisplayDialog("Confirm Deletion", $"Are you sure you want to delete the Actor '{selectedActor.name}'?", "Delete", "Cancel"))
    {
        // Get the asset path and delete it
        string assetPath = AssetDatabase.GetAssetPath(selectedActor);
        AssetDatabase.DeleteAsset(assetPath);
        
        // Clear the selection
        selectedActor = null;
        ClearActorData("NewActor");

        // Refresh the asset database
        AssetDatabase.Refresh();        
    }
    OnActorAvailableUpdated?.Invoke();
}
private void CreateNewActor(string _actorName, NPCSpawner npcSpawner = null)
{    
    // Define the default name
    string actorNameToUse = GetUniqueActorName(_actorName);

    // Check if an Actor with the generated name already exists
    string actorAssetPath = $"Assets/Resources/FingTools/Actors/{actorNameToUse}.asset";
    if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(actorAssetPath) != null)
    {
        EditorUtility.DisplayDialog("Error", "An Actor with this name already exists.", "OK");
        return;
    }

    // Create the NPC asset
    Actor_SO newNPC = ScriptableObject.CreateInstance<Actor_SO>();
    newNPC.name = actorNameToUse;

    // Save the NPC asset
    AssetDatabase.CreateAsset(newNPC, actorAssetPath);
    AssetDatabase.SaveAssets();

    // Set the newly created NPC as the selected NPC
    selectedActor = newNPC;
    selectedActor.name = actorNameToUse;    
    tempActorName = string.Empty; // Clear the input field
    GUI.FocusControl(null);
    ClearActorData(_actorName);

    // If an NPCSpawner was passed, assign the new Actor_SO to its npcTemplate
    if (npcSpawner != null)
    {
        npcSpawner.npcTemplate = newNPC; // Assign the new Actor_SO to the NPCSpawner
        EditorUtility.SetDirty(npcSpawner); // Mark the NPCSpawner as dirty to save changes
    }

    // Refresh the AssetDatabase
    AssetDatabase.Refresh();
    OnActorAvailableUpdated?.Invoke();
}

private string GetUniqueActorName(string baseName)
{
    int index = 1;
    string uniqueName = baseName;
    
    if(!Directory.Exists(actorsFolderPath))
    {
        Directory.CreateDirectory(actorsFolderPath);
    }
    // Check if a name with the baseName or a suffixed version already exists
    while (AssetDatabase.FindAssets($"t:Actor_SO", new[] { actorsFolderPath })
                        .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                        .Any(path => Path.GetFileNameWithoutExtension(path) == uniqueName))
    {
        uniqueName = $"{baseName}_{index}";
        index++;
    }

    return uniqueName;
}
    private void DiscardActorChanges()
{
    if (originalactorData != null)
    {
        // Restore data from the snapshot
        actorName = originalactorData.name;
        body = originalactorData.body;
        outfit = originalactorData.outfit;
        eyes = originalactorData.eyes;
        hairstyle = originalactorData.hairstyle;
        accessory = originalactorData.accessory;

        // Update sheet indexes
        bodySheetIndex = body != null ? bodySheets.IndexOf(body) : -1;
        outfitSheetIndex = outfit != null ? outfitSheets.IndexOf(outfit) : -1;
        eyesSheetIndex = eyes != null ? eyesSheets.IndexOf(eyes) : -1;
        hairstyleSheetIndex = hairstyle != null ? hairstyleSheets.IndexOf(hairstyle) : -1;
        accessorySheetIndex = accessory != null ? accessorySheets.IndexOf(accessory) : -1;

        // Reset any other state as necessary
        originalactorData = null; // Clear snapshot after discarding changes
        Repaint(); // Force repaint to update GUI state
    }
}

    private bool CheckForChanges()
    {
        if (selectedActor == null) return false;

        bool hasChanges = false;

        hasChanges |= !actorName.Equals(selectedActor.name);
        hasChanges |= !Equals(body, selectedActor.body);
        hasChanges |= !Equals(outfit, selectedActor.outfit);
        hasChanges |= !Equals(eyes, selectedActor.eyes);
        hasChanges |= !Equals(hairstyle, selectedActor.hairstyle);
        hasChanges |= !Equals(accessory, selectedActor.accessory);

        return hasChanges;
    }

   private void DrawPartSelectors()
    {   
        GUILayout.Space(20);     
        GUILayout.BeginVertical();
        GUILayout.Space(40);

        // Part selectors
        body = DrawPartSelector("Body", bodySheets, bodySheetIndex, (index) => bodySheetIndex = index);
        outfit = DrawPartSelector("Outfit", outfitSheets, outfitSheetIndex, (index) => outfitSheetIndex = index);
        eyes = DrawPartSelector("Eyes", eyesSheets, eyesSheetIndex, (index) => eyesSheetIndex = index);
        hairstyle = DrawPartSelector("Hairstyle", hairstyleSheets, hairstyleSheetIndex, (index) => hairstyleSheetIndex = index);
        accessory = DrawPartSelector("Accessory", accessorySheets, accessorySheetIndex, (index) => accessorySheetIndex = index);

        DrawSaveDiscardButtons();
        GUILayout.EndVertical();
    }

    private void DrawNavigationButtons()
    {
        GUILayout.BeginHorizontal();

        // Left button
        if (GUILayout.Button("<"))
        {
            globalIndex = (globalIndex - 1 + maxIndex + 1) % (maxIndex + 1);
            Repaint();
        }

        GUILayout.FlexibleSpace();

        // Direction label
        CardinalDirection direction = (CardinalDirection)globalIndex;
        GUILayout.Label($"Direction: {direction} ({globalIndex + 1}/{maxIndex + 1})", GUILayout.Width(100));

        GUILayout.FlexibleSpace();

        // Right button
        if (GUILayout.Button(">"))
        {
            globalIndex = (globalIndex + 1) % (maxIndex + 1);
            Repaint();
        }

        GUILayout.EndHorizontal();
    }
    

private SpritePart_SO DrawPartSelector(
    string label, 
    List<SpritePart_SO> sheets, 
    int currentIndex, 
    Action<int> onIndexChanged)
    {
        GUILayout.BeginHorizontal();

        // Previous Sheet button
        if (GUILayout.Button("<", GUILayout.Width(20), GUILayout.Height(20)))
        {
            if (sheets.Count > 0)
            {
                int newIndex = (currentIndex - 1 + sheets.Count) % sheets.Count;
                onIndexChanged(newIndex);
                Repaint();
            }
        }

        GUILayout.Space(5); // Adjust space as needed

        // Dropdown Button
        if (GUILayout.Button(sheets.ElementAtOrDefault(currentIndex)?.name ?? "Select Part", EditorStyles.popup, GUILayout.Width(150), GUILayout.Height(20)))
        {
            GenericMenu menu = new GenericMenu();

            // Add menu items
            for (int i = 0; i < sheets.Count; i++)
            {
                var sheet = sheets[i];
                string menuItem = sheet.name;

                // Add item to the menu with an updated index
                int index = i; // Capture the current index
                menu.AddItem(new GUIContent(menuItem), i == currentIndex, () =>
                {
                    // Update the sheet index without using ref in the lambda
                    onIndexChanged(index);
                    Repaint();
                });
            }

            menu.ShowAsContext();
        }
        // Next Sheet button
        if (GUILayout.Button(">", GUILayout.Width(20), GUILayout.Height(20)))
        {
            if (sheets.Count > 0)
            {
                int newIndex = (currentIndex + 1) % sheets.Count;
                onIndexChanged(newIndex);
                Repaint();
            }
        }
        // Display the previewed sprite with tooltip
        if (sheets.Count > 0 && currentIndex >= 0 && currentIndex < sheets.Count && sheets[currentIndex] != null)
        {
            Rect previewRect = GUILayoutUtility.GetRect(48, 48, GUILayout.ExpandWidth(false));
            GUIContent content = new GUIContent
            {
                tooltip = sheets[currentIndex].name // Set the tooltip to the sprite sheet name
            };

            if (Event.current.type == EventType.Repaint)
            {
                GUI.Button(previewRect, content, GUIStyle.none);
                DrawSprite(sheets[currentIndex], 0, previewRect);
            }

            // Clear button
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
            {
                onIndexChanged(-1);
                Repaint();
            }
        }

        GUILayout.Space(5); 
        GUILayout.EndHorizontal();

        // Return the selected sheet or null if no valid index
        return currentIndex >= 0 && currentIndex < sheets.Count ? sheets[currentIndex] : null;
    }

    private void DrawActorPreview(Rect rect)
    {
        if (body != null) DrawSprite(body, globalIndex, rect,globalIndex);
        if (outfit != null) DrawSprite(outfit, globalIndex, rect,globalIndex);
        if (eyes != null) DrawSprite(eyes, globalIndex, rect,globalIndex);
        if (hairstyle != null) DrawSprite(hairstyle, globalIndex, rect,globalIndex);
        if (accessory != null) DrawSprite(accessory, globalIndex, rect,globalIndex);
    }

    private void DrawSprite(SpritePart_SO part, int spriteIndex, Rect rect, int localIndex = 3)
    {
        if (part != null && part.sprites.Length > 0)
        {
            spriteIndex = Mathf.Clamp(spriteIndex, localIndex, part.sprites.Length - 1); //THE 3 IS CORRECT, THIS IS THE FACING SOUTH FRAME, WE WANT THIS

            Sprite sprite = part.sprites[spriteIndex];
            if (sprite != null)
            {
                Texture2D texture = sprite.texture;
                Rect spriteRect = sprite.rect;

                Rect normalizedRect = new Rect(
                    spriteRect.x / texture.width,
                    spriteRect.y / texture.height,
                    spriteRect.width / texture.width,
                    spriteRect.height / texture.height
                );

                float aspect = spriteRect.width / spriteRect.height;
                Rect displayRect = rect;

                if (aspect > 1)
                {
                    displayRect.height = rect.width / aspect;
                    displayRect.y += (rect.height - displayRect.height) / 2;
                }
                else
                {
                    displayRect.width = rect.height * aspect;
                    displayRect.x += (rect.width - displayRect.width) / 2;
                }

                GUI.DrawTextureWithTexCoords(displayRect, texture, normalizedRect);
            }
        }
    }

    private void SaveActor()
    {
        // Clear focus
        EditorGUI.FocusTextInControl(null);
        if (selectedActor != null)
        {
            // Update existing NPC_SO
            selectedActor.name = actorName;
            selectedActor.body = body;
            selectedActor.outfit = outfit;
            selectedActor.eyes = eyes;
            selectedActor.hairstyle = hairstyle;
            selectedActor.accessory = accessory;

            OnActorUpdated?.Invoke(selectedActor);
            
            // Save the updated NPC_SO
            EditorUtility.SetDirty(selectedActor);
            AssetDatabase.SaveAssets();        
            
            
        }
        else
        {
            // Create new NPC_SO
            string path = $"{actorsFolderPath}/{actorName}.asset";

            if (File.Exists(path))
            {
                Debug.LogError($"An NPC with the name '{actorName}' already exists.");
                return;
            }

            Actor_SO newNPC = CreateInstance<Actor_SO>();
            newNPC.body = body;
            newNPC.outfit = outfit;
            newNPC.eyes = eyes;
            newNPC.hairstyle = hairstyle;
            newNPC.accessory = accessory;

            AssetDatabase.CreateAsset(newNPC, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newNPC;

            Debug.Log($"NPC {actorName} created at {path}");
        }
    }

    private void RenameSelectedActorAsset(string newName)
    {
        if (selectedActor != null)
        {
            // Get the current asset path
            string oldAssetPath = AssetDatabase.GetAssetPath(selectedActor);
            string newAssetName = actorName; // Name you want to use for the asset
            string newAssetPath = Path.Combine(Path.GetDirectoryName(oldAssetPath), $"{newAssetName}.asset");

            // Check if the asset needs to be renamed
            if (oldAssetPath != newAssetPath)
            {
                // Rename the NPC asset
                string error = AssetDatabase.RenameAsset(oldAssetPath, newAssetName);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"Failed to rename NPC asset: {error}");
                }
                else
                {
                    selectedActor.name = newAssetName; // Ensure the object's name matches the new asset name
                }
            }

            // Always call this to ensure changes are saved
            AssetDatabase.SaveAssets();

            // Check for any mismatch after renaming
            string finalAssetPath = AssetDatabase.GetAssetPath(selectedActor);
        }
    }
}
}
#endif