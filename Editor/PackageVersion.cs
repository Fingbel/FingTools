using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using FingTools.Internal;

[InitializeOnLoad]
public static class PackageVersion
{
    private const string VersionFilePath = "Assets/FingTools/Config/package_version.json";
    private const string CurrentVersion = "1.1.0";
    private static List<string> oldFolders;
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
            VersionComparison();
        }
    }
    #if FINGDEBUG
    [MenuItem("FingTools/DEBUG/Force Migration", false, 999)]
    #endif    
    private static void HandleFirstMigration()
    {               
        bool oldSpriteDir = Directory.Exists("Assets/Resources/FingTools/Sprites");
        bool oldLibr = Directory.Exists("Assets/Resources/FingTools/SpritesLibraries");
        if(oldSpriteDir || oldLibr )
        {
            //Old structure detected, we need to run the migration process            
            if (EditorUtility.DisplayDialog(
                "Package Update",
                "It seems you're updating from version 1.0.0 to 1.1.0. The folder structure has been updated and need a reorganization. Please click below to start the migration process",
                "Migrate"))
            {
                RunMigration("1.0.0", CurrentVersion);
            }
        }        
        CreateVersionFile(CurrentVersion);
    }

    private static void VersionComparison()
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
        oldFolders = new();
        Debug.Log($"Migrating from version {oldVersion} to {newVersion}...");

        // 1. Rename the Sprites folder to CharacterSprites
        MoveFolder("Assets/Resources/FingTools/Sprites", "Assets/Resources/FingTools/Sprites/CharacterSprites");

        // 2. Move ScriptableObjects to ScriptableObjects/CharacterParts
        MoveFolder("Assets/Resources/FingTools/ScriptableObjects", "Assets/Resources/FingTools/ScriptableObjects/CharacterParts");

        // 3. Move SpriteLibrairies to SpriteLibrairies/CharacterParts
        MoveFolder("Assets/Resources/FingTools/SpriteLibraries", "Assets/Resources/FingTools/SpriteLibrairies/CharacterParts");
        AssetDatabase.Refresh();
        
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
        foreach (var folder in partFolders)
        {
            folder.Replace("\\","/");

            oldFolders.Add(folder);
            folders.Add(folder);            
        }
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }      
        EditorApplication.delayCall += ()=>{              
        foreach(var folder in folders)
        {
            if (Path.GetDirectoryName(folder) == "CharacterParts") continue;
            string targetFolderPath = Path.Combine(targetPath, Path.GetFileName(folder));
            if (!AssetDatabase.IsValidFolder(targetFolderPath))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(targetFolderPath), Path.GetFileName(targetFolderPath));
            }
            MoveFiles(folder, targetFolderPath);            
        }                
        EditorApplication.delayCall += () =>
        {
            foreach(var oldFolder in oldFolders)
            {
                DeleteEmptyFolders( oldFolder);                
            }   
            DeleteEmptyFolders("Assets/Resources/FingTools/SpriteLibraries");        
            oldFolders.Clear();                        
            CharacterImporter.LinkCharAssets();
            AssetDatabase.Refresh();
        };
        };                            
    }

    private static void DeleteEmptyFolders(string folder)
    {
        if(Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            File.Delete(folder + ".meta");
        }         
    }

    private static void MoveFiles(string folder, string targetFolderPath)
    {
        string[] files = Directory.GetFiles(folder, "*.asset", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            files = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);
        }
        foreach (var file in files)
        {
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
