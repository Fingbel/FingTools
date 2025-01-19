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
        
        #if UNITY_EDITOR
        public static void BuildPortraitFromActorSO(ref Actor_SO actor_SO)
        {
            if(actor_SO.portrait_SO == null)
            {
                Portrait_SO newPortrait =  ScriptableObject.CreateInstance<Portrait_SO>();
                actor_SO.portrait_SO = newPortrait;            
                string path = $"{CharacterImporter.portraitsFolderPath}/{actor_SO.name}.asset";
                if(!Directory.Exists(CharacterImporter.portraitsFolderPath))
                {
                    Directory.CreateDirectory(CharacterImporter.portraitsFolderPath);
                }
                AssetDatabase.CreateAsset(newPortrait, path);
                AssetDatabase.SaveAssets();
            }
            
            //Here we should add the portrait parts here, we have the actor_SO as a source
            //But first we need to build the scriptables and librairies

        }        
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
                    string outputPath = CharacterImporter.resourcesPortraitFolderPath+ $"/{type}/";
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
            string[] bodyPartFolders = Directory.GetDirectories(CharacterImporter.resourcesPortraitFolderPath);
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
        public static void MissingChecks()
        {
            var portraits = Resources.LoadAll<Portrait_SO>("FingTools/Portraits");    
            var actors = Resources.LoadAll<Actor_SO>("FingTools/Actors");  
            CheckForMissingActors(portraits,actors);
            CheckForMissingPortraits(portraits,actors);
        }
        private static void CheckForMissingActors(Portrait_SO[] portraits, Actor_SO[] actors)
        {
            
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
        private static void CheckForMissingPortraits(Portrait_SO[] portraits, Actor_SO[] actors)
        {            
            foreach(var actor in actors)
            {
                bool hasCorrespondingPortrait =
                portraits.Any(portrait => portrait == actor.portrait_SO);
                if (!hasCorrespondingPortrait)
                {
                    var tempActor = actor;
                    BuildPortraitFromActorSO(ref tempActor);
                }
            }

        }
        #endif
}
}