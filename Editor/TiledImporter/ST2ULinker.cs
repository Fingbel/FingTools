using System.IO;
using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace FingTools.Internal{
public static class ST2ULinker
{
    private static string superTiled2UnityPackageId = "com.seanba.super-tiled2unity";
    private static string superTiled2UnityGitUrl = "https://github.com/Seanba/SuperTiled2Unity.git?path=/SuperTiled2Unity/Packages/com.seanba.super-tiled2unity";
    public static bool? isSuperTiled2UnityInstalled{get;private set;}
        
    internal static void GenerateTSXFile(string fileName, string tilesetName, string tileSet, int width, int height, int tileSize)
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
    internal static void AutoFixTextures(string tilesetPath)
    {
        #if SUPER_TILED2UNITY_INSTALLED
        // Get all .tsx files in the specified directory
        string[] tsxFiles = Directory.GetFiles(tilesetPath, "*.tsx", SearchOption.TopDirectoryOnly);
        AssetDatabase.StartAssetEditing();
        // Iterate over the .tsx files and attempt to find ImportErrors
        foreach (string tsxFile in tsxFiles)
        {                
            SuperTiled2Unity.ImportErrors importErrors = AssetDatabase.LoadAssetAtPath(tsxFile, typeof(SuperTiled2Unity.ImportErrors)) as SuperTiled2Unity.ImportErrors;                
            if (importErrors != null)
            {
                // Iterate over the missing sprites in the ImportErrors and handle them
                foreach (SuperTiled2Unity.ImportErrors.MissingTileSprites missingTileSprite in importErrors.m_MissingTileSprites)
                {
                    // Call method to add missing sprites
                    CallAddSpritesToTexture(missingTileSprite.m_TextureAssetPath, missingTileSprite.m_MissingSprites.Select(m => m.m_Rect));
                }                    
            }
        }
        AssetDatabase.StopAssetEditing();
        #endif
    }        
    
    internal static void CallAddSpritesToTexture(string textureAssetPath, IEnumerable<Rect> missingSpritesRects)
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
    internal static bool CheckSuperTiled2Unity()
    {
        // Request the list of installed packages
        ListRequest listRequest = Client.List();

        // Wait for the request to finish
        while (!listRequest.IsCompleted)
        {
        }
        if (listRequest.Status == StatusCode.Success)
        {
            var installedPackages = listRequest.Result;

            // Check if SuperTiled2Unity is installed
            bool packageFound = installedPackages.Any(p => p.name == superTiled2UnityPackageId);

            if (packageFound)
            {
                isSuperTiled2UnityInstalled = true;
                return true;
            }
            else
            {
                isSuperTiled2UnityInstalled = false;
                return false;
            }
        }
        return false;
    }

    internal static void AddPackage()
    {
        // Create a request to add the package
        AddRequest addRequest = Client.Add(superTiled2UnityGitUrl);

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
            UnityEngine.Debug.Log("Package added successfully: " + superTiled2UnityGitUrl);
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to add package: " + superTiled2UnityGitUrl);
        }

    }
}
}

#endif