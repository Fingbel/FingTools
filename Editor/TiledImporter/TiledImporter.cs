using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System;

#if SUPER_TILED2UNITY_INSTALLED
using SuperTiled2Unity;
#endif

namespace FingTools.Tiled
{
    public static class TiledImporter
    {
        public static bool ValidateInteriorZipFile(string zipFilePath)
        {
            bool output = true;
            var fileName = Path.GetFileName(zipFilePath);
            if (string.IsNullOrEmpty(zipFilePath) || fileName != "moderninteriors-win.zip")
                output = false;
            return output;
        }

        public static bool ValidateExteriorZipFile(string zipFilePath)
        {
            bool output = true;
            var fileName = Path.GetFileName(zipFilePath);
            if (string.IsNullOrEmpty(zipFilePath)|| fileName != "modernexteriors-win.zip")
                output = false;
            return output;
        }
        public static void ImportAssets(bool importInterior, string selectedInteriorZipFile, List<string> selectedInteriorTilesets, bool importExterior, string selectedExteriorZipFile, List<string> selectedExteriorTilesets, string outputPath, int selectedSizeIndex, List<string> validSizes)
        {
            // Unzip assets first
            if (importInterior && !string.IsNullOrEmpty(selectedInteriorZipFile))
            {
                if (!ValidateInteriorZipFile(selectedInteriorZipFile))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid Modern Interior zip file. Please select the correct file.", "OK");
                    return;
                }
                UnzipInteriorAssets(selectedInteriorZipFile, validSizes[selectedSizeIndex], selectedInteriorTilesets, outputPath,validSizes[selectedSizeIndex].ToInt());
            }

            if (importExterior && !string.IsNullOrEmpty(selectedExteriorZipFile))
            {
                if (!ValidateExteriorZipFile(selectedExteriorZipFile))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid Modern Exterior zip file. Please select the correct file.", "OK");
                    return;
                }
                UnzipExteriorAssets(selectedExteriorZipFile, validSizes[selectedSizeIndex], selectedExteriorTilesets, outputPath,validSizes[selectedSizeIndex].ToInt());
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            #if SUPER_TILED2UNITY_INSTALLED
            // After unzipping, now generate the TSX files for each tileset
            if (importInterior && !string.IsNullOrEmpty(selectedInteriorZipFile))
            {
                string interiorArtOutput = Path.Combine(outputPath, "Art/Interior/");
                string interiorTilesetOutputPath = Path.Combine(outputPath, "Tilesets/Interior");
                AdjustTextureImportSettings(interiorArtOutput, 2048,validSizes[selectedSizeIndex].ToInt());
                GenerateTSXFilesForImportedTilesets(interiorArtOutput, "Interior", int.Parse(validSizes[selectedSizeIndex]), outputPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AutoFixTextures(interiorTilesetOutputPath);
                EditorApplication.delayCall += () =>
                {
                    foreach(var tileset in selectedInteriorTilesets)
                    {
                        var tilesetName = Path.GetFileNameWithoutExtension(tileset);
                        string assetPath = interiorTilesetOutputPath + "/" + tilesetName + ".tsx";
                        UpdatePixelsPerUnit(assetPath, validSizes[selectedSizeIndex].ToInt());
                    }
                };
            }

            if (importExterior && !string.IsNullOrEmpty(selectedExteriorZipFile))
            {
                string exteriorArtOutput = Path.Combine(outputPath, "Art/Exterior/");
                string exteriorTilesetOutputPath = Path.Combine(outputPath, "Tilesets/Exterior");
                AdjustTextureImportSettings(exteriorArtOutput, 4096,validSizes[selectedSizeIndex].ToInt());
                GenerateTSXFilesForImportedTilesets(exteriorArtOutput, "Exterior", int.Parse(validSizes[selectedSizeIndex]), outputPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AutoFixTextures(exteriorTilesetOutputPath);
                EditorApplication.delayCall += () =>
                {
                    foreach(var tileset in selectedExteriorTilesets)
                    {
                        var tilesetName = Path.GetFileNameWithoutExtension(tileset);
                        string assetPath = exteriorTilesetOutputPath + "/" + tilesetName + ".tsx";
                        UpdatePixelsPerUnit(assetPath, validSizes[selectedSizeIndex].ToInt());
                    }
                };
                
            }
            #endif

            // Add new tilesets to all existing maps
            AddTilesetsToExistingMaps(outputPath);
        }

        private static void UnzipInteriorAssets(string zipFilePath, string spriteSize, List<string> selectedInteriorTilesets, string outputPath,int pixelsPerUnit)
        {
            if (!Directory.Exists(outputPath + "/Art/Interior/"))
                Directory.CreateDirectory(outputPath + "/Art/Interior/");
            var archive = ZipFile.OpenRead(zipFilePath);

            // All the assets are inside : 1_Interiors/"spriteSize"x"spriteSize"/Theme_Sorter/*.png
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                var fullName = entry.FullName;
                var entryName = entry.Name;
                if (fullName.StartsWith($"1_Interiors/{spriteSize}x{spriteSize}/Theme_Sorter/") && fullName.EndsWith(".png"))
                {
                    if (selectedInteriorTilesets.Contains(entry.Name))
                    {
                        if (!Directory.Exists(outputPath + "/Art/Interior/"))
                            Directory.CreateDirectory(outputPath + "/Art/Interior/");
                        entry.ExtractToFile(outputPath + "/Art/Interior/" + entryName, true);
                    string assetPath = outputPath + "/Art/Interior/" + entryName;
                    TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (textureImporter != null)
                    {
                        textureImporter.spritePixelsPerUnit = pixelsPerUnit; // Set your desired pixels per unit value here
                        textureImporter.SaveAndReimport();
                    }
                    }
                }
            }
        }

