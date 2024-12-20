using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System.Linq;
using System.IO.Compression;
using System.IO;
using System.Collections.Generic;

#if SUPER_TILED2UNITY_INSTALLED
using SuperTiled2Unity.Editor;
using SuperTiled2Unity; // Include the SuperTiled2Unity namespaces if available
#endif

//projected workflow :
//0. Make sure SuperTiled2Unity is installed - DONE
//1. Unzip tilesets - DONE
//2. Create Tiled project file - DONE
//3. Create a .tsx file for each tileset - DONE but we still have import errors to fix

namespace FingTools.Tiled{

public class TiledImporterEditorWindow : EditorWindow
{
    private static string superTiled2UnityPackageId = "com.seanba.super-tiled2unity";
    private static string superTiled2UnityGitUrl = "https://github.com/Seanba/SuperTiled2Unity.git?path=/SuperTiled2Unity/Packages/com.seanba.super-tiled2unity";
    private static bool? isSuperTiled2UnityInstalled = null;
    private string selectedInteriorZipFile = null;
    private string selectedExteriorZipFile = null;
    private int selectedSizeIndex = 0; 
    private bool importInterior = true;
    private bool importExterior = true;
    private readonly List<string> validSizes = new () { "16", "32"};
    private string outputPath = "Assets/FingTools/Tiled/";

    [MenuItem("FingTools/Importer/Tilesets Importer",false,99)]
    public static void ShowWindow()
    {
        GetWindow<TiledImporterEditorWindow>(true,"Tilesets Importer");
    }

