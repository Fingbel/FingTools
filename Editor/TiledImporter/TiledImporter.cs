using UnityEngine;
using UnityEditor;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
namespace FingTools.Internal
{

    public static class TiledImporter
    {
        private static bool IsTilesetAlreadyImported(string tileset, string type)
        {
            string tilesetPath = Path.Combine("Assets/FingTools/Tiled/", "Tilesets", type, $"{Path.GetFileNameWithoutExtension(tileset)}.tsx");           
            return File.Exists(tilesetPath);
        }
        private static List<string> selectedInteriorTilesets = new ();
        private static List<string> selectedExteriorTilesets = new ();        
        public static void ImportAssets(string selectedInteriorZipFile, List<string> _selectedInteriorTilesets, string selectedExteriorZipFile, List<string> _selectedExteriorTilesets, string outputPath, int selectedSizeIndex, List<string> validSizes)
        {
            GenerateTiledProjectFile(outputPath);
            selectedInteriorTilesets = _selectedInteriorTilesets.Where(tileset => !IsTilesetAlreadyImported(tileset, "Interior")).ToList();
            selectedExteriorTilesets = _selectedExteriorTilesets.Where(tileset => !IsTilesetAlreadyImported(tileset, "Exterior")).ToList();

            // Start the coroutine process for importing assets
            EditorCoroutineUtility.StartCoroutineOwnerless(ImportAssetsCoroutine(selectedInteriorZipFile, selectedInteriorTilesets, selectedExteriorZipFile, selectedExteriorTilesets, outputPath, selectedSizeIndex, validSizes));
        }

        private static IEnumerator ImportAssetsCoroutine(string selectedInteriorZipFile, List<string> selectedInteriorTilesets, string selectedExteriorZipFile, List<string> selectedExteriorTilesets, string outputPath, int selectedSizeIndex, List<string> validSizes)
        {            
            // Step 1: Unzip assets
            if (!string.IsNullOrEmpty(selectedInteriorZipFile))
            {
                if (!FingHelper.ValidateInteriorZipFile(selectedInteriorZipFile))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid Modern Interior zip file. Please select the correct file.", "OK");
                    yield break;
                }
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(UnzipInteriorAssetsCoroutine(selectedInteriorZipFile, selectedInteriorTilesets, outputPath, validSizes[selectedSizeIndex]));
            }

