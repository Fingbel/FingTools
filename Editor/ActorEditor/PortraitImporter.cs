using UnityEngine;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.IO;
using UnityEditor;
using System.Text.RegularExpressions;

namespace FingTools.Internal{

    public static class PortraitImporter
    {
        private static readonly List<int>  spritesPerRowList = new List<int> {10,10,10};
        private static readonly List<string> validBodyParts = new () { "Accessory",  "Eyes", "Hairstyle", "Skin" };
        
        #if UNITY_EDITOR
        public static void ResolvePortrait(Portrait_SO portrait_SO,Actor_SO actor_SO)
        {            
            if(actor_SO.accessory != null) 
                portrait_SO.accessory = ResolvePortraitPart(PortraitPartType.Accessory,actor_SO.accessory.name);                
            else 
                portrait_SO.accessory = null;
                
            if(actor_SO.eyes != null) 
                portrait_SO.eyes = ResolvePortraitPart(PortraitPartType.Eyes,actor_SO.eyes.name);
            else
                portrait_SO.eyes = null;

            if(actor_SO.hairstyle != null) 
                portrait_SO.hairstyle = ResolvePortraitPart(PortraitPartType.Hairstyle,actor_SO.hairstyle.name);
            else
                portrait_SO.hairstyle = null;

            if(actor_SO.body != null)
                portrait_SO.body = ResolvePortraitPart(PortraitPartType.Skin,actor_SO.body.name);
            else
                portrait_SO.body = null;
        }
        public static PortraitPart_SO ResolvePortraitPart(PortraitPartType portraitPartType, string actorPartName)
        {
            if(string.IsNullOrEmpty(actorPartName)) return null;
            // Step 1: Add the "PG_" prefix to the actorPartName
            string expectedPortraitPartName = "PG_" + actorPartName;

            // Now process the PortraitPartType
            switch (portraitPartType)
            {
                case PortraitPartType.Accessory:
                    expectedPortraitPartName = Regex.Replace(expectedPortraitPartName, @"(_0[1-9])$", match =>
                    {
                        return match.Value.Replace("0", "");
                    });
                    var newAccessory = SpriteManager.Instance.accessoryPortraitParts.Where(x => x.name == expectedPortraitPartName).FirstOrDefault();
                    if (newAccessory != null)
                    {
                        return newAccessory;
                    }
                    break;

                case PortraitPartType.Eyes:
                    var newEyes = SpriteManager.Instance.eyePortraitParts.Where(x => x.name == expectedPortraitPartName).FirstOrDefault();
                    if (newEyes != null)
                    {
                        return  newEyes;
                    }
                    break;

                case PortraitPartType.Hairstyle:
                    expectedPortraitPartName = Regex.Replace(expectedPortraitPartName, @"(_0[1-9])$", match =>
                    {
                        return match.Value.Replace("0", "");
                    });
                    var newHairstyle = SpriteManager.Instance.hairstylePortraitParts.Where(x => x.name == expectedPortraitPartName).FirstOrDefault();
                    if (newHairstyle != null)
                    {
                       return  newHairstyle;
                    }
                    break;

                case PortraitPartType.Skin:
                    expectedPortraitPartName = "PG_Skin" + actorPartName.Substring(4);
                    expectedPortraitPartName = Regex.Replace(expectedPortraitPartName, @"(_0[1-9])$", match =>
                    {
                        return match.Value.Replace("0", "");
                    });
                    var newSkin = SpriteManager.Instance.bodyPortraitParts.Where(x => x.name == expectedPortraitPartName).FirstOrDefault();
                    if (newSkin != null)
                    {
                        return  newSkin;                    
                    }
                    break;
            }
            return null;
        }
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
                
            }
            ResolvePortrait(actor_SO.portrait_SO,actor_SO);
            EditorUtility.SetDirty(actor_SO.portrait_SO);
            AssetDatabase.SaveAssets();
        }        
        public static void UnzipUISprites(string zipFilePath, string spriteSize, bool enableMaxAssetsPerType, int maxAssetsPerType)
        {
            Dictionary<PortraitPartType, int> processedAssetsPerType = new ()
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
                EditorUtility.DisplayProgressBar("Processing Assets", $"Slicing asset{i + 1} of {importList.Count}",(i + 1) / (float)importList.Count);
                i++;
            }
            EditorUtility.ClearProgressBar();
            foreach (var assetPath in importList)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
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