        private static void UnzipExteriorAssets(string zipFilePath, string spriteSize, List<string> selectedExteriorTilesets, string outputPath, int pixelsPerUnit)
        {
            if (!Directory.Exists(outputPath + "/Art/Exterior/"))
                Directory.CreateDirectory(outputPath + "/Art/Exterior/");
            var archive = ZipFile.OpenRead(zipFilePath);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                var fullName = entry.FullName;
                var entryName = entry.Name;
                if (fullName.StartsWith($"Modern_Exteriors_{spriteSize}x{spriteSize}/ME_Theme_Sorter_{spriteSize}x{spriteSize}/") && fullName.EndsWith(".png") && !fullName.Contains("Singles") && !fullName.Contains("Old_Sorting"))
                {
                    if (selectedExteriorTilesets.Contains(entry.Name))
                    {
                        if (!Directory.Exists(outputPath + "/Art/Exterior/"))
                            Directory.CreateDirectory(outputPath + "/Art/Exterior/");
                        entry.ExtractToFile(outputPath + "/Art/Exterior/" + entryName, true);
                        string assetPath = outputPath + "/Art/Exterior/" + entryName;
                        TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (textureImporter != null)
                        {
                            textureImporter.spritePixelsPerUnit = pixelsPerUnit; // Set your desired pixels per unit value here
                            textureImporter.SaveAndReimport();
                        }
                    }
                }
            }
        }       

        private static void GenerateTSXFilesForImportedTilesets(string tilesetDirectory, string tilesetType, int tileSize, string outputPath)
        {
            // Define the folder path based on tileset type (Interior or Exterior)
            string tsxOutputPath = Path.Combine("Assets", "FingTools", "Tiled", "Tilesets", tilesetType);

            // Create the directory if it doesn't exist
            if (!Directory.Exists(tsxOutputPath))
            {
                Directory.CreateDirectory(tsxOutputPath);
            }

            // Get all the PNG files from the tileset directory
            string[] tilesetFiles = Directory.GetFiles(tilesetDirectory, "*.png", SearchOption.AllDirectories);

            // Use a HashSet to track processed files to avoid duplicates
            HashSet<string> processedTextures = new HashSet<string>();

            foreach (string filePath in tilesetFiles)
            {
                // Check if the texture has already been processed
                if (processedTextures.Contains(filePath))
                    continue;

                // Mark this texture as processed
                processedTextures.Add(filePath);

                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string tsxFileName = $"{fileName}.tsx";

                // Get the dimensions of the tileset image
                Texture2D texture = new Texture2D(2, 2);
                byte[] imageData = File.ReadAllBytes(filePath);
                texture.LoadImage(imageData);

                int width = texture.width;
                int height = texture.height;

                // Release memory used by the texture
                UnityEngine.Object.DestroyImmediate(texture);

                // Generate the TSX file
                GenerateTSXFile(
                    Path.Combine(tsxOutputPath, tsxFileName),
                    fileName, // Tileset name is the file name without extension
                    filePath.Replace(outputPath, "../../"), // Relative path for TSX file
                    width,
                    height,
                    tileSize
                );                
            }

            // Log the output path where TSX files are saved
            Debug.Log($"TSX files generated in: {tsxOutputPath}");
        }

