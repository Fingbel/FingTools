using FingTools.Internal;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;

#if UNITY_EDITOR
namespace FingTools.Internal
{
public partial class ActorEditorWindow : EditorWindow
{
    private PortraitPart_SO bodyPortrait;
    private PortraitPart_SO hairPortrait;
    private PortraitPart_SO eyesPortrait;
    private PortraitPart_SO accessoryPortrait ;
    bool renaming = false;
    private float animationTick;
    private int currentPortraitFrame = 0;
    private int portraitDelta =0;
    private string portraitAnimation = "Shake";
    private void DrawPortrait()
    {                
        GUILayout.Label("Portrait Preview  : ") ;
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("Fixed",GUILayout.Width(50)))
        {
            portraitAnimation = "Fixed";
            currentPortraitFrame=0;
        }
        if(GUILayout.Button("Talk",GUILayout.Width(50)))
        {
            portraitAnimation = "Talk";
            currentPortraitFrame=0;
        }
        if(GUILayout.Button("Nod",GUILayout.Width(50)))
        {
            portraitAnimation = "Nod";
            currentPortraitFrame=10;
        }
        if(GUILayout.Button("Shake",GUILayout.Width(50)))
        {
            portraitAnimation = "Shake";
            currentPortraitFrame=20;
        }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(100);  
        GUILayout.BeginVertical(GUILayout.Width(200));
        Rect previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
        GUILayout.Space(40);  

        DrawSprite(bodyPortrait,currentPortraitFrame,previewRect,1);        
        DrawSprite(hairPortrait,currentPortraitFrame,previewRect,1);
        DrawSprite(eyesPortrait,currentPortraitFrame,previewRect,1);
        DrawSprite(accessoryPortrait,currentPortraitFrame,previewRect,1);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }
    private void DrawPartSelectors()
    {   
        GUILayout.BeginArea(new Rect(550,0,280,500));
        GUILayout.BeginVertical();
        GUILayout.Space(40);

        // Part selectors
        body = DrawPartSelector(ActorPartType.Bodies, bodySheets, bodySheetIndex, (index) =>bodySheetIndex = index);
        //DrawSeparator();
        outfit = DrawPartSelector(ActorPartType.Outfits, outfitSheets, outfitSheetIndex, (index) => outfitSheetIndex = index);
        //DrawSeparator();
        eyes = DrawPartSelector(ActorPartType.Eyes, eyesSheets, eyesSheetIndex, (index) => eyesSheetIndex = index);
        //DrawSeparator();
        hairstyle = DrawPartSelector(ActorPartType.Hairstyles, hairstyleSheets, hairstyleSheetIndex, (index) => hairstyleSheetIndex = index);
        //DrawSeparator();
        accessory = DrawPartSelector(ActorPartType.Accessories, accessorySheets, accessorySheetIndex, (index) =>accessorySheetIndex = index);

        DrawSaveDiscardButtons();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }    
    private static void DrawSprite(SpritePart_SO part, int spriteIndex, Rect rect, int localIndex = 3)
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

