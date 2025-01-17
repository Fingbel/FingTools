using UnityEngine;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.IO;
using UnityEditor;

namespace FingTools.Internal{

    public static class PortraitImporter
    {
        private static readonly List<int>  spritesPerRowList = new List<int> {10,10,10};
        private static readonly List<string> validBodyParts = new () { "Accessory",  "Eyes", "Hairstyle", "Skin" };
        public const string resourcesPortraitFolderPath = "Assets/Resources/FingTools/PortraitSprites"; 
        public const string portraitsFolderPath = "Assets/Resources/FingTools/Portraits";
        #if UNITY_EDITOR
        public static void UnzipUISprites(string zipFilePath, string spriteSize, bool enableMaxAssetsPerType, int maxAssetsPerType)
        {
            Dictionary<PortraitPartType, int> processedAssetsPerType = new Dictionary<PortraitPartType, int>()
            {
                { PortraitPartType.Accessory, 0 },
                { PortraitPartType.Eyes, 0 },
                { PortraitPartType.Hairstyle, 0 },
                { PortraitPartType.Skin, 0 }                
            };     
            ZipArchive archive = ZipFile.OpenRead(zipFilePath);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                PortraitPartType? type = validBodyParts.FirstOrDefault(x => entry.FullName.Contains(x)) switch
                {
                    "Accessory" => PortraitPartType.Accessory,
                    "Eyes" => PortraitPartType.Eyes,
                    "Hairstyle" => PortraitPartType.Hairstyle,
                    "Skin" => PortraitPartType.Skin,
                    _ => null
                };

                if (type == null)
                    continue;

                if (enableMaxAssetsPerType && processedAssetsPerType[type.Value] >= maxAssetsPerType)
                {
                    //Debug.Log($"Reached the limit for {type.Value}");
                    continue;
                }
                string sizeDir = $"{spriteSize}x{spriteSize}";
                string expectedPath = $"{sizeDir}/Portrait_Generator";

                if (entry.FullName.StartsWith(expectedPath) && entry.FullName.EndsWith(".png"))
                {        
                    string outputPath = resourcesPortraitFolderPath+ $"/{type}/";
                    if (!Directory.Exists(outputPath))
                        Directory.CreateDirectory(outputPath);

                    string outputFilePath = $"{outputPath}/{entry.Name}";
                    if (!File.Exists(outputFilePath))
                    {                        
                        entry.ExtractToFile(outputFilePath, false);
                    }
                    processedAssetsPerType[type.Value]++;
                }
            }
            archive.Dispose();
        }
        public static void ProcessImportedAsset(string selectedSize)
        {
            int i = 0;
            var importList = PrepareImportList();
            foreach (var assetFile in importList)
            {
                string relativeAssetPath = assetFile.Replace(Application.dataPath, "").Replace("\\", "/");
                CommonImporter.ApplyImportSettings(AssetImporter.GetAtPath(relativeAssetPath) as TextureImporter, selectedSize,2048);
                CommonImporter.AutoSliceTexture(relativeAssetPath, spritesPerRowList, selectedSize,true);
                EditorUtility.DisplayProgressBar("Processing Assets", $"Slicing asset",0.5f);
                i++;
            }
            EditorUtility.ClearProgressBar();
        }

        private static List<string> PrepareImportList()
        {
            List<string> importList = new();
            string[] bodyPartFolders = Directory.GetDirectories(resourcesPortraitFolderPath);
            // Iterate through body part folders and find assets
            foreach (string bodyPartFolder in bodyPartFolders)
            {
                string[] assetFiles = Directory.GetFiles(bodyPartFolder, "*.png", SearchOption.AllDirectories);

                foreach (string assetFile in assetFiles)
                {                    
                    importList.Add(assetFile);
                }
            }
            return importList;
        }        

        public static void BuildPortraitFromActorSO(ref Actor_SO actor_SO)
        {
            if(actor_SO.portrait_SO == null)
            {
                Portrait_SO newPortrait =  ScriptableObject.CreateInstance<Portrait_SO>();
                actor_SO.portrait_SO = newPortrait;            
                string path = $"{portraitsFolderPath}/{actor_SO.name}.asset";
                if(!Directory.Exists(portraitsFolderPath))
                {
                    Directory.CreateDirectory(portraitsFolderPath);
                }
                AssetDatabase.CreateAsset(newPortrait, path);
                AssetDatabase.SaveAssets();
            }
            
            //Here we should add the portrait parts here, we have the actor_SO as a source

            

           
        }
        public static void RenamePortrait(string newName,Actor_SO selectedActor)
        {
            string AssetPath = AssetDatabase.GetAssetPath(selectedActor.portrait_SO);
            string error = AssetDatabase.RenameAsset(AssetPath, newName);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError($"Failed to rename portrait asset: {error}");
            }
             else
            {
                selectedActor.name = newName; 
            }
            
        }

        [InitializeOnLoadMethod]
        public static void CheckForMissingActors()
        {
            var portraits = Resources.LoadAll<Portrait_SO>("FingTools/Portraits");    
            var actors = Resources.LoadAll<Actor_SO>("FingTools/Actors");    
            foreach(var portrait in portraits)
            {
                bool hasCorrespondingActor = actors.Any(actor => actor.portrait_SO == portrait);
                if (!hasCorrespondingActor)
                {
                    string assetPath = AssetDatabase.GetAssetPath(portrait);
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }
        }
        #endif
}
}