        private static void GenerateTSXFile(string fileName, string tilesetName, string tileSet, int width, int height, int tileSize)
        {
            // Calculate tile count based on the image size and tile size
            int tileCount = width * height / (tileSize * tileSize);

            // Define the TSX file content
            string content =
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                $"<tileset version=\"1.10\" tiledversion=\"1.11.0\" name=\"{tilesetName}\" " +
                $"tilewidth=\"{tileSize}\" tileheight=\"{tileSize}\" tilecount=\"{tileCount}\" columns=\"{width / tileSize}\">\n" +
                $" <image source=\"{tileSet}\" width=\"{width}\" height=\"{height}\"/>\n" +
                $"</tileset>";

            // Define the file path in the Unity project's directory
            string filePath = Path.Combine(Application.dataPath, "..", fileName);

            try
            {
                // Write the content to the file
                File.WriteAllText(filePath, content);

                // Notify the user that the file was successfully created
                UnityEngine.Debug.Log($"TSX file generated at: {filePath}");
            }
            catch (IOException e)
            {
                // Handle any potential file write errors
                UnityEngine.Debug.LogError($"Failed to generate TSX file: {e.Message}");
            }
        }
        public static void UpdatePixelsPerUnit(string assetPath,int pixelsPerUnit)
        {           
            Debug.Log($"Updating Pixels Per Unit for {assetPath}");
            // Get the generic importer for the .tsx file
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
            {
                Debug.LogError($"No importer found for the asset at path: {assetPath}");
                return;
            }

            // Check if it's the correct importer
            Debug.Log($"Importer type detected: {importer.GetType().Name}");

            // Use SerializedObject to modify properties
            SerializedObject serializedObject = new SerializedObject(importer);
            SerializedProperty pixelsPerUnitProp = serializedObject.FindProperty("m_PixelsPerUnit");

            if (pixelsPerUnitProp != null)
            {
                float newValue = pixelsPerUnit;
                pixelsPerUnitProp.floatValue = newValue;
                serializedObject.ApplyModifiedProperties();

                // Force reimport to apply changes
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                Debug.Log($"Successfully updated Pixels Per Unit to {newValue} for {assetPath}");
            }
            else
            {
                Debug.LogWarning("Property 'm_PixelsPerUnit' not found. Ensure the importer supports this property.");
            }
        }        
        #if SUPER_TILED2UNITY_INSTALLED
        public static void AutoFixTextures(string tilesetPath)
        {
            // Get all .tsx files in the specified directory
            string[] tsxFiles = Directory.GetFiles(tilesetPath, "*.tsx", SearchOption.TopDirectoryOnly);
            AssetDatabase.StartAssetEditing();
            // Iterate over the .tsx files and attempt to find ImportErrors
            foreach (string tsxFile in tsxFiles)
            {
                ImportErrors importErrors = AssetDatabase.LoadAssetAtPath(tsxFile, typeof(ImportErrors)) as ImportErrors;                
                if (importErrors != null)
                {
                    // Iterate over the missing sprites in the ImportErrors and handle them
                    foreach (ImportErrors.MissingTileSprites missingTileSprite in importErrors.m_MissingTileSprites)
                    {
                        // Call method to add missing sprites
                        CallAddSpritesToTexture(missingTileSprite.m_TextureAssetPath, missingTileSprite.m_MissingSprites.Select(m => m.m_Rect));
                    }                    
                }
            }
            AssetDatabase.StopAssetEditing();
        }        
        #endif

