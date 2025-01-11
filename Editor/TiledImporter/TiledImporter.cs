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
#if UNITY_EDITOR
namespace FingTools.Tiled
{

    public static class TiledImporter
    {
        private static List<string> selectedInteriorTilesets = new ();
        private static List<string> selectedExteriorTilesets = new ();
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
        public static void ImportAssets(string selectedInteriorZipFile, List<string> _selectedInteriorTilesets,string selectedExteriorZipFile, List<string> _selectedExteriorTilesets, string outputPath, int selectedSizeIndex, List<string> validSizes)
        {
            #if SUPER_TILED2UNITY_INSTALLED
            selectedExteriorTilesets = _selectedExteriorTilesets;
            selectedInteriorTilesets = _selectedInteriorTilesets;
            if (!string.IsNullOrEmpty(selectedInteriorZipFile))
            {
                if (!ValidateInteriorZipFile(selectedInteriorZipFile))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid Modern Interior zip file. Please select the correct file.", "OK");
                    return;
                }
                UnzipInteriorAssets(selectedInteriorZipFile, validSizes[selectedSizeIndex], _selectedInteriorTilesets, outputPath,validSizes[selectedSizeIndex].ToInt());
            }
            if (!string.IsNullOrEmpty(selectedExteriorZipFile))
            {
                if (!ValidateExteriorZipFile(selectedExteriorZipFile))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid Modern Exterior zip file. Please select the correct file.", "OK");
                    return;
                }
                UnzipExteriorAssets(selectedExteriorZipFile, validSizes[selectedSizeIndex], _selectedExteriorTilesets, outputPath,validSizes[selectedSizeIndex].ToInt());
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // After unzipping, now generate the TSX files for each tileset
            if (!string.IsNullOrEmpty(selectedInteriorZipFile))
            {
                string interiorArtOutput = Path.Combine(outputPath, "Art/Interior/");
                string interiorTilesetOutputPath = Path.Combine(outputPath, "Tilesets/Interior");
                AdjustTextureImportSettings(interiorArtOutput, 2048,validSizes[selectedSizeIndex].ToInt());
                GenerateTSXFilesForImportedTilesets(interiorArtOutput, "Interior", int.Parse(validSizes[selectedSizeIndex]), outputPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorApplication.delayCall += () => AutoFixTextures(interiorTilesetOutputPath);
                EditorApplication.delayCall += () =>
                {
                    foreach(var tileset in _selectedInteriorTilesets)
                    {
                        var tilesetName = Path.GetFileNameWithoutExtension(tileset);
                        string assetPath = interiorTilesetOutputPath + "/" + tilesetName + ".tsx";
                        UpdatePixelsPerUnit(assetPath, validSizes[selectedSizeIndex].ToInt());
                    }
                };
            }

            if (!string.IsNullOrEmpty(selectedExteriorZipFile))
            {
                string exteriorArtOutput = Path.Combine(outputPath, "Art/Exterior/");
                string exteriorTilesetOutputPath = Path.Combine(outputPath, "Tilesets/Exterior");
                AdjustTextureImportSettings(exteriorArtOutput, 4096,validSizes[selectedSizeIndex].ToInt());
                GenerateTSXFilesForImportedTilesets(exteriorArtOutput, "Exterior", int.Parse(validSizes[selectedSizeIndex]), outputPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorApplication.delayCall += () =>AutoFixTextures(exteriorTilesetOutputPath);
                EditorApplication.delayCall += () =>
                {
                    foreach(var tileset in _selectedExteriorTilesets)
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

            
            foreach (string filePath in tilesetFiles)
            {              
               if(!selectedExteriorTilesets.Contains(Path.GetFileName(filePath))
                && !selectedInteriorTilesets.Contains(Path.GetFileName(filePath)))
                {
                    continue;
                }
               
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
            // Get the generic importer for the .tsx file
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null)
            {
                Debug.LogError($"No importer found for the asset at path: {assetPath}");
                return;
            }

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
            string[] textureFiles = Directory.GetFiles(textureDirectory, "*.png", SearchOption.TopDirectoryOnly);
            foreach (string textureFile in textureFiles)
            {                              
                if(!selectedExteriorTilesets.Contains(Path.GetFileName(textureFile))
                && !selectedInteriorTilesets.Contains(Path.GetFileName(textureFile)))
                {
                    continue;
                }
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

        //Create the Tiled project file if it doesn't exist already
        public static void GenerateTiledProjectFile(string outputPath)
        {
            string projectName = "TiledProject";
            string projectFilePath = Path.Combine(outputPath, projectName + ".tiled-project");
            string sessionFilePath = Path.Combine(outputPath, projectName + ".tiled-session");

            if (!File.Exists(projectFilePath))
            {
                // Create the Tiled project file
                string projectContent = "{\n" +
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
                    // Write the content to the project file
                    File.WriteAllText(projectFilePath, projectContent);

                    // Notify the user that the file was successfully created
                    UnityEngine.Debug.Log($"Tiled project file generated at: {projectFilePath}");
                }
                catch (IOException e)
                {
                    // Handle any potential file write errors
                    UnityEngine.Debug.LogError($"Failed to generate Tiled project file: {e.Message}");
                }
            }

            if (!File.Exists(sessionFilePath))
            {
                // Create the Tiled session file
                string sessionContent = "{\n" +
                                        "    \"activeFile\": \"\",\n" +
                                        "    \"expandedProjectPaths\": [\n" +
                                        "    ],\n" +
                                        "    \"fileStates\": {\n" +
                                        "    },\n" +
                                        "    \"openFiles\": [\n" +
                                        "    ],\n" +
                                        "    \"project\": \"TiledProject.tiled-project\",\n" +
                                        "    \"recentFiles\": [\n" +
                                        "    ]\n" +
                                        "}";
                try
                {
                    // Write the content to the session file
                    File.WriteAllText(sessionFilePath, sessionContent);

                    // Notify the user that the file was successfully created
                    UnityEngine.Debug.Log($"Tiled session file generated at: {sessionFilePath}");
                }
                catch (IOException e)
                {
                    // Handle any potential file write errors
                    UnityEngine.Debug.LogError($"Failed to generate Tiled session file: {e.Message}");
                }
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
                int firstGid = GetLastFirstGid(mapContent);

                foreach (string tilesetFile in tilesetFiles)
                {
                    string relativePath = Path.GetRelativePath(Path.GetDirectoryName(mapFile), tilesetFile).Replace("\\", "/");
                    int tileCount = GetTileCountFromTileset(tilesetFile);
                    string tilesetReference = $"<tileset firstgid=\"{firstGid}\" source=\"{relativePath}\"/>";
                    if (!mapContent.Contains(tilesetReference))
                    {
                        mapContent = mapContent.Replace("</map>", $"{tilesetReference}\n</map>");
                    }
                    firstGid += tileCount;
                }
                File.WriteAllText(mapFile, mapContent);
            }
        }

        private static int GetLastFirstGid(string mapContent)
        {
            int lastFirstGid = 1;
            string firstGidString = "firstgid=\"";
            int startIndex = mapContent.LastIndexOf(firstGidString);
            if (startIndex != -1)
            {
                startIndex += firstGidString.Length;
                int endIndex = mapContent.IndexOf("\"", startIndex);
                if (endIndex > startIndex)
                {
                    string firstGidValue = mapContent.Substring(startIndex, endIndex - startIndex);
                    int.TryParse(firstGidValue, out lastFirstGid);
                    lastFirstGid++;
                }
            }
            return lastFirstGid;
        }

        private static int GetTileCountFromTileset(string tilesetFile)
        {
            string content = File.ReadAllText(tilesetFile);
            int tileCount = 0;
            string tileCountString = "tilecount=\"";
            int startIndex = content.IndexOf(tileCountString) + tileCountString.Length;
            if (startIndex > tileCountString.Length)
            {
                int endIndex = content.IndexOf("\"", startIndex);
                if (endIndex > startIndex)
                {
                    string tileCountValue = content.Substring(startIndex, endIndex - startIndex);
                    int.TryParse(tileCountValue, out tileCount);
                }
            }
            return tileCount;
        }
    }

}
#endif