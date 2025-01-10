using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

public class CreateNewTiledMapWindow : EditorWindow
{
    private int width = 30;
    private int height = 20;
    private string mapName = "NewMap";
    private int tileSize;
    private bool tiledProjectDetected = false;

    [MenuItem("FingTools/Create New Tiled Map", false, 3)]
    public static void ShowWindow()
    {
        var window = GetWindow<CreateNewTiledMapWindow>(true, "Create New Tiled Map");
        window.minSize = new Vector2(300, 100); // Specify the minimum size of the window
        window.maxSize = new Vector2(300, 100); // Specify the maximum size of the window
    }

    [MenuItem("FingTools/Create New Tiled Map", true)]
    public static bool ValidateShowWindow()
    {
        string projectPath = Path.Combine(Application.dataPath, "FingTools", "Tiled", $"TiledProject.tiled-project");
        return File.Exists(projectPath);
    }

    private void OnEnable()
    {
        tileSize = EditorPrefs.GetInt("TileSize", 16); // Default to 16 if not set
        string projectPath = Path.Combine(Application.dataPath, "FingTools", "Tiled", $"TiledProject.tiled-project");
        tiledProjectDetected = File.Exists(projectPath);
    }

    private void OnGUI()
    {
        GUILayout.Label("Create New Tiled Map", EditorStyles.boldLabel);

        if (!tiledProjectDetected)
        {
            EditorGUILayout.HelpBox("No Tiled project detected. Please create or open a Tiled project first.", MessageType.Warning);
            return;
        }

        mapName = EditorGUILayout.TextField("Map Name:", mapName);
        width = EditorGUILayout.IntField("Map Width:", width);
        height = EditorGUILayout.IntField("Map Height:", height);

        if (GUILayout.Button("Create"))
        {
            CreateNewTiledMap();
        }
    }

    private void CreateNewTiledMap()
    {
        string mapDirectory = Path.Combine(Application.dataPath, "FingTools", "Tiled", "Tilemaps");
        if (!Directory.Exists(mapDirectory))
        {
            Directory.CreateDirectory(mapDirectory);
        }
        string outputPath = Path.Combine(mapDirectory, $"{mapName}.tmx");

        if (File.Exists(outputPath))
        {
            EditorUtility.DisplayDialog("Error", "A map with this name already exists. Please choose a different name.", "OK");
            return;
        }

        string tilesetReferences = GenerateTilesetReferences();
        string mapContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<map version=""1.11"" tiledversion=""1.11.0"" orientation=""orthogonal"" renderorder=""right-down"" width=""{width}"" height=""{height}"" tilewidth=""{tileSize}"" tileheight=""{tileSize}"" infinite=""0"" nextlayerid=""2"" nextobjectid=""1"">
{tilesetReferences}
 <layer id=""1"" name=""Tile Layer 1"" width=""{width}"" height=""{height}"">
  <data encoding=""csv"">
{GenerateEmptyTiles(width, height)}
  </data>
 </layer>
</map>";

        File.WriteAllText(outputPath, mapContent);
        AssetDatabase.Refresh();
        Debug.Log($"New Tiled map created at: {outputPath}");

        OpenTiledWithMap(outputPath);
        Close();
    }

    private string GenerateTilesetReferences()
    {
        string tilesetDirectory = Path.Combine(Application.dataPath, "FingTools", "Tiled", "Tilesets");
        if (!Directory.Exists(tilesetDirectory))
        {
            return string.Empty;
        }

        string[] tilesetFiles = Directory.GetFiles(tilesetDirectory, "*.tsx", SearchOption.AllDirectories);
        List<string> tilesetReferences = new List<string>();

        foreach (string tilesetFile in tilesetFiles)
        {
            string relativePath = Path.GetRelativePath(Path.Combine(Application.dataPath, "FingTools", "Tiled", "Tilemaps"), tilesetFile).Replace("\\", "/");
            tilesetReferences.Add($"<tileset firstgid=\"1\" source=\"{relativePath}\"/>");
        }

        return string.Join("\n", tilesetReferences);
    }

    private void OpenTiledWithMap(string mapPath)
    {
        string tiledPath = EditorPrefs.GetString("TiledExecutablePath", string.Empty);
        if (!string.IsNullOrEmpty(tiledPath) && File.Exists(tiledPath))
        {
            try
            {
                Process.Start(tiledPath, $"\"{mapPath}\"");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to open Tiled: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Tiled executable path is not set or invalid.");
        }
    }

    private string GenerateEmptyTiles(int width, int height)
    {
        string emptyTiles = "";
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                emptyTiles += "0";
                if (x < width - 1)
                {
                    emptyTiles += ",";
                }
            }
            emptyTiles += ",";
        }
        return emptyTiles.TrimEnd(',');
    }
}
