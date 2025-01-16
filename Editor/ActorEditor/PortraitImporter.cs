using UnityEngine;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.IO;
using UnityEditor;

namespace FingTools.Internal{
    public enum PortraitPartType{Accessory,Eyes,Hairstyle,Skin}
    public static class PortraitImporter
    {
        private static readonly List<int>  spritesPerRowList = new List<int> {10,10,10};
        private static readonly List<string> validBodyParts = new () { "Accessory",  "Eyes", "Hairstyle", "Skin" };
        public const string resourcesPortraitFolderPath = "Assets/Resources/FingTools/PortraitSprites"; 
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
        #endif
}
}