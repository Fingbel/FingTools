using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System;


#if UNITY_EDITOR
using UnityEditor.U2D.Sprites;
namespace FingTools.Internal
{
    public static class CharacterImporter
    {        
        public const string resourcesCharacterFolderPath = "Assets/Resources/FingTools/Sprites/CharacterSprites"; 
        public const string resourcesPortraitFolderPath = "Assets/Resources/FingTools/Sprites/PortraitSprites"; 
        public const string ActorLibraryRootFolderName = "SpriteLibrairies/CharacterParts";
        public const string PortraitLibraryRootFolderName = "SpriteLibrairies/PortraitParts";
        public const string portraitsFolderPath = "Assets/Resources/FingTools/Portraits";
        public const string actorsFolderPath = "Assets/Resources/FingTools/Actors";
        private static readonly List<string> validBodyParts = new () { "Accessories","Accessory", "Bodies", "Eyes", "Hairstyles", "Outfits","Outfit" };
        private const int slicePerSprite = 467;         
        private static readonly List<int>  spritesPerRowList = new List<int> { 4, 24, 24, 6, 12, 12, 12, 12, 24, 48, 40, 56, 56, 24, 24, 24, 16, 24, 12, 12 };

        #if FINGDEBUG
        [MenuItem("FingTools/DEBUG/Rebuild Assets")]
        #endif
        public static void LinkCharAssets()
        {
            SpriteManager.PopulateActorSpriteLists(resourcesCharacterFolderPath);
            SpriteManager.PopulatePortraitSpriteLists(resourcesPortraitFolderPath);

            SpriteLibraryBuilder.BuildAllActorSpriteLibrairies();
            SpriteLibraryBuilder.BuildAllPortraitSpriteLibrairies();

            Directory.CreateDirectory(actorsFolderPath);

            AssetEnumGenerator.GenerateAssetEnum();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        public static void UnzipIntSprites(string zipFilePath, string spriteSize, ref int unzipedAssets, bool enableMaxAssetsPerType, int maxAssetsPerType)
        {
            unzipedAssets = 0;     
            Dictionary<ActorPartType, int> processedAssetsPerType = new Dictionary<ActorPartType, int>()
            {
                { ActorPartType.Accessories, 0 },
                { ActorPartType.Bodies, 0 },
                { ActorPartType.Eyes, 0 },
                { ActorPartType.Hairstyles, 0 },
                { ActorPartType.Outfits, 0 }
            };       
            ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                ActorPartType? type = validBodyParts.FirstOrDefault(x => entry.FullName.Contains(x)) switch
                {
                    "Accessories" => ActorPartType.Accessories,
                    "Bodies"     => ActorPartType.Bodies,
                    "Eyes"       => ActorPartType.Eyes,
                    "Hairstyles" => ActorPartType.Hairstyles,
                    "Outfits"    => ActorPartType.Outfits,
                    _            => null
                };

                if (type == null)
                    continue;

                if (enableMaxAssetsPerType && processedAssetsPerType[type.Value] >= maxAssetsPerType)
                {
                    //Debug.Log($"Reached the limit for {type.Value}");
                    continue;
                }

                string sizeDir = $"{spriteSize}x{spriteSize}";
                string expectedPath = $"2_Characters/Character_Generator/{type}/{sizeDir}";
                
                if (entry.FullName.StartsWith(expectedPath) && entry.FullName.EndsWith(".png"))
                {
                    string outputPath = resourcesCharacterFolderPath+ $"/{type}/";
                    if (!Directory.Exists(outputPath))
                        Directory.CreateDirectory(outputPath);

                    if(!File.Exists($"{outputPath}/{entry.Name}"))
                    {
                        entry.ExtractToFile($"{outputPath}/{entry.Name}", false);
                    }
                    processedAssetsPerType[type.Value]++;
                    unzipedAssets++;
                }
            }
            archive.Dispose();
        }

