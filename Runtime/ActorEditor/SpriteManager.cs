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
    public List<ActorSpritePart_SO> accessoryParts = new();
    public List<ActorSpritePart_SO> bodyParts = new();
    public List<ActorSpritePart_SO> outfitParts = new();
    public List<ActorSpritePart_SO> hairstyleParts = new();
    public List<ActorSpritePart_SO> eyeParts = new();           
    public List<PortraitPart_SO> bodyPortraitParts = new();
    public List<PortraitPart_SO> eyePortraitParts = new();
    public List<PortraitPart_SO> hairstylePortraitParts = new();
    public List<PortraitPart_SO> accessoryPortraitParts = new();

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

    public ActorSpritePart_SO GetActorSpritePart(ActorPartType type,string name)
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
    public static void PopulatePortraitSpriteLists(string destinationPath)
    {
        if(!Directory.Exists(destinationPath)) return;
        Instance.bodyPortraitParts.Clear();
        Instance.eyePortraitParts.Clear();
        Instance.hairstylePortraitParts.Clear();
        Instance.accessoryPortraitParts.Clear();

        string[] portraitPartFolders = Directory.GetDirectories(destinationPath);
        foreach (string portraitPartFolder in portraitPartFolders)
        {
            string PortraitPartName = Path.GetFileName(portraitPartFolder);
            PortraitPartType type = (PortraitPartType)Instance.GetPortraitPartTypeFromPath(portraitPartFolder);
            
            // Load all textures in the folder
            Texture2D[] textures = Resources.LoadAll<Texture2D>("FingTools/Sprites/PortraitSprites/" + PortraitPartName);
            // Create a SpritePart for each texture
            foreach (Texture2D texture in textures)
            {
                // Load all sprites in the folder
                Sprite[] sprites = Resources.LoadAll<Sprite>("FingTools/Sprites/PortraitSprites/" + PortraitPartName + "/" + texture.name);

                // Look for an existing SpritePart or Create a SpritePart with the sprites
                PortraitPart_SO portraitPart;
                var part = Resources.Load<PortraitPart_SO>($"FingTools/ScriptableObjects/PortraitParts/{PortraitPartName}/{texture.name}");
                if(part != null)
                {                    
                    portraitPart = part;
                }
                else
                {
                    portraitPart= CreateInstance<PortraitPart_SO>();
                    string assetName = texture.name + ".asset";
                    string assetPath = Path.Combine("Assets/Resources/FingTools/ScriptableObjects/PortraitParts/" + PortraitPartName + "/", assetName);
                    if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                    }
                    AssetDatabase.CreateAsset(portraitPart, assetPath);
                }       
                portraitPart.type = type;
                portraitPart.sprites = sprites;                            

                // Add the SpritePart to the appropriate list
                switch (type)
                {
                    case PortraitPartType.Accessory:
                        Instance.accessoryPortraitParts.Add(portraitPart);
                        break;
                    case PortraitPartType.Skin:
                        Instance.bodyPortraitParts.Add(portraitPart);
                        break;
                    case PortraitPartType.Eyes:
                        Instance.eyePortraitParts.Add(portraitPart);
                        break;
                    case PortraitPartType.Hairstyle:
                        Instance.hairstylePortraitParts.Add(portraitPart);
                        break;                    
                }
            }
        }
    }
    public static void PopulateActorSpriteLists(string destinationPath)
    {
        if(!Directory.Exists(destinationPath)) return; //Early return as we don't have no directory
        // Clear existing lists
        Instance.accessoryParts.Clear();
        Instance.bodyParts.Clear();
        Instance.outfitParts.Clear();
        Instance.hairstyleParts.Clear();
        Instance.eyeParts.Clear();

        // Get the body part folders in the destination path
        string[] bodyPartFolders = Directory.GetDirectories(destinationPath);
        foreach (string bodyPartFolder in bodyPartFolders)
        {
            string bodyPartName = Path.GetFileName(bodyPartFolder);
            var ntype = Instance.GetActorSpriteTypeFromPath(bodyPartFolder);
            ActorPartType type;
            if(ntype != null)
            {
                type = (ActorPartType)ntype;
            }
            else
            {
                Debug.LogError($"Type undefined for {bodyPartFolder}");
                return;
            }

            // Load all textures in the folder
            Texture2D[] textures = Resources.LoadAll<Texture2D>("FingTools/Sprites/CharacterSprites/" + bodyPartName);

            // Create a SpritePart for each texture
            foreach (Texture2D texture in textures)
            {
                // Load all sprites in the folder
                Sprite[] sprites = Resources.LoadAll<Sprite>("FingTools/Sprites/CharacterSprites/" + bodyPartName + "/" + texture.name);

                // Look for an existing SpritePart or Create a SpritePart with the sprites
                ActorSpritePart_SO spritePart;
                var part = Resources.Load<ActorSpritePart_SO>($"FingTools/ScriptableObjects/CharacterParts/{bodyPartName}/{texture.name}");
                if(part != null)
                {
                    spritePart = part;
                }
                else
                {
                    spritePart= CreateInstance<ActorSpritePart_SO>();
                    string assetName = texture.name + ".asset";
                    string assetPath = Path.Combine("Assets/Resources/FingTools/ScriptableObjects/CharacterParts/" + bodyPartName + "/", assetName);
                    if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
                    }
                    AssetDatabase.CreateAsset(spritePart, assetPath);
                }                
                spritePart.type = type;
                spritePart.sprites = sprites;                            

                // Add the SpritePart to the appropriate list
                switch (type)
                {
                    case ActorPartType.Accessories:
                        Instance.accessoryParts.Add(spritePart);
                        break;
                    case ActorPartType.Bodies:
                        Instance.bodyParts.Add(spritePart);
                        break;
                    case ActorPartType.Outfits:
                        Instance.outfitParts.Add(spritePart);
                        break;
                    case ActorPartType.Hairstyles:
                        Instance.hairstyleParts.Add(spritePart);
                        break;
                    case ActorPartType.Eyes:
                        Instance.eyeParts.Add(spritePart);
                        break;
                }
            }
        }

        // Mark the manager as dirty and save the changes
        EditorUtility.SetDirty(Instance);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();        
    }
    #endif
    public ActorPartType? GetActorSpriteTypeFromPath(string folderPath)
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
        
        return null; // Default case
    }
    
    public ActorPartType GetActorSpriteTypeFromAssetName(string assetName)
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
    public PortraitPartType? GetPortraitPartTypeFromPath(string folderPath)
    {
        if (folderPath.Contains("Accessory"))
            return PortraitPartType.Accessory;
        if (folderPath.Contains("Eyes"))
            return PortraitPartType.Eyes;
        if (folderPath.Contains("Hairstyle"))
            return PortraitPartType.Hairstyle;
        if (folderPath.Contains("Skin"))
            return PortraitPartType.Skin;
        
        return null; // Default case
    }
}


}