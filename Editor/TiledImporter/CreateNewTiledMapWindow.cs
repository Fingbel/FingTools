using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
namespace FingTools.Tiled{
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
        MapManager.RefreshUniverse();
        TiledLinker.OpenTiledWithProjectAndMap(outputPath);
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
        int firstGid = 1;

        foreach (string tilesetFile in tilesetFiles)
        {
            string relativePath = Path.GetRelativePath(Path.Combine(Application.dataPath, "FingTools", "Tiled", "Tilemaps"), tilesetFile).Replace("\\", "/");
            int tileCount = GetTileCountFromTileset(tilesetFile);
            tilesetReferences.Add($"<tileset firstgid=\"{firstGid}\" source=\"{relativePath}\"/>");
            firstGid += tileCount;
        }

        return string.Join("\n", tilesetReferences);
    }

    private int GetTileCountFromTileset(string tilesetFile)
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
}
#endif