    private void OnGUI()
{
    if (isSuperTiled2UnityInstalled == null)
    {
        isSuperTiled2UnityInstalled = CheckSuperTiled2Unity();
    }

    EditorGUILayout.LabelField("The Tiled importer requires SuperTiled2Unity in order to work properly", EditorStyles.boldLabel);
    DrawSeparator();

    if (isSuperTiled2UnityInstalled == true)
    {
        EditorGUILayout.LabelField("✅ SuperTiled2Unity is correctly installed", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("You can now import tilesets from the Limezu zip files", EditorStyles.boldLabel);            

        DrawSeparator();

        // Import interior asset selection
        importInterior = EditorGUILayout.Toggle("Import Interior Assets", importInterior);
        if (importInterior)
        {
            // Show button to select interior zip file if selected
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Modern Interior zip file", GUILayout.Width(180)))
            {
                selectedInteriorZipFile = EditorUtility.OpenFilePanel("Select Modern Interior zip file", "", "zip");
            }
            EditorGUILayout.LabelField("Selected Zip File:", selectedInteriorZipFile ?? "None");
            EditorGUILayout.EndHorizontal();
        }

        DrawSeparator();

        // Import exterior asset selection
        importExterior = EditorGUILayout.Toggle("Import Exterior Assets", importExterior);
        if (importExterior)
        {
            // Show button to select exterior zip file if selected
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Modern Exterior zip file", GUILayout.Width(180)))
            {
                selectedExteriorZipFile = EditorUtility.OpenFilePanel("Select Modern Exterior zip file", "", "zip");
            }
            EditorGUILayout.LabelField("Selected Zip File:", selectedExteriorZipFile ?? "None");
            EditorGUILayout.EndHorizontal();
        }

        DrawSeparator();

        // Select the sprite size to import
        EditorGUILayout.LabelField("Select the sprite size to import: (48x48 is not available at the moment)", EditorStyles.wordWrappedLabel);
        selectedSizeIndex = EditorGUILayout.Popup(selectedSizeIndex, validSizes.ToArray());

        DrawSeparator();

        // Import button
        EditorGUI.BeginDisabledGroup(
            !importExterior && !importInterior ||
            (string.IsNullOrEmpty(selectedInteriorZipFile) && !importExterior) || 
            (string.IsNullOrEmpty(selectedExteriorZipFile) && !importInterior) ||
            string.IsNullOrEmpty(selectedExteriorZipFile) && string.IsNullOrEmpty(selectedInteriorZipFile)
            );
        if (GUILayout.Button("Import Assets"))
        {
            EditorUtility.DisplayProgressBar("Importing Tilesets", $"Processing tilesets", 0.5f);  
            #if SUPER_TILED2UNITY_INSTALLED 
            ImportAssets();
            #endif
            GenerateTiledProjectFile();
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();            
            EditorUtility.ClearProgressBar();
        }
        EditorGUI.EndDisabledGroup();
    }
    else if (isSuperTiled2UnityInstalled == false)
    {
        EditorGUILayout.LabelField("🔴 SuperTiled2Unity is not currently installed.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("Click on the button below to add SuperTiled2Unity Package to this Unity project.");

        if (GUILayout.Button("Install SuperTiled2Unity"))
        {
            AddPackage(superTiled2UnityGitUrl);
        }
    }
}
    #if SUPER_TILED2UNITY_INSTALLED
    private void ImportAssets()
    {
        // Unzip assets first
        if (importInterior && !string.IsNullOrEmpty(selectedInteriorZipFile))
        {
            if (!ValidateInteriorZipFile(selectedInteriorZipFile))
            {
                EditorUtility.DisplayDialog("Error", "Invalid Modern Interior zip file. Please select the correct file.", "OK");
                return;
            }
            string interiorOutput = Path.Combine(outputPath, "Art/Interior/");
            UnzipInteriorAssets(selectedInteriorZipFile, validSizes[selectedSizeIndex]);
        }

        if (importExterior && !string.IsNullOrEmpty(selectedExteriorZipFile))
        {
            if (!ValidateExteriorZipFile(selectedExteriorZipFile))
            {
                EditorUtility.DisplayDialog("Error", "Invalid Modern Exterior zip file. Please select the correct file.", "OK");
                return;
            }
            string exteriorOutput = Path.Combine(outputPath, "Art/Exterior/");
            UnzipexteriorAssets(selectedExteriorZipFile, validSizes[selectedSizeIndex]);
        }

        // Adjust texture import settings (resize if necessary)
        if (importInterior && !string.IsNullOrEmpty(selectedInteriorZipFile))
        {
            string interiorOutput = Path.Combine(outputPath, "Art/Interior/");
            AdjustTextureImportSettings(interiorOutput);
        }

        if (importExterior && !string.IsNullOrEmpty(selectedExteriorZipFile))
        {
            string exteriorOutput = Path.Combine(outputPath, "Art/Exterior/");
            AdjustTextureImportSettings(exteriorOutput);
        }

        // After unzipping, now generate the TSX files for each tileset
        if (importInterior && !string.IsNullOrEmpty(selectedInteriorZipFile))
        {
            string interiorOutput = Path.Combine(outputPath, "Art/Interior/");
            GenerateTSXFilesForImportedTilesets(interiorOutput, "Interior", int.Parse(validSizes[selectedSizeIndex]));
            AutoReimportTextures(interiorOutput);
        }

        if (importExterior && !string.IsNullOrEmpty(selectedExteriorZipFile))
        {
            string exteriorOutput = Path.Combine(outputPath, "Art/Exterior/");
            GenerateTSXFilesForImportedTilesets(exteriorOutput, "Exterior", int.Parse(validSizes[selectedSizeIndex]));
            AutoReimportTextures(exteriorOutput);
        }
    }
    #endif
    private static void GenerateTiledProjectFile()
    {
        string tiledProjectName = Application.productName+".tiled-project";
        // Create the Tiled project file
        string filePath = "Assets/FingTools/Tiled/" + tiledProjectName;

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
            Debug.Log($"Tiled project file generated at: {filePath}");
        }
        catch (IOException e)
        {
            // Handle any potential file write errors
            Debug.LogError($"Failed to generate Tiled project file: {e.Message}");
        }
    }
    private void GenerateTSXFilesForImportedTilesets(string tilesetDirectory, string tilesetType, int tileSize)
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
            DestroyImmediate(texture);

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

    private void GenerateTSXFile(string fileName, string tilesetName, string tileSet, int width, int height, int tileSize)
    {
        // Fixed number of columns
        int columns = 32;

        // Calculate tile count based on the image size and tile size
        int tileCount = width * height / (tileSize * tileSize);

        // Define the TSX file content
        string content =
            $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            $"<tileset version=\"1.10\" tiledversion=\"1.11.0\" name=\"{tilesetName}\" " +
            $"tilewidth=\"{tileSize}\" tileheight=\"{tileSize}\" tilecount=\"{tileCount}\" columns=\"{columns}\">\n" +
            $" <image source=\"{tileSet}\" width=\"{width}\" height=\"{height}\"/>\n" +
            $"</tileset>";

        // Define the file path in the Unity project's directory
        string filePath = Path.Combine(Application.dataPath, "..", fileName);

        try
        {
            // Write the content to the file
            File.WriteAllText(filePath, content);

            // Notify the user that the file was successfully created
            Debug.Log($"TSX file generated at: {filePath}");
        }
        catch (IOException e)
        {
            // Handle any potential file write errors
            Debug.LogError($"Failed to generate TSX file: {e.Message}");
        }
    }
    #if SUPER_TILED2UNITY_INSTALLED
    // Method to force the re-import of textures and trigger the missing sprite action
    private void AutoReimportTextures(string tilesetDirectory)
    {
        // Get all PNG files from the tileset directory
        string[] textureFiles = Directory.GetFiles(tilesetDirectory, "*.png", SearchOption.AllDirectories);
        TsxAssetImporter tsxAssetImporter = new TsxAssetImporter();
        
        // Iterate over each texture and force re-import
        foreach (string textureFile in textureFiles)
        {
            string assetPath = textureFile.Replace(Application.dataPath, "").Replace("\\", "/");
            
            // Check if texture exists in the asset database
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                continue; // Skip folders
            }

            // Reimport the texture to trigger the sprite checking process
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            
            // Optionally, you can log the texture files
            Debug.Log($"Reimporting texture: {assetPath}");
        }
    }
    #endif
    private void AdjustTextureImportSettings(string textureDirectory)
    {
        // Get all the PNG files from the texture directory
        string[] textureFiles = Directory.GetFiles(textureDirectory, "*.png", SearchOption.AllDirectories);

        foreach (string textureFile in textureFiles)
        {
            // Load the texture importer for the current texture
            TextureImporter textureImporter = AssetImporter.GetAtPath(textureFile) as TextureImporter;
            
            if (textureImporter != null)
            {
                // Load the texture to get its dimensions
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureFile);

                // Check if the texture exceeds the max size (2048x2048)
                if (texture.width > 2048 || texture.height > 2048)
                {
                    // Adjust the max texture size to 2048x2048 (or another limit if desired)
                    textureImporter.maxTextureSize = 2048;
                    textureImporter.textureCompression = TextureImporterCompression.Uncompressed;  // Optional: adjust compression

                    // Save the new settings and re-import the texture
                    textureImporter.SaveAndReimport();
                    Debug.Log($"Resized texture: {textureFile} to fit within 2048x2048.");
                }
                else
                {
                    Debug.Log($"Texture {textureFile} is already within the max size.");
                }

                // Release texture memory
                DestroyImmediate(texture);
            }
        }
    }

