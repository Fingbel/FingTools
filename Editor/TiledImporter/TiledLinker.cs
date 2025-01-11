using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class TiledLinker
{
    private const string TiledPathKey = "TiledExecutablePath";

    public static void CheckForTiled()
    {
        string savedPath = EditorPrefs.GetString(TiledPathKey, string.Empty);
        if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath) && IsValidTiledExecutable(savedPath))
        {
            return;
        }

        string[] commonPaths =
        {
            @"C:\Program Files\Tiled\tiled.exe",
            @"C:\Program Files (x86)\Tiled\tiled.exe",
            @"C:\Users\" + System.Environment.UserName + @"\AppData\Local\Tiled\tiled.exe",
            @"/usr/local/bin/tiled", // macOS or Linux paths
            @"/Applications/Tiled.app/Contents/MacOS/Tiled" // macOS bundle executable
        };

        bool isTiledInstalled = false;
        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                isTiledInstalled = true;
                Debug.Log($"Tiled found at: {path}");
                SaveTiledPath(path);
                break;
            }
        }

        if (!isTiledInstalled)
        {
            Debug.LogWarning("Tiled is not installed or could not be found in common paths.");
            PromptUserForTiledPath();
        }
    }

    [MenuItem("FingTools/Open Tiled", true)]
    public static bool ValidateOpenTiled()
    {
        string projectPath = Path.Combine(Application.dataPath, "FingTools", "Tiled", $"TiledProject.tiled-project");
        return File.Exists(projectPath);
    }

    [MenuItem("FingTools/Open Tiled", false, 2)]
    public static void OpenTiled()
    {
        CheckForTiled();
        string savedPath = EditorPrefs.GetString(TiledPathKey, string.Empty);
        if (!string.IsNullOrEmpty(savedPath) && File.Exists(savedPath) && IsValidTiledExecutable(savedPath))
        {
            string projectPath = Path.Combine(Application.dataPath, "FingTools", "Tiled", $"TiledProject.tiled-project");
            if (File.Exists(projectPath))
            {
                string mapDirectory = Path.Combine(Application.dataPath, "FingTools", "Tiled", "Tilemaps");
                if(!Directory.Exists(mapDirectory))
                {
                    Directory.CreateDirectory(mapDirectory);
                }
                string[] mapFiles = Directory.GetFiles(mapDirectory, "*.tmx", SearchOption.AllDirectories);

                if (mapFiles.Length == 0)
                {
                    int option = EditorUtility.DisplayDialogComplex(
                        "No Maps Found",
                        "No maps have been created yet. What would you like to do?",
                        "Create a Map",
                        "Open Tiled Anyway",
                        "Cancel"
                    );

                    switch (option)
                    {
                        case 0: // Create a Map                            
                            CreateNewTiledMapWindow.ShowWindow();
                            break;
                        case 1: // Open Tiled Anyway
                            OpenTiledWithProject(savedPath, projectPath);
                            break;
                        case 2: // Cancel
                            Debug.Log("User cancelled the operation.");
                            return;
                    }
                }
                else
                {
                    OpenTiledWithProject(savedPath, projectPath);
                }
            }
            else
            {
                Debug.LogError("Tiled project file not found.");
            }
        }
        else
        {
            Debug.LogError("Tiled is not installed or could not be found.");
        }
    }

    private static void OpenTiledWithProject(string tiledPath, string projectPath)
    {
        try
        {
            Process.Start(tiledPath, $"\"{projectPath}\"");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to open Tiled: {ex.Message}");
        }
    }
    
    private static void PromptUserForTiledPath()
    {
        int option = EditorUtility.DisplayDialogComplex(
            "Tiled Not Found",
            "Tiled is not installed or could not be found in common paths. What would you like to do?",
            "Visit Tiled Website",
            "Search for Tiled",
            "Cancel"
        );

        switch (option)
        {
            case 0: // Visit Tiled Website
                Application.OpenURL("https://www.mapeditor.org/");
                break;
            case 1: // Search for Tiled
                string path = EditorUtility.OpenFilePanel("Select Tiled Executable", "", "exe");
                if (!string.IsNullOrEmpty(path) && File.Exists(path) && IsValidTiledExecutable(path))
                {
                    Debug.Log($"Tiled found at: {path}");
                    SaveTiledPath(path);
                }
                else
                {
                    Debug.LogError("Invalid path or Tiled executable not found.");
                }
                break;
            case 2: // Cancel
                Debug.Log("User cancelled the operation.");
                return;
        }
    }

    private static bool IsValidTiledExecutable(string path)
    {
        string fileName = Path.GetFileName(path).ToLower();
        return fileName == "tiled.exe" || fileName == "tiled";
    }


    private static void SaveTiledPath(string path)
    {
        EditorPrefs.SetString(TiledPathKey, path);
    }
}