        public static void CallAddSpritesToTexture(string textureAssetPath, IEnumerable<Rect> missingSpritesRects)
        {
            Assembly targetAssembly = null;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.Contains("Super Tiled2Unity Editor"))
                {
                    targetAssembly = assembly;
                    break;
                }
            }
            // Proceed if the assembly is found
            if (targetAssembly != null)
            {
                Type targetType = targetAssembly.GetType("SuperTiled2Unity.Editor.AddST2USpritesToTexture");
                if (targetType != null)
                {
                    MethodInfo methodInfo = targetType.GetMethod("AddSpritesToTextureAsset", BindingFlags.NonPublic | BindingFlags.Static);
                    if (methodInfo != null)
                    {
                        // Invoke the method using reflection
                        methodInfo.Invoke(null, new object[] { textureAssetPath, missingSpritesRects });
                    }
                    else
                    {
                        Debug.LogError("Method not found.");
                    }
                }
                else
                {
                    Debug.LogError("Class not found.");
                }
            }
            else
            {
                Debug.LogError("Assembly not found.");
            }
        }

        public static void AdjustTextureImportSettings(string textureDirectory, int maxTextureSize, int pixelsPerUnit)
        {
            UnityEngine.Debug.Log(textureDirectory);
            string[] textureFiles = Directory.GetFiles(textureDirectory, "*.png", SearchOption.TopDirectoryOnly);
            UnityEngine.Debug.Log(textureFiles.Length);
            foreach (string textureFile in textureFiles)
            {
                UnityEngine.Debug.Log(textureFile);
                if (textureFile.Contains("5_Floor"))
                {
                    maxTextureSize = 8192;
                }
                // Load the texture importer for the current texture
                TextureImporter textureImporter = AssetImporter.GetAtPath(textureFile) as TextureImporter;

                if (textureImporter != null)
                {
                    // Adjust the max texture size to 2048x2048 (or another limit if desired)
                    textureImporter.maxTextureSize = maxTextureSize;
                    textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    textureImporter.spritePixelsPerUnit = pixelsPerUnit;

                    // Save the new settings and re-import the texture
                    textureImporter.SaveAndReimport();
                }
            }
        }

        public static void GenerateTiledProjectFile(string productName, string outputPath)
        {
            string tiledProjectName = productName + ".tiled-project";
            // Create the Tiled project file
            string filePath = Path.Combine(outputPath, tiledProjectName);

            // Define the base content for the Tiled project file
            string content = "{\n" +
                             "    \"automappingRulesFile\": \"\",\n" +
                             "    \"commands\": [\n" +
                             "    ],\n" +
                             "    \"extensionsPath\": \"extensions\",\n" +
                             "    \"folders\": [\n" +
                             "        \".\"\n" +
                             "    ]\n" +
                             "}";
            try
            {
                // Write the content to the file
                File.WriteAllText(filePath, content);

                // Notify the user that the file was successfully created
                UnityEngine.Debug.Log($"Tiled project file generated at: {filePath}");
            }
            catch (IOException e)
            {
                // Handle any potential file write errors
                UnityEngine.Debug.LogError($"Failed to generate Tiled project file: {e.Message}");
            }
        }

        private static void AddTilesetsToExistingMaps(string outputPath)
        {
            string mapDirectory = Path.Combine(outputPath, "Tilemaps");
            if (!Directory.Exists(mapDirectory))
            {
                return;
            }

            string[] mapFiles = Directory.GetFiles(mapDirectory, "*.tmx", SearchOption.AllDirectories);
            string tilesetDirectory = Path.Combine(outputPath, "Tilesets");
            string[] tilesetFiles = Directory.GetFiles(tilesetDirectory, "*.tsx", SearchOption.AllDirectories);

            foreach (string mapFile in mapFiles)
            {
                string mapContent = File.ReadAllText(mapFile);
                foreach (string tilesetFile in tilesetFiles)
                {
                    string relativePath = Path.GetRelativePath(Path.GetDirectoryName(mapFile), tilesetFile).Replace("\\", "/");
                    string tilesetReference = $"<tileset firstgid=\"1\" source=\"{relativePath}\"/>";
                    if (!mapContent.Contains(tilesetReference))
                    {
                        mapContent = mapContent.Replace("<layer", $"{tilesetReference}\n<layer");
                    }
                }
                File.WriteAllText(mapFile, mapContent);
            }
        }
    }
}