            if (!string.IsNullOrEmpty(selectedExteriorZipFile))
            {
                if (!FingHelper.ValidateExteriorZipFile(selectedExteriorZipFile))
                {
                    EditorUtility.DisplayDialog("Error", "Invalid Modern Exterior zip file. Please select the correct file.", "OK");
                    yield break;
                }
                yield return EditorCoroutineUtility.StartCoroutineOwnerless(UnzipExteriorAssetsCoroutine(selectedExteriorZipFile, selectedExteriorTilesets, outputPath, validSizes[selectedSizeIndex]));
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Step 2: Adjust settings and generate TSX files
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(AdjustAndGenerateTSXFiles(selectedInteriorTilesets, outputPath, selectedSizeIndex, validSizes, "Interior"));
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(AdjustAndGenerateTSXFiles(selectedExteriorTilesets, outputPath, selectedSizeIndex, validSizes, "Exterior"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Step 3: Add tilesets to existing maps
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(AddTilesetsToExistingMapsCoroutine(outputPath,selectedInteriorTilesets,true));
            yield return EditorCoroutineUtility.StartCoroutineOwnerless(AddTilesetsToExistingMapsCoroutine(outputPath,selectedExteriorTilesets,false));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Coroutine for unzipping interior assets
        private static IEnumerator UnzipInteriorAssetsCoroutine(string selectedInteriorZipFile, List<string> interiorTilesets, string outputPath, string selectedSize)
        {
            UnzipInteriorAssets(selectedInteriorZipFile, selectedSize, interiorTilesets, outputPath, int.Parse(selectedSize));
            yield return null; // Allow Editor to process events
        }

        // Coroutine for unzipping exterior assets
        private static IEnumerator UnzipExteriorAssetsCoroutine(string selectedExteriorZipFile, List<string> exteriorTilesets, string outputPath, string selectedSize)
        {
            UnzipExteriorAssets(selectedExteriorZipFile, selectedSize, exteriorTilesets, outputPath, int.Parse(selectedSize));
            yield return null; // Allow Editor to process events
        }

        // Coroutine for adjusting settings and generating TSX files
        private static IEnumerator AdjustAndGenerateTSXFiles(List<string> selectedTilesets, string outputPath, int selectedSizeIndex, List<string> validSizes, string type)
        {
            string artOutput = Path.Combine(outputPath, "Art", type); // "Interior" or "Exterior"
            string tilesetOutputPath = Path.Combine(outputPath, "Tilesets", type); // "Interior" or "Exterior"
            
            foreach (var tileset in selectedTilesets)
            {
                string tilesetPath = Path.Combine(artOutput, tileset); // Path to the specific tileset
                AdjustTextureImportSettings(tilesetPath, type == "Interior" ? 2048 : 4096, int.Parse(validSizes[selectedSizeIndex]));
            }

            // Generate TSX files after adjusting the textures
            GenerateTSXFilesForImportedTilesets(artOutput, type, int.Parse(validSizes[selectedSizeIndex]), outputPath);            
            yield return null;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            yield return null;

            yield return AutoFixTexturesCoroutine(tilesetOutputPath);

            foreach (var tileset in selectedTilesets)
            {                
                yield return UpdatePixerlPerUnitCoroutine(tileset, tilesetOutputPath,int.Parse(validSizes[selectedSizeIndex]));
            }
        }

        private static IEnumerator AutoFixTexturesCoroutine(string tilesetOutputPath)
        {
            ST2ULinker.AutoFixTextures(tilesetOutputPath);            
            yield return null;
        }

        private static IEnumerator UpdatePixerlPerUnitCoroutine(string tileset,string tilesetOutputPath,int pixelsPerUnit)
        {           
            var tilesetName = Path.GetFileNameWithoutExtension(tileset);
            string assetPath = Path.Combine(tilesetOutputPath, tilesetName + ".tsx");
            UpdatePixelsPerUnit(assetPath, pixelsPerUnit);
            yield return null;
            
        }
        // Coroutine for adding tilesets to existing maps
        private static IEnumerator AddTilesetsToExistingMapsCoroutine(string outputPath,List<string> selectedTilesets,bool isInterior)
        {
            AddTilesetsToExistingMaps(outputPath,selectedTilesets,isInterior);
            yield return null;
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
                        entry.ExtractToFile(outputPath + "/Art/Interior/" + entryName, false);

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
                        entry.ExtractToFile(outputPath + "/Art/Exterior/" + entryName, false);
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
                string fileName = Path.GetFileName(filePath);
                // Check if the file is part of the selected interior or exterior tilesets
                if (!(selectedExteriorTilesets.Contains(fileName) || selectedInteriorTilesets.Contains(fileName)))
                {
                    continue;  // Skip files that are not selected
                }
                
                // Generate the TSX file
                string tsxFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.tsx";
                string tsxFilePath = Path.Combine(tsxOutputPath, tsxFileName);
                if (File.Exists(tsxFilePath))
                {
                    continue;
                }
                // Get the dimensions of the tileset image
                Texture2D texture = new Texture2D(2, 2);
                byte[] imageData = File.ReadAllBytes(filePath);
                texture.LoadImage(imageData);

                int width = texture.width;
                int height = texture.height;

                // Release memory used by the texture
                Object.DestroyImmediate(texture);

                // Generate the TSX file
                ST2ULinker.GenerateTSXFile(
                    tsxFilePath,
                    fileName, // Tileset name is the file name without extension
                    filePath.Replace(outputPath, "../../"), // Relative path for TSX file
                    width,
                    height,
                    tileSize
                );
            }
        }

        
        public static void UpdatePixelsPerUnit(string assetPath,int pixelsPerUnit)
        {           
            // Get the generic importer for the .tsx file          
            assetPath = assetPath.Replace("\\","/");
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
                
        public static void AdjustTextureImportSettings(string textureFile, int maxTextureSize, int pixelsPerUnit)
        {
            // Check if the file is part of the selected tileset (either Interior or Exterior)
            if (!selectedExteriorTilesets.Contains(Path.GetFileName(textureFile)) 
                && !selectedInteriorTilesets.Contains(Path.GetFileName(textureFile)))
            {
                return; // Skip if it's not a selected tileset
            }

            // Special case for textures containing "5_Floor"
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

        //Create the Tiled project file if it doesn't exist already
        public static void GenerateTiledProjectFile(string outputPath)
        {
            string projectFilePath = Path.Combine(outputPath,"TiledProject.tiled-project");
            string sessionFilePath = Path.Combine(outputPath,"TiledProject.tiled-session");
            if(!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);
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

        private static void AddTilesetsToExistingMaps(string outputPath, List<string> selectedTilesets, bool isInterior)
        {
            string mapDirectory = Path.Combine(outputPath, "Tilemaps");
            if (!Directory.Exists(mapDirectory))
            {
                return;
            }
            string artType = isInterior ? "Interior" : "Exterior";
            string[] mapFiles = Directory.GetFiles(mapDirectory, "*.tmx", SearchOption.AllDirectories);
            string tilesetDirectory = Path.Combine(outputPath, "Tilesets", artType);

            // Iterate through the map files
            foreach (string mapFile in mapFiles)
            {
                string mapContent = File.ReadAllText(mapFile);
                int firstGid = GetLastFirstGid(mapContent, Path.GetDirectoryName(mapFile)); // Correct firstGid calculation

                // Iterate through the selected tilesets
                foreach (string selectedTileset in selectedTilesets)
                {
                    // Build the corresponding .tsx filename by changing the .png extension to .tsx
                    string tilesetFileName = Path.GetFileNameWithoutExtension(selectedTileset) + ".tsx";
                    string tilesetFile = Path.Combine(tilesetDirectory, tilesetFileName).Replace("\\", "/");

                    // Ensure the tileset file exists
                    if (File.Exists(tilesetFile))
                    {
                        string relativePath = Path.GetRelativePath(Path.GetDirectoryName(mapFile), tilesetFile).Replace("\\", "/");
                        int tileCount = GetTileCountFromTileset(tilesetFile);

                        // Create the tileset reference string
                        string tilesetReference = $"<tileset firstgid=\"{firstGid}\" source=\"{relativePath}\"/>";

                        // Add the tileset reference to the map if it's not already included
                        if (!mapContent.Contains(tilesetReference))
                        {
                            mapContent = mapContent.Replace("</map>", $"{tilesetReference}\n</map>");
                        }

                        // Update the first GID for the next tileset
                        firstGid += tileCount;
                    }
                    else
                    {
                        Debug.LogWarning($"Tileset file not found: {tilesetFile}");
                    }
                }

                // Write the updated map content back to the file
                File.WriteAllText(mapFile, mapContent);
            }
        }

        private static int GetLastFirstGid(string mapContent, string mapDirectory)
        {
            int lastFirstGid = 1;
            const string tilesetTag = "<tileset firstgid=\"";
            int startIndex = mapContent.LastIndexOf(tilesetTag);
            
            if (startIndex != -1)
            {
                // Extract the firstgid of the last tileset
                startIndex += tilesetTag.Length;
                int endIndex = mapContent.IndexOf("\"", startIndex);
                if (endIndex > startIndex)
                {
                    string firstGidValue = mapContent.Substring(startIndex, endIndex - startIndex);
                    if (int.TryParse(firstGidValue, out lastFirstGid))
                    {
                        // Extract the source attribute to locate the .tsx file
                        const string sourceTag = "source=\"";
                        int sourceStartIndex = mapContent.IndexOf(sourceTag, endIndex) + sourceTag.Length;
                        int sourceEndIndex = mapContent.IndexOf("\"", sourceStartIndex);

                        if (sourceStartIndex > sourceTag.Length && sourceEndIndex > sourceStartIndex)
                        {
                            string relativeTilesetPath = mapContent.Substring(sourceStartIndex, sourceEndIndex - sourceStartIndex);
                            string absoluteTilesetPath = Path.Combine(mapDirectory, relativeTilesetPath).Replace("\\", "/");

                            // Ensure the .tsx file exists and get its tilecount
                            if (File.Exists(absoluteTilesetPath))
                            {
                                int tileCount = GetTileCountFromTileset(absoluteTilesetPath);
                                lastFirstGid += tileCount; // Increment by tile count to avoid overlap
                            }
                        }
                    }
                }
            }

            return lastFirstGid; // Return the correct starting firstgid
        }

        private static int GetTileCountFromTileset(string tilesetFile)
        {
            string content = File.ReadAllText(tilesetFile);
            int tileCount = 0;
            const string tileCountString = "tilecount=\"";
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