    private void UnzipInteriorAssets(string zipFilePath, string spriteSize)
    {        
        if(!Directory.Exists(outputPath+"/Art/Interior/"))
            Directory.CreateDirectory(outputPath+"/Art/Interior/");
        var archive = ZipFile.OpenRead(zipFilePath);

        //All the assets are inside : 1_Interior/"spriteSize"x"spriteSize"/Theme_Sorter/*.png
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            var fullName = entry.FullName;
            var entryName = entry.Name;
            if (fullName.StartsWith($"1_Interiors/{spriteSize}x{spriteSize}/Theme_Sorter/") && fullName.EndsWith(".png"))
            {
                if(!Directory.Exists(outputPath+"/Art/Interior/"))
                    Directory.CreateDirectory(outputPath+"/Art/Interior/");
                entry.ExtractToFile(outputPath+"/Art/Interior/"+entryName);
            }
        }
    }
    private void UnzipexteriorAssets(string zipFilePath, string spriteSize)
    {        
        if(!Directory.Exists(outputPath+"/Art/Exterior/"))
            Directory.CreateDirectory(outputPath+"/Art/Exterior/");
        var archive = ZipFile.OpenRead(zipFilePath);


        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            var fullName = entry.FullName;
            var entryName = entry.Name;
            //Debug.Log(fullName);
            if (fullName.StartsWith($"Modern_Exteriors_{spriteSize}x{spriteSize}/ME_Theme_Sorter_{spriteSize}x{spriteSize}/") && fullName.EndsWith(".png") && !fullName.Contains("Singles") && !fullName.Contains("Old_Sorting"))
            {
                if(!Directory.Exists(outputPath+"/Art/Exterior/"))
                    Directory.CreateDirectory(outputPath+"/Art/Exterior/");
                Debug.Log(fullName);
                entry.ExtractToFile(outputPath+"/Art/Exterior/"+entryName);
            }
        }
    }

    private bool ValidateInteriorZipFile(string zipFilePath)
    {
        bool output = true;
        if(string.IsNullOrEmpty(zipFilePath))
            output = false;
        //TODO : write specific validation for interior zip file
        return output;
    }

    private bool ValidateExteriorZipFile(string zipFilePath)
    {
        bool output = true;
        if(string.IsNullOrEmpty(zipFilePath))
            output = false;
        //TODO : write specific validation for exterior zip file
        return output;
    }

    //GUI HELPER FUNCTIONS
    private void DrawSeparator()
    {
        GUILayout.Space(5);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
    }
    
    //SUPERTILED2UNITY CHECK ONLY BELOW
    private static bool CheckSuperTiled2Unity()
    {
        // Request the list of installed packages
        ListRequest listRequest = Client.List();

        // Wait for the request to finish
        while (!listRequest.IsCompleted)
        {
            // Optionally, show some loading indicator here
            EditorUtility.DisplayProgressBar("Checking for SuperTiled2Unity", "Please wait...", 0.5f);
        }
        EditorUtility.ClearProgressBar();

        if (listRequest.Status == StatusCode.Success)
        {
            var installedPackages = listRequest.Result;

            // Check if SuperTiled2Unity is installed
            bool packageFound = installedPackages.Any(p => p.name == superTiled2UnityPackageId);

            if (packageFound)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    public static void AddPackage(string packageUrl)
    {
        // Create a request to add the package
        AddRequest addRequest = Client.Add(packageUrl);

        // Wait for the request to complete
        while (!addRequest.IsCompleted)
        {
            // You can optionally display a loading progress bar here
            EditorUtility.DisplayProgressBar("Adding Package", "Please wait while the package is added...", 0.5f);
        }
        EditorUtility.ClearProgressBar();

        // Check the result
        if (addRequest.Status == StatusCode.Success)
        {
            Debug.Log("Package added successfully: " + packageUrl);
        }
        else
        {
            Debug.LogError("Failed to add package: " + packageUrl);
        }
    }
}
}