        public static void UnzipExtSprites(string zipFilePath, string spriteSize, ref int unzipedAssets, bool enableMaxAssetsPerType, int maxAssetsPerType)
        {
            Dictionary<ActorPartType, int> processedAssetsPerType = new Dictionary<ActorPartType, int>()
            {
                { ActorPartType.Accessories, 0 },
                { ActorPartType.Bodies, 0 },
                { ActorPartType.Eyes, 0 },
                { ActorPartType.Hairstyles, 0 },
                { ActorPartType.Outfits, 0 }
            };       
            ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                ActorPartType? type = validBodyParts.FirstOrDefault(x => entry.FullName.Contains(x)) switch
                {
                    "Accessory" => ActorPartType.Accessories, 
                    "Outfit"    => ActorPartType.Outfits,
                    _            => null
                };

                if (type == null)
                    continue;                   
                
                if (enableMaxAssetsPerType && processedAssetsPerType[type.Value] >= maxAssetsPerType)
                {
                    //Debug.Log($"Reached the limit for {type.Value}");
                    continue;
                }

                string expectedPath = $"Modern_Exteriors_{spriteSize}x{spriteSize}/Character_Generator_Addons_{spriteSize}x{spriteSize}";
                if (entry.FullName.StartsWith(expectedPath) && entry.FullName.EndsWith(".png"))
                {
                    string outputPath = resourcesCharacterFolderPath+ $"/{type}/";
                    if (!Directory.Exists(outputPath))
                        Directory.CreateDirectory(outputPath);

                    if(!File.Exists($"{outputPath}/{entry.Name}"))
                    {
                        entry.ExtractToFile($"{outputPath}/{entry.Name}", false);                        
                    }                    
                    processedAssetsPerType[type.Value]++;
                    unzipedAssets++;
                }
            }
            archive.Dispose();
        }

        public static void ProcessImportedAssets(string selectedSize)
        {            
            int i = 0;
            if(!Directory.Exists(resourcesCharacterFolderPath)) return; //Early return as we don't have no directory
            var importList = PrepareImportList(resourcesCharacterFolderPath);
            int totalAssetsToProcess = importList.Count;
            foreach (var assetFile in importList)
            {
                string relativeAssetPath = assetFile.Replace(Application.dataPath, "").Replace("\\", "/");
                CommonImporter.ApplyImportSettings(AssetImporter.GetAtPath(relativeAssetPath) as TextureImporter, selectedSize,4096);
                CommonImporter.AutoSliceTexture(relativeAssetPath, spritesPerRowList, selectedSize);
                EditorUtility.DisplayProgressBar("Processing Assets", $"Slicing asset {i + 1} of {totalAssetsToProcess} ", (i + 1) / (float)totalAssetsToProcess);
                i++;
            }

            EditorUtility.ClearProgressBar();
            foreach (var assetPath in importList)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
        }

        private static List<string> PrepareImportList(string destinationFolderPath)
        {
            List<string> outputList = new List<string>();
            string[] bodyPartFolders = Directory.GetDirectories(destinationFolderPath);
            // Iterate through body part folders and find assets
            foreach (string bodyPartFolder in bodyPartFolders)
            {
                string[] assetFiles = Directory.GetFiles(bodyPartFolder, "*.png", SearchOption.AllDirectories);

                foreach (string assetFile in assetFiles)
                {
                    string assetName = Path.GetFileNameWithoutExtension(assetFile);
                    string spritePartSoPath = Path.Combine("Assets/Resources/FingTools/ScriptableObjects/CharacterParts/", bodyPartFolder.Split("\\").Last(), $"{assetName}.asset");
                    // Check if the asset has already been processed
                    if (AssetDatabase.LoadAllAssetsAtPath(assetFile).Length == slicePerSprite &&
                        File.Exists(spritePartSoPath))
                    {
                        continue;
                    }

                    outputList.Add(assetFile);
                }
            }
            return outputList;
        }
    }
}
#endif
