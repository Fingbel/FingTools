using UnityEngine;
using UnityEditor;

namespace FingTools.Internal
{
    public class NPCSpawner : MonoBehaviour
    {       
        public Actor_SO npcActor;
        private readonly int defaultSpriteIndex = 3;                        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // If the npcActor is set, draw the NPC preview
            if (npcActor != null)
            {
                Color outlineColor = Color.white;
                DrawNPCPreview(outlineColor);
            }
            else
            {
                // Calculate the size of the 16x32 box in world units based on the grid size
                float worldWidth = 1f; // Two grid cells horizontally (2 * 1 unit)
                float worldHeight = 2f; // Two grid cells vertically (2 * 1 unit)
                
                // Use the world units directly for the Gizmo size (no need for pixel-based scaling here)
                Gizmos.color = Color.red; 
                Vector3 position = transform.position + new Vector3(0.5f,0,0); 
                Vector3 size = new Vector3(worldWidth, worldHeight, 0); 
                Gizmos.DrawWireCube(position, size);
            }
        }
        public void SetActorSO(string actorSOName)
        {
            npcActor = Resources.Load<Actor_SO>($"FingTools/Actors/{gameObject.name}");
        }

        private void DrawNPCPreview(Color outlineColor)
        {
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;
            // Cache the position of the spawner in world space
            Vector3 position = transform.position;

            // Define the order in which the sprites are drawn (bodies, outfits, etc.)
            var parts = new ActorSpritePart_SO[]
            {
                npcActor.body,
                npcActor.outfit,
                npcActor.eyes,
                npcActor.hairstyle,
                npcActor.accessory
            };
           
            // Define a base size for the Gizmo in world units
            float baseWidth = 1f; // Width in world units
            float baseHeight = baseWidth * 2f; // Height is twice the width for a 2:1 ratio

            // Convert base size from world units to screen pixels
            float pixelsPerUnit = Screen.height / (2f * sceneCamera.orthographicSize);
            float gizmoWidth = baseWidth * pixelsPerUnit;
            float gizmoHeight = baseHeight * pixelsPerUnit;

            // Start drawing GUI elements
            Handles.BeginGUI();

            // Convert the world position to GUI position
            Vector2 guiPosition = HandleUtility.WorldToGUIPoint(position);

            // Define the Gizmo rect
            Rect rect = new Rect(
                guiPosition.x +(0.47f*pixelsPerUnit)- gizmoWidth / 2 ,
                guiPosition.y +(0.85f*pixelsPerUnit)- gizmoHeight / 2 , 
                gizmoWidth,
                gizmoHeight
            );

            // Draw the outline
            DrawOutline(rect, 0.1f, outlineColor); // Adjust the outline thickness as needed

            // Draw each part
            foreach (var part in parts)
            {
                if (part != null && part.sprites != null && part.sprites.Length > 0)
                {
                    // Use the first sprite as the preview
                    Sprite sprite = part.sprites[defaultSpriteIndex];
                    if (sprite != null)
                    {
                        // Calculate the UVs for the sprite portion of the texture
                        Rect spriteRect = sprite.rect;
                        Rect uv = new Rect(
                            spriteRect.x / sprite.texture.width,
                            spriteRect.y / sprite.texture.height,
                            spriteRect.width / sprite.texture.width,
                            spriteRect.height / sprite.texture.height
                        );

                        // Draw the texture with correct UV mapping
                        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv);
                    }
                }
            }

            // End drawing GUI elements
            Handles.EndGUI();
            
        }

        private void DrawOutline(Rect rect, float thickness, Color color)
        {
            // Set the GUI color
            GUI.color = color;

            // Draw outline with the given thickness
            GUI.DrawTexture(new Rect(rect.x - thickness, rect.y - thickness, rect.width + 2 * thickness, thickness), Texture2D.whiteTexture); // Top
            GUI.DrawTexture(new Rect(rect.x - thickness, rect.y + rect.height, rect.width + 2 * thickness, thickness), Texture2D.whiteTexture); // Bottom
            GUI.DrawTexture(new Rect(rect.x - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture); // Left
            GUI.DrawTexture(new Rect(rect.x + rect.width, rect.y, thickness, rect.height), Texture2D.whiteTexture); // Right

            // Reset the GUI color
            GUI.color = Color.white;
        }
        #endif
    }
}