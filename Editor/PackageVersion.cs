using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public static class PackageVersion
{
    private const string VersionFilePath = "Assets/FingTools/Config/package_version.json";
    private const string CurrentVersion = "1.1.0";  // Update this for each release

    static PackageVersion()
    {
        CheckForVersionUpdate();
    }

    private static void CheckForVersionUpdate()
    {
        if (!File.Exists(VersionFilePath))
        {
            HandleFirstMigration();
        }
        else
        {
            HandleVersionComparison();
        }
    }

    [MenuItem("FingTools/Force Migration", false, 1)]
    private static void HandleFirstMigration()
    {
        Debug.LogWarning("No version file found. Assuming version 1.0.0. Running migration to 1.1.0");

        if (EditorUtility.DisplayDialog(
            "Initial Package Migration",
            "It seems you're updating from version 1.0.0 to 1.1.0. Would you like to migrate your data?",
            "Migrate",
            "Skip"))
        {
            RunMigration("1.0.0", CurrentVersion);
        }

        CreateVersionFile(CurrentVersion);
    }

    private static void HandleVersionComparison()
    {
        string json = File.ReadAllText(VersionFilePath);
        PackageVersionData versionData = JsonUtility.FromJson<PackageVersionData>(json);

        if (versionData.version != CurrentVersion)
        {
            if (EditorUtility.DisplayDialog(
                "Package Update Detected",
                $"The package was updated from version {versionData.version} to {CurrentVersion}. Would you like to migrate your data?",
                "Migrate",
                "Ignore"))
            {
                RunMigration(versionData.version, CurrentVersion);
            }

            CreateVersionFile(CurrentVersion);
        }
    }

    private static void RunMigration(string oldVersion, string newVersion)
    {
        AssetDatabase.StartAssetEditing();
        Debug.Log($"Migrating from version {oldVersion} to {newVersion}...");
        
        // 1. Rename the Sprites folder to CharacterSprites
        RenameFolder("Assets/Resources/FingTools/Sprites", "Assets/Resources/FingTools/CharacterSprites");

        // 2. Move ScriptableObjects to ScriptableObjects/CharacterParts
        MoveFolder("Assets/Resources/FingTools/ScriptableObjects", "Assets/Resources/FingTools/ScriptableObjects/CharacterParts");

        // 3. Move SpriteLibrairies to SpriteLibrairies/CharacterParts
        MoveFolder("Assets/Resources/FingTools/SpriteLibraries", "Assets/Resources/FingTools/SpriteLibrairies/CharacterParts");
        AssetDatabase.StopAssetEditing();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Migration completed successfully.");
    }
    private static void RenameFolder(string oldPath, string newPath)
    {
        if(!Directory.Exists(oldPath)) return;
        if (AssetDatabase.IsValidFolder(oldPath))
        {
            AssetDatabase.MoveAsset(oldPath, newPath);
            Debug.Log($"Renamed folder from {oldPath} to {newPath}");
        }
        else
        {
            Debug.LogWarning($"Folder not found: {oldPath}");
        }
    }

    private static void MoveFolder(string sourcePath, string targetPath)
    {
        List<string> folders = new();
        if (!Directory.Exists(sourcePath))
        {
            Debug.LogWarning($"Source path does not exist: {sourcePath}");
            return;
        }            
        string[] partFolders = Directory.GetDirectories(sourcePath);
        List<string> oldFolders = new();
        foreach (var folder in partFolders)
        {
            oldFolders.Add(folder);
            folder.Replace("\\","/");
            folders.Add(folder);            
        }
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }                    
        foreach(var folder in folders)
        {
            if (Path.GetDirectoryName(folder) == "CharacterParts") continue;
            string targetFolderPath = Path.Combine(targetPath, Path.GetFileName(folder));
            if (!AssetDatabase.IsValidFolder(targetFolderPath))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(targetFolderPath), Path.GetFileName(targetFolderPath));
            }
            EditorApplication.delayCall += () =>
            {
                MoveFiles(folder, targetFolderPath);                
            };
        }
        
        foreach(var oldFolder in oldFolders)
        {
            EditorApplication.delayCall += () =>{DeleteEmptyFolders(sourcePath,oldFolder);};
        }                
    }

    private static void DeleteEmptyFolders(string sourcePath,string oldFolder)
    {
        if (Directory.GetFiles(sourcePath,"*.asset").Length == 0)
        {            
            if(sourcePath == "Assets/Resources/FingTools/SpriteLibraries")
            {
                Directory.Delete(sourcePath,true);
                File.Delete(sourcePath+".meta");
            }            
        }
        if(!oldFolder.Contains("SpriteLibraries"))
        {
            Directory.Delete(oldFolder,true);
        }
        
    }

    private static void MoveFiles(string folder, string targetFolderPath)
    {
        string[] files = Directory.GetFiles(folder, "*.asset", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            Debug.Log(file);
            string error = AssetDatabase.MoveAsset(file, Path.Combine(targetFolderPath, Path.GetFileName(file)));
            if (!string.IsNullOrEmpty(error))
            {
                Debug.Log(error);
            }
        }
        
    }

    private static void CreateVersionFile(string version)
    {
        PackageVersionData newVersionData = new PackageVersionData
        {
            version = version,
            lastUpdated = System.DateTime.Now.ToString("yyyy-MM-dd")
        };

        string json = JsonUtility.ToJson(newVersionData, true);
        Directory.CreateDirectory(Path.GetDirectoryName(VersionFilePath));
        File.WriteAllText(VersionFilePath, json);
        AssetDatabase.Refresh();
    }

    [System.Serializable]
    private class PackageVersionData
    {
        public string version;
        public string lastUpdated;
    }
}
