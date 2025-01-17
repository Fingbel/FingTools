using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

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
        //We need to :
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
        if (!Directory.Exists(sourcePath))
        {
            Debug.LogWarning($"Source path does not exist: {sourcePath}");
            return;
        }
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }        
        string[] partFolders = Directory.GetDirectories(sourcePath);
        foreach (var folder in partFolders)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                string error = AssetDatabase.MoveAsset(folder, targetPath);
                if(string.IsNullOrEmpty(error))
                {
                    Debug.Log(error);
                }
            }
            else
            {
                Debug.LogWarning($"Folder not found or invalid: {folder}");
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
