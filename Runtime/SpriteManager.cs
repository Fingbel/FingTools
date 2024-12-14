using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FingTools.Lime
{
[System.Serializable]
public class SpriteManager : ScriptableObject 
{
    public List<SpritePart_SO> accessoryParts = new();
    public List<SpritePart_SO> bodyParts = new();
    public List<SpritePart_SO> outfitParts = new();
    public List<SpritePart_SO> hairstyleParts = new();
    public List<SpritePart_SO> eyeParts = new();
            
    private int selectedSizeIndex;

    public int SelectedSizeIndex { get => selectedSizeIndex; set => selectedSizeIndex = value;}


    private Dictionary<CharSpriteType, int> spriteCounts = new Dictionary<CharSpriteType, int>(); //TODO : remove this dictionary and use the scriptable list to count

    // Method to set the count for a specific CharSpriteType
    public void SetSpriteCount(CharSpriteType type, int count)
    {
        spriteCounts[type] = count; // Update the count
    }

    public int GetSpriteCount(CharSpriteType type)
    {
        return type switch
        {
            CharSpriteType.Accessories => accessoryParts.Count,
            CharSpriteType.Bodies => bodyParts.Count,
            CharSpriteType.Outfits => outfitParts.Count,
            CharSpriteType.Eyes => eyeParts.Count,
            CharSpriteType.Hairstyles => hairstyleParts.Count,
            _ => 0,
        };
    }

    public bool HasAssetsImported()
    {
        return accessoryParts?.Count > 0 || 
        bodyParts?.Count > 0 || 
        outfitParts?.Count > 0 || 
        hairstyleParts?.Count > 0 || 
        eyeParts?.Count > 0;
    }

    public SpritePart_SO GetSpritePart(CharSpriteType type,string name)
    {
        return type switch
        {
            CharSpriteType.Accessories => accessoryParts.Find(x => x.name == name),
            CharSpriteType.Bodies => bodyParts.Find(x => x.name == name),
            CharSpriteType.Outfits => outfitParts.Find(x => x.name == name),
            CharSpriteType.Eyes => eyeParts.Find(x => x.name == name),
            CharSpriteType.Hairstyles => hairstyleParts.Find(x => x.name == name),
            _ => null,
        };
    }

    #if UNITY_EDITOR
    public void PopulateSpriteLists(string destinationPath)
    {
        // Clear existing lists
        accessoryParts.Clear();
        bodyParts.Clear();
        outfitParts.Clear();
        hairstyleParts.Clear();
        eyeParts.Clear();

        // Get the body part folders in the destination path
        string[] bodyPartFolders = Directory.GetDirectories(destinationPath);
        foreach (string bodyPartFolder in bodyPartFolders)
        {
            string bodyPartName = Path.GetFileName(bodyPartFolder);
            CharSpriteType type = GetSpriteTypeFromPath(bodyPartFolder);

            // Load all textures in the folder
            Texture2D[] textures = Resources.LoadAll<Texture2D>("FingTools/Sprites/" + bodyPartName);

            // Create a SpritePart for each texture
            foreach (Texture2D texture in textures)
            {
                // Load all sprites in the folder
                Sprite[] sprites = Resources.LoadAll<Sprite>("FingTools/Sprites/" + bodyPartName + "/" + texture.name);

                // Create a SpritePart with the sprites
                SpritePart_SO spritePart = CreateInstance<SpritePart_SO>();
                spritePart.type = type;
                spritePart.sprites = sprites;

                // Save the SpritePart as an asset
                string assetName = texture.name + ".asset";
                string assetPath = Path.Combine("Assets/Resources/FingTools/ScriptableObjects/" + bodyPartName + "/", assetName);
                if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                }
                AssetDatabase.CreateAsset(spritePart, assetPath);

                // Add the SpritePart to the appropriate list
                switch (type)
                {
                    case CharSpriteType.Accessories:
                        accessoryParts.Add(spritePart);
                        break;
                    case CharSpriteType.Bodies:
                        bodyParts.Add(spritePart);
                        break;
                    case CharSpriteType.Outfits:
                        outfitParts.Add(spritePart);
                        break;
                    case CharSpriteType.Hairstyles:
                        hairstyleParts.Add(spritePart);
                        break;
                    case CharSpriteType.Eyes:
                        eyeParts.Add(spritePart);
                        break;
                }
            }
        }

        // Mark the manager as dirty and save the changes
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();        
    }
    #endif
    private static CharSpriteType GetSpriteTypeFromPath(string folderPath)
{
    if (folderPath.Contains("Accessories"))
        return CharSpriteType.Accessories;
    if (folderPath.Contains("Bodies"))
        return CharSpriteType.Bodies;
    if (folderPath.Contains("Outfits"))
        return CharSpriteType.Outfits;
    if (folderPath.Contains("Hairstyles"))
        return CharSpriteType.Hairstyles;
    if (folderPath.Contains("Eyes"))
        return CharSpriteType.Eyes;
    
    return CharSpriteType.Accessories; // Default case
}
}

public enum CharSpriteType {Accessories,Bodies,Outfits,Hairstyles,Eyes}
}