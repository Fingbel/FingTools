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
    public static ActorEditorWindow Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GetWindow<ActorEditorWindow>();
            }
            return _instance;
        }
    }
    public static ActorEditorWindow _instance;
    public static event Action OnActorAvailableUpdated;
    public static event Action<Actor_SO> OnActorUpdated;
    private string actorName;
    private ActorSpritePart_SO body;
    private ActorSpritePart_SO outfit;
    private ActorSpritePart_SO eyes;
    private ActorSpritePart_SO hairstyle;
    private ActorSpritePart_SO accessory;
    private string tempActorName;
    
    private Actor_SO selectedActor;

    // Snapshot to track changes
    private Actor_SO originalactorData;

    // CharacterPartsManager instance
    private SpriteManager spriteManager;

    // Lists and indexes for sprite sheets
    private List<ActorSpritePart_SO> bodySheets = new List<ActorSpritePart_SO>();
    private List<ActorSpritePart_SO> outfitSheets = new List<ActorSpritePart_SO>();
    private List<ActorSpritePart_SO> eyesSheets = new List<ActorSpritePart_SO>();
    private List<ActorSpritePart_SO> hairstyleSheets = new List<ActorSpritePart_SO>();
    private List<ActorSpritePart_SO> accessorySheets = new List<ActorSpritePart_SO>();
    private int bodySheetIndex = 0;
    private int outfitSheetIndex = 0;
    private int eyesSheetIndex = 0;
    private int hairstyleSheetIndex = 0;
    private int accessorySheetIndex = 0;
    private Vector2 actorListScrollPosition = Vector2.zero;
    private Vector2 globalScrollPosition = Vector2.zero;

    [MenuItem("FingTools/Actor Editor", false, 1)]
    public static void ShowWindow()
    {
        ActorEditorWindow window = GetWindow<ActorEditorWindow>(true);
        window.titleContent = new GUIContent("Actor Editor");
        window.Show();
    }

    public static void ShowWindow(string _actorName,NPCSpawner npcSpawner )
    {
        ActorEditorWindow window = GetWindow<ActorEditorWindow>(true);
        window.titleContent = new GUIContent(_actorName);
        CreateNewActor(_actorName,npcSpawner,true);        
        window.Show();
    }

    public static void SetActorToPreview(Actor_SO actor)
    {
        if (actor != null)
        {
            ActorEditorWindow window = GetWindow<ActorEditorWindow>(true);
            window.selectedActor = actor;
            window.LoadActorData(actor);
        }
    }

    [MenuItem("FingTools/Actor Editor",true)]
    public static bool ValidateActorEditWindow()
    {
        if(!Directory.Exists("Assets/Resources/FingTools"))
            return false;
        else
            return false;
    }

    private void OnEnable()
    {
        EditorUtility.DisplayProgressBar("Loading ","Loading character assets",0.5f);
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
        EditorApplication.update += RefreshPortraitPreview;
        EditorApplication.update += RefreshActorPreview;
        EditorUtility.ClearProgressBar();
    }

    private void OnDisable() {
        EditorApplication.update -= RefreshPortraitPreview;
        EditorApplication.update -= RefreshActorPreview;
    }

    private void RefreshActorPreview()
    {
        if(actorAnimation == "Fixed") {actorAnimationDelta = 22;Repaint();return;}
        switch(actorAnimation)
        {                
            case "Idle":actorAnimationDelta = 22; break;
            case "Walking":actorAnimationDelta = 46; break;
        }
        actorAnimationTick += Time.deltaTime;
        if(actorAnimationTick >= 4f)
        {
            
            currentActorFrame++;
            if(currentActorFrame >= 6)
                currentActorFrame = 0;
            actorAnimationTick = 0;
            Repaint();
        }
    }
    private void RefreshPortraitPreview()
    {   
        if(portraitAnimation == "Fixed") {portraitAnimationDelta = 0;Repaint();return;}
        switch(portraitAnimation)
        {
            case "Talk":portraitAnimationDelta = 0; break;
            case "Nod":portraitAnimationDelta = 10; break;
            case "Shake":portraitAnimationDelta = 20; break;
        }
        portraitAnimationTick += Time.deltaTime;
        if(portraitAnimationTick >= 3f)
        {
            
            currentPortraitFrame++;
            if(currentPortraitFrame >= portraitAnimationDelta+10)
                currentPortraitFrame = portraitAnimationDelta;            
            portraitAnimationTick = 0;
            Repaint();
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
    }

    private void OnGUI()
    {
        GUILayout.BeginScrollView(globalScrollPosition);
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
                GUILayout.BeginVertical();
                DrawActorInfoAndPreview();
                DrawPortrait();
                GUILayout.EndVertical();                
                DrawPartSelectors();
            }        
        GUILayout.EndHorizontal();

        // Handle Enter key press for the name input field
        HandleEnterKeyPress();
        GUILayout.EndScrollView();
    }

    private void HandleEnterKeyPress()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return)
        {
            if (GUI.GetNameOfFocusedControl() == "TempNameField")
            {
                UpdateActorName();
                GUI.FocusControl(null);
                e.Use();
            }
        }
    }

    private bool UpdateActorName()
    {
        var actors = Resources.LoadAll<Actor_SO>("FingTools/Actors");
        foreach (var actorName in actors)
        {
            if(tempActorName == actorName.name)
            {
                EditorUtility.DisplayDialog("Error", "An Actor with this name already exists.", "OK");          
                tempActorName = string.Empty;  
                GUI.FocusControl(null);
                return false;
            }
        }
        if (selectedActor != null)
        {        
            actorName = tempActorName;        
            RenameSelectedActorAsset(actorName);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(selectedActor);
            
        }
        return true;
    }

    private void LoadActorData(Actor_SO actor)
    {
        actorName = actor.name;    
        body = actor.body;
        outfit = actor.outfit;
        eyes = actor.eyes;
        hairstyle = actor.hairstyle;
        accessory = actor.accessory;

        if(actor.portrait_SO == null) 
        {
            PortraitImporter.BuildPortraitFromActorSO(ref actor);
        }

        bodyPortrait = actor.portrait_SO.body;
        hairPortrait = actor.portrait_SO.hairstyle;
        eyesPortrait = actor.portrait_SO.eyes;
        accessoryPortrait = actor.portrait_SO.accessory;

        // Update sheet indices, default to -1 if part is null
        bodySheetIndex = body != null ? bodySheets.IndexOf(body) : -1;
        outfitSheetIndex = outfit != null ? outfitSheets.IndexOf(outfit) : -1;
        eyesSheetIndex = eyes != null ? eyesSheets.IndexOf(eyes) : -1;
        hairstyleSheetIndex = hairstyle != null ? hairstyleSheets.IndexOf(hairstyle) : -1;
        accessorySheetIndex = accessory != null ? accessorySheets.IndexOf(accessory) : -1;

        // Create a snapshot of the current data
        originalactorData = CreateSnapshot(actor);

        //Reset animation to fixed
        actorAnimation = "Fixed";
        currentActorFrame = 0;
        portraitAnimation = "Fixed";
        currentPortraitFrame = 0;
        renaming = false;
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

        bodyPortrait = null;
        eyesPortrait = null;
        hairPortrait = null;
        accessoryPortrait = null;

        bodySheetIndex = -1;
        outfitSheetIndex = -1;
        eyesSheetIndex = -1;
        hairstyleSheetIndex = -1;
        accessorySheetIndex = -1;

        // Reset maxIndex as needed
        Repaint();
    }

