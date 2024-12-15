using UnityEngine;
using UnityEditor;

namespace FingTools.Internal
{
    public class NPCSpawner : MonoBehaviour
    {
        public Actor_SO npcTemplate;
        private readonly int defaultSpriteIndex = 3;
        
        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (npcTemplate != null)
            {
                // Set outline color based on NPC type
                //Color outlineColor = GetOutlineColorForNPC(npcTemplate.nPCType);
                Color outlineColor = Color.white;

                DrawNPCPreview(outlineColor);
            }
        }

        public void SetActorSO(string actorSOName)
        {
            npcTemplate = Resources.Load<Actor_SO>($"FingTools/Actors/{gameObject.name}");
        }

        private void DrawNPCPreview(Color outlineColor)
        {
            // Cache the position of the spawner in world space
            Vector3 position = transform.position;

            // Define the order in which the sprites are drawn (bodies, outfits, etc.)
            var parts = new SpritePart_SO[]
            {
                npcTemplate.body,
                npcTemplate.outfit,
                npcTemplate.eyes,
                npcTemplate.hairstyle,
                npcTemplate.accessory
            };

            // Get the current Scene view camera
            Camera sceneCamera = SceneView.lastActiveSceneView.camera;

            if (sceneCamera != null)
            {
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