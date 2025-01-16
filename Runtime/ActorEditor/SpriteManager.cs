using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FingTools.Internal
{
[System.Serializable]
public class SpriteManager : ScriptableObject 
{
    private static SpriteManager _instance;
     public static SpriteManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Load the Singleton instance from Resources or create one if not found
                    _instance = Resources.Load<SpriteManager>("FingTools/SpriteManager");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<SpriteManager>();
                        if(!Directory.Exists("Assets/Resources/FingTools"))
                        {
                            Directory.CreateDirectory("Assets/Resources/FingTools");
                        }
                        #if UNITY_EDITOR
                        AssetDatabase.CreateAsset(_instance, "Assets/Resources/FingTools/SpriteManager.asset");
                        EditorApplication.delayCall += () => AssetDatabase.SaveAssets();
                        #endif
                    
                    }
                }
                return _instance;
            }
        }
    public List<SpritePart_SO> accessoryParts = new();
    public List<SpritePart_SO> bodyParts = new();
    public List<SpritePart_SO> outfitParts = new();
    public List<SpritePart_SO> hairstyleParts = new();
    public List<SpritePart_SO> eyeParts = new();           
    private int selectedSizeIndex;
    public int SelectedSizeIndex { get => selectedSizeIndex; set => selectedSizeIndex = value;}
    private Dictionary<ActorPartType, int> spriteCounts = new Dictionary<ActorPartType, int>(); //TODO : remove this dictionary and use the scriptable list to count

    // Method to set the count for a specific CharSpriteType
    public void SetSpriteCount(ActorPartType type, int count)
    {
        spriteCounts[type] = count; // Update the count
    }

    public int GetSpriteCount(ActorPartType type)
    {
        return type switch
        {
            ActorPartType.Accessories => accessoryParts.Count,
            ActorPartType.Bodies => bodyParts.Count,
            ActorPartType.Outfits => outfitParts.Count,
            ActorPartType.Eyes => eyeParts.Count,
            ActorPartType.Hairstyles => hairstyleParts.Count,
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

    public SpritePart_SO GetSpritePart(ActorPartType type,string name)
    {
        return type switch
        {
            ActorPartType.Accessories => accessoryParts.Find(x => x.name == name),
            ActorPartType.Bodies => bodyParts.Find(x => x.name == name),
            ActorPartType.Outfits => outfitParts.Find(x => x.name == name),
            ActorPartType.Eyes => eyeParts.Find(x => x.name == name),
            ActorPartType.Hairstyles => hairstyleParts.Find(x => x.name == name),
            _ => null,
        };
    }

    #if UNITY_EDITOR
    public void PopulateSpriteLists(string destinationPath)
    {
        if(!Directory.Exists("Assets/Resources/FingTools/CharacterSprites")) return; //Early return as we don't have no directory
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
            ActorPartType type = GetSpriteTypeFromPath(bodyPartFolder);

            // Load all textures in the folder
            Texture2D[] textures = Resources.LoadAll<Texture2D>("FingTools/CharacterSprites/" + bodyPartName);

            // Create a SpritePart for each texture
            foreach (Texture2D texture in textures)
            {
                // Load all sprites in the folder
                Sprite[] sprites = Resources.LoadAll<Sprite>("FingTools/CharacterSprites/" + bodyPartName + "/" + texture.name);

                // Create a SpritePart with the sprites
                SpritePart_SO spritePart = CreateInstance<SpritePart_SO>();
                spritePart.type = type;
                spritePart.sprites = sprites;

                // Save the SpritePart as an asset
                string assetName = texture.name + ".asset";
                string assetPath = Path.Combine("Assets/Resources/FingTools/ScriptableObjects/CharacterParts/" + bodyPartName + "/", assetName);
                if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                }
                AssetDatabase.CreateAsset(spritePart, assetPath);

                // Add the SpritePart to the appropriate list
                switch (type)
                {
                    case ActorPartType.Accessories:
                        accessoryParts.Add(spritePart);
                        break;
                    case ActorPartType.Bodies:
                        bodyParts.Add(spritePart);
                        break;
                    case ActorPartType.Outfits:
                        outfitParts.Add(spritePart);
                        break;
                    case ActorPartType.Hairstyles:
                        hairstyleParts.Add(spritePart);
                        break;
                    case ActorPartType.Eyes:
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
    public ActorPartType GetSpriteTypeFromPath(string folderPath)
{
    if (folderPath.Contains("Accessories"))
        return ActorPartType.Accessories;
    if (folderPath.Contains("Bodies"))
        return ActorPartType.Bodies;
    if (folderPath.Contains("Outfits"))
        return ActorPartType.Outfits;
    if (folderPath.Contains("Hairstyles"))
        return ActorPartType.Hairstyles;
    if (folderPath.Contains("Eyes"))
        return ActorPartType.Eyes;
    
    return ActorPartType.Accessories; // Default case
}
public ActorPartType GetSpriteTypeFromAssetName(string assetName)
{
    if (assetName.Contains("Accessory"))
        return ActorPartType.Accessories;
    if (assetName.Contains("Body"))
        return ActorPartType.Bodies;
    if (assetName.Contains("Outfit"))
        return ActorPartType.Outfits;
    if (assetName.Contains("Hairstyle"))
        return ActorPartType.Hairstyles;
    if (assetName.Contains("Eyes"))
        return ActorPartType.Eyes;
    
    return ActorPartType.Accessories; // Default case
}
}


}