public static void CreateNewActor(string _actorName, NPCSpawner npcSpawner = null,bool openWindow = true)
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
    if(openWindow)
    {
        Instance.selectedActor = newNPC;
        Instance.selectedActor.name = actorNameToUse;    
        Instance.tempActorName = string.Empty; // Clear the input field
        Instance.renaming = false;
        GUI.FocusControl(null);
        Instance.ClearActorData(_actorName);
        SetActorToPreview(newNPC);
    }   
    // If an NPCSpawner was passed, assign the new Actor_SO to its npcTemplate
    if (npcSpawner != null)
    {
        npcSpawner.npcActor = newNPC; // Assign the new Actor_SO to the NPCSpawner
        EditorUtility.SetDirty(npcSpawner); // Mark the NPCSpawner as dirty to save changes
        NPCManager.RefreshNPCSpawners(); // Refresh the NPCSpawners in the scene
    }
       
    // Refresh the AssetDatabase
    AssetDatabase.Refresh();
    OnActorAvailableUpdated?.Invoke();
}

private static string GetUniqueActorName(string baseName)
{
    int index = 1;
    string uniqueName = baseName;
    
    if(!Directory.Exists(CharacterImporter.actorsFolderPath))
    {
        Directory.CreateDirectory(CharacterImporter.actorsFolderPath);
    }
    // Check if a name with the baseName or a suffixed version already exists
    while (AssetDatabase.FindAssets($"t:Actor_SO", new[] { CharacterImporter.actorsFolderPath })
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
        actorName = selectedActor.name;
        body = selectedActor.body;
        outfit = selectedActor.outfit;
        eyes = selectedActor.eyes;
        hairstyle = selectedActor.hairstyle;
        accessory = selectedActor.accessory;

        bodyPortrait = PortraitImporter.ResolvePortraitPart(PortraitPartType.Skin,body?.name);
        eyesPortrait = PortraitImporter.ResolvePortraitPart(PortraitPartType.Eyes,eyes?.name);
        hairPortrait = PortraitImporter.ResolvePortraitPart(PortraitPartType.Hairstyle,hairstyle?.name);
        accessoryPortrait = PortraitImporter.ResolvePortraitPart(PortraitPartType.Accessory,accessory?.name);

        // Update sheet indexes
        bodySheetIndex = body != null ? bodySheets.IndexOf(body) : -1;
        outfitSheetIndex = outfit != null ? outfitSheets.IndexOf(outfit) : -1;
        eyesSheetIndex = eyes != null ? eyesSheets.IndexOf(eyes) : -1;
        hairstyleSheetIndex = hairstyle != null ? hairstyleSheets.IndexOf(hairstyle) : -1;
        accessorySheetIndex = accessory != null ? accessorySheets.IndexOf(accessory) : -1;

        // Reset any other state as necessary
        originalactorData = null;
        AssetDatabase.SaveAssets();   
        LoadActorData(selectedActor);
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
            EditorUtility.SetDirty(selectedActor.portrait_SO);
            AssetDatabase.SaveAssets();      
            UpdateSpawnedActors();         
            
        }
        else
        {
            // Create new NPC_SO
            string path = $"{CharacterImporter.actorsFolderPath}/{actorName}.asset";

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
            EditorUtility.SetDirty(newNPC.portrait_SO);
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