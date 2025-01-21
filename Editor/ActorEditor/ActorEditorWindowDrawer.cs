using FingTools.Internal;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

#if UNITY_EDITOR
namespace FingTools.Internal
{
public partial class ActorEditorWindow : EditorWindow
{
    private PortraitPart_SO bodyPortrait;
    private PortraitPart_SO hairPortrait;
    private PortraitPart_SO eyesPortrait;
    private PortraitPart_SO accessoryPortrait ;
    private void DrawPortrait()
    {                
        GUILayout.Label("Portrait Preview  : ") ;
        GUILayout.BeginHorizontal();
        GUILayout.Space(100);  
        GUILayout.BeginVertical(GUILayout.Width(200));
        Rect previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.ExpandWidth(false));
        GUILayout.Space(40);  
        DrawSprite(bodyPortrait,globalIndex,previewRect,3);        
        DrawSprite(hairPortrait,globalIndex,previewRect,3);
        DrawSprite(eyesPortrait,globalIndex,previewRect,3);
        DrawSprite(accessoryPortrait,globalIndex,previewRect,3);
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
                ProcessPortraitPreview(actorPartType,onIndexChanged, newIndex);
            }
        }
        GUILayout.FlexibleSpace();
        // Display the previewed sprite
        if (sheets.Count > 0 && currentIndex >= 0 && currentIndex < sheets.Count && sheets[currentIndex] != null)
        {            
            Rect rect = GUILayoutUtility.GetRect(32, 64, GUILayout.ExpandWidth(false),GUILayout.ExpandHeight(false));
            rect.y -= 16;
            if (Event.current.type == EventType.Repaint)
            {
                DrawSprite(sheets[currentIndex], 0, rect);
            }
        }
        else
        {
            Rect rect = GUILayoutUtility.GetRect(32, 64, GUILayout.ExpandWidth(false),GUILayout.ExpandHeight(false));
        }
        // Next Sheet button
        if (GUILayout.Button(">", GUILayout.Width(28), GUILayout.Height(28)))
        {
            if (sheets.Count > 0)
            {
                int newIndex = (currentIndex + 1) % sheets.Count;
                ProcessPortraitPreview(actorPartType,onIndexChanged, newIndex);
            }
        }
        GUILayout.Space(5); // Adjust space as needed
        // Clear button
        if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20)))
        {
            ProcessPortraitPreview(actorPartType,onIndexChanged,-1);
            Repaint();
        }
        // Dropdown Button
        if (GUILayout.Button(sheets.ElementAtOrDefault(currentIndex)?.name ?? "Select Part", EditorStyles.popup, GUILayout.Width(150)))
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
                    ProcessPortraitPreview(actorPartType,onIndexChanged, index);
                });
            }

            menu.ShowAsContext();
        }
        
        
        GUILayout.EndHorizontal();

        // Return the selected sheet or null if no valid index
        return currentIndex >= 0 && currentIndex < sheets.Count ? sheets[currentIndex] : null;
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
     private void DrawSeparator(bool horizontal = true,int beforeSpace =0,int afterSpace=0)
        {
            GUILayout.Space(beforeSpace);
            if(horizontal)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            else
                EditorGUILayout.LabelField("", GUI.skin.verticalSlider);
            GUILayout.Space(afterSpace);
        }     
}
}
#endif