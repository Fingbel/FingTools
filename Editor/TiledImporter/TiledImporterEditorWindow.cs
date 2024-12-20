using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using System.Linq;
using System.IO.Compression;
using System.IO;
using System.Collections.Generic;

#if SUPER_TILED2UNITY_INSTALLED
using SuperTiled2Unity; // Include the SuperTiled2Unity namespaces if available
#endif

//projected workflow :
//0. Make sure SuperTiled2Unity is installed
//1. Unzip tilesets
//2. Create Tiled project file 
//3. Create a .tmx file for each tileset
//This is the correct git link to retrieve SuperTiled2Unity directly from the package manager
//https://github.com/Seanba/SuperTiled2Unity.git?path=/SuperTiled2Unity/Packages/com.seanba.super-tiled2unity

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
    private string outputPath = "Assets/FingTools/Tiled/Art/";

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
            ImportAssets();
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

    private void ImportAssets()
    {
        if(importInterior && !string.IsNullOrEmpty(selectedInteriorZipFile))
        {
            if(!ValidateInteriorZipFile(selectedInteriorZipFile)) 
            {
                // Show an error message in a display box
                EditorUtility.DisplayDialog("Error", "Invalid Modern Interior zip file. Please select the correct file.", "OK");
                return;
            }
            UnzipInteriorAssets(selectedInteriorZipFile,validSizes[selectedSizeIndex]);
        }
        if(importExterior && !string.IsNullOrEmpty(selectedExteriorZipFile))
        {
            if(!ValidateExteriorZipFile(selectedExteriorZipFile)) 
            {
                // Show an error message in a display box
                EditorUtility.DisplayDialog("Error", "Invalid Modern Exterior zip file. Please select the correct file.", "OK");
                return;
            }
            UnzipexteriorAssets(selectedExteriorZipFile,validSizes[selectedSizeIndex]);
        }

    }

    private void UnzipInteriorAssets(string zipFilePath, string spriteSize)
    {        
        if(!Directory.Exists(outputPath+"/Interior/"))
            Directory.CreateDirectory(outputPath+"/Interior/");
        var archive = ZipFile.OpenRead(zipFilePath);

        //All the assets are inside : 1_Interior/"spriteSize"x"spriteSize"/Theme_Sorter/*.png
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            var fileName = entry.FullName;
            //Debug.Log(fileName);
            if (entry.FullName.StartsWith($"1_Interior/{spriteSize}x{spriteSize}/Theme_Sorter/") && entry.FullName.EndsWith(".png"))
            {
                Debug.Log(fileName);
            }
        }
    }
    private void UnzipexteriorAssets(string zipFilePath, string spriteSize)
    {        
        if(!Directory.Exists(outputPath+"/Exterior/"))
            Directory.CreateDirectory(outputPath+"/Exterior/");
        var archive = ZipFile.OpenRead(zipFilePath);


        foreach (ZipArchiveEntry entry in archive.Entries)
        {

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