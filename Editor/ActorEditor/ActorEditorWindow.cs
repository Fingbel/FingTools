using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

#if UNITY_EDITOR
namespace FingTools.Internal
{
public partial class ActorEditorWindow : EditorWindow
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

    [MenuItem("FingTools/Actor Editor", false, 1)]
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
            var manager = Resources.Load<SpriteManager>("FingTools/SpriteManager");
            if(manager?.HasAssetsImported() == true)
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
    Actor_SO newNPC = CreateInstance<Actor_SO>();
    newNPC.name = actorNameToUse;
    PortraitImporter.BuildPortraitFromActorSO(ref newNPC);

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
            PortraitImporter.BuildPortraitFromActorSO(ref selectedActor);
            OnActorUpdated?.Invoke(selectedActor);
            // Save the updated NPC_SO
            EditorUtility.SetDirty(selectedActor);
            AssetDatabase.SaveAssets();      
            UpdateSpawnedActors();                         
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
            PortraitImporter.BuildPortraitFromActorSO(ref newNPC);

            AssetDatabase.CreateAsset(newNPC, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newNPC;

            Debug.Log($"NPC {actorName} created at {path}");
        }
    }

        private void UpdateSpawnedActors()
        {
            //We need to gather all the ActorAPI of the scene
            var actorAPIs = FindObjectsByType<ActorAPI>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var actorApi in actorAPIs)
            {
                actorApi.GetComponent<ActorModelController>().UpdatePreviewSprites();
            }
        }

        private void RenameSelectedActorAsset(string newName)
        {
            if (selectedActor != null)
            {
                // Get the current asset path
                string oldAssetPath = AssetDatabase.GetAssetPath(selectedActor);
                string newAssetName = newName; // Name you want to use for the asset
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
                        PortraitImporter.RenamePortrait(newAssetName,selectedActor);
                    }
                }
                // Always call this to ensure changes are saved
                AssetDatabase.SaveAssets();
            }
        }
       
}
}
#endif