    private void DrawActorPreview(Rect rect)
    {
        if (body != null) DrawSprite(body, globalIndex, rect,globalIndex);
        if (outfit != null) DrawSprite(outfit, globalIndex, rect,globalIndex);
        if (eyes != null) DrawSprite(eyes, globalIndex, rect,globalIndex);
        if (hairstyle != null) DrawSprite(hairstyle, globalIndex, rect,globalIndex);
        if (accessory != null) DrawSprite(accessory, globalIndex, rect,globalIndex);
    }   
    private void DrawActorInfoAndPreview() 
    {
        if(!Directory.Exists(CharacterImporter.actorsFolderPath))
        {
            Directory.CreateDirectory(CharacterImporter.actorsFolderPath);
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
        GUILayout.Label("Current Name: " + selectedActor?.name, EditorStyles.label, GUILayout.Width(200));        
        if(!renaming)
        {
            if(GUILayout.Button("Rename"))
            {
                renaming = !renaming;
            }
        }
        else
        {            
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            // Input field for new name    
            tempActorName = EditorGUILayout.TextField(selectedActor.name);
            if (GUILayout.Button("✔", GUILayout.Width(30)))
            {
                actorName = tempActorName;            
                
                // Call the method to rename the asset
                renaming = !UpdateActorName();
                tempActorName = string.Empty;
                GUI.FocusControl(null);
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(selectedActor);        
            }
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                renaming = false;
                tempActorName = "";
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        // Actor Preview
        GUILayout.Label("Actor Preview", EditorStyles.boldLabel);
        GUILayout.EndHorizontal();

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

    private ActorSpritePart_SO DrawPartSelector(
    ActorPartType actorPartType,
    List<ActorSpritePart_SO> sheets, 
    int currentIndex, 
    Action<int> onIndexChanged)
        {
            GUILayout.BeginHorizontal();

            // Previous Sheet button
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("<", GUILayout.Width(28), GUILayout.Height(28)))
            {
                if (sheets.Count > 0)
                {
                    int newIndex = (currentIndex - 1 + sheets.Count) % sheets.Count;
                    ProcessPortraitPreview(actorPartType, onIndexChanged, newIndex);
                }
            }
            GUILayout.FlexibleSpace();
            // Display the previewed sprite
            if (sheets.Count > 0 && currentIndex >= 0 && currentIndex < sheets.Count && sheets[currentIndex] != null)
            {
                Rect rect = GUILayoutUtility.GetRect(32, 64, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
                rect.y -= 16;
                if (Event.current.type == EventType.Repaint)
                {
                    DrawSprite(sheets[currentIndex], 0, rect);
                }
            }
            else
            {
                Rect rect = GUILayoutUtility.GetRect(32, 64, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            }
            // Next Sheet button
            if (GUILayout.Button(">", GUILayout.Width(28), GUILayout.Height(28)))
            {
                if (sheets.Count > 0)
                {
                    int newIndex = (currentIndex + 1) % sheets.Count;
                    ProcessPortraitPreview(actorPartType, onIndexChanged, newIndex);
                }
            }
            GUILayout.Space(5); // Adjust space as needed
                                // Clear button
            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
            {
                ProcessPortraitPreview(actorPartType, onIndexChanged, -1);
                Repaint();
            }

            // Dropdown Button
            //We need to change the basic dropdown  for a SearchWindow
            DrawSearchTree(actorPartType, sheets, currentIndex, onIndexChanged);
            GUILayout.EndHorizontal();

            // Return the selected sheet or null if no valid index
            return currentIndex >= 0 && currentIndex < sheets.Count ? sheets[currentIndex] : null;
        }
        private void DrawSearchTree(ActorPartType actorPartType, List<ActorSpritePart_SO> sheets, int currentIndex, Action<int> onIndexChanged)
        {
           if (GUILayout.Button(sheets.ElementAtOrDefault(currentIndex)?.name ?? "Select Part", EditorStyles.popup, GUILayout.Width(150)))
            {
                // Create and display the SearchWindow
                var searchWindow = ScriptableObject.CreateInstance<PartSearchWindow>();
                searchWindow.Initialize(sheets, selectedPart =>
                {
                    int newIndex = sheets.IndexOf(selectedPart);
                    ProcessPortraitPreview(actorPartType, onIndexChanged, newIndex);
                });

                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)), searchWindow);
            }  
        }        

        private void ProcessPortraitPreview(ActorPartType type, Action<int> onIndexChanged, int index)
        {
            onIndexChanged(index);
            
            switch(type)
            {
                case ActorPartType.Bodies:
                if(index != -1)
                    bodyPortrait = PortraitImporter.ResolvePortraitPart(PortraitPartType.Skin, bodySheets[index].name);
                else
                    bodyPortrait = null;
                break;
                
                case ActorPartType.Eyes:
                if(index != -1)
                    eyesPortrait = PortraitImporter.ResolvePortraitPart(PortraitPartType.Eyes, eyesSheets[index].name);
                else
                    eyesPortrait = null;
                break;

                case ActorPartType.Hairstyles:
                if(index != -1)
                    hairPortrait = PortraitImporter.ResolvePortraitPart(PortraitPartType.Hairstyle, hairstyleSheets[index].name);
                else
                    hairPortrait = null;
                break;

                case ActorPartType.Accessories:
                if(index != -1)
                    accessoryPortrait = PortraitImporter.ResolvePortraitPart(PortraitPartType.Accessory, accessorySheets[index].name);
                else
                    accessoryPortrait = null;
                break;
            }
            Repaint();
        }

        private void DrawActorList()
        {
            GUILayout.BeginVertical(GUILayout.Width(200));            
            GUILayout.Label("Select Actor", EditorStyles.boldLabel);

            string[] actorGUIDs = AssetDatabase.FindAssets("t:Actor_SO", new[] { CharacterImporter.actorsFolderPath });
            List<Actor_SO> actorAssets = actorGUIDs
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => AssetDatabase.LoadAssetAtPath<Actor_SO>(path))
                .Where(actor => actor != null)
                .ToList();
            actorListScrollPosition = GUILayout.BeginScrollView(actorListScrollPosition);
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
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            // Restore original GUI.enabled state
            GUI.enabled = originalGUIState;
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
}
public class PartSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private List<ActorSpritePart_SO> parts;
    private Action<ActorSpritePart_SO> onPartSelected;

    // Initialize the SearchWindow
    public void Initialize(List<ActorSpritePart_SO> parts, Action<ActorSpritePart_SO> onPartSelected)
    {
        this.parts = parts;
        this.onPartSelected = onPartSelected;
    }

    // Populate the SearchWindow with entries
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        List<SearchTreeEntry> entries = new List<SearchTreeEntry>
        {
            new SearchTreeGroupEntry(new GUIContent("Select Part"), 0) // Top-level group
        };
        // Categories to exclude from grouping 
        HashSet<string> excludedCategories = new HashSet<string> { "Eyes","Body" };
        // Dictionary to group entries by their base name
        Dictionary<string, List<ActorSpritePart_SO>> groupedParts = new Dictionary<string, List<ActorSpritePart_SO>>();

        foreach (var part in parts)
        {
            // Extract base name (e.g., "Accessory_01_Ladybug" from "Accessory_01_Ladybug_01")
            string baseName = GetBaseName(part.name);

            // Check if this category is excluded
            if (excludedCategories.Contains(baseName))
            {
                // Add directly as a single entry
                entries.Add(new SearchTreeEntry(new GUIContent(part.name))
                {
                    level = 1, // Directly under the top group
                    userData = part
                });
            }
            else
            {
                // Group other entries
                if (!groupedParts.ContainsKey(baseName))
                {
                    groupedParts[baseName] = new List<ActorSpritePart_SO>();
                }
                groupedParts[baseName].Add(part);
            }
        }

        // Add grouped entries to the search tree
        foreach (var group in groupedParts)
        {
            // Add the group as a new tree entry
            entries.Add(new SearchTreeGroupEntry(new GUIContent(group.Key), 1)); // Group level

            // Add individual items within the group
            foreach (var part in group.Value)
            {
                entries.Add(new SearchTreeEntry(new GUIContent(part.name))
                {
                    level = 2, // Indented under the group
                    userData = part // Store part data
                });
            }
        }

        return entries;
    }

    // Helper method to extract the base name
    private string GetBaseName(string partName)
    {
        // Split by underscore and remove the last numeric segment
        string[] segments = partName.Split('_');
        if (segments.Length > 1 && int.TryParse(segments.Last(), out _))
        {
            return string.Join("_", segments.Take(segments.Length - 1));
        }
        return partName; // Return full name if no numeric suffix
    }

    // Handle selection
    public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
    {
        if (entry.userData is ActorSpritePart_SO selectedPart)
        {
            onPartSelected?.Invoke(selectedPart); // Notify of selection
        }
        return true;
    }
}
}
#endif