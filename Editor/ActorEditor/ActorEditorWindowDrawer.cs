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
    private void DrawPortrait()
    {
        GUILayout.Space(40);   
        GUILayout.BeginVertical(GUILayout.Width(300));
        GUILayout.Space(40);   
        GUILayout.Label("PORTRAIT GO HERE");
        GUILayout.Label("===============");
        GUILayout.Label("===============");
        GUILayout.Label("===============");
        GUILayout.Label("===============");
        GUILayout.Label("PORTRAIT GO HERE");
        GUILayout.EndVertical();
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
    private static void DrawSprite(ActorSpritePart_SO part, int spriteIndex, Rect rect, int localIndex = 3)
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
    string label, 
    List<ActorSpritePart_SO> sheets, 
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
}
#endif