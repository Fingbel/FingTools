using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class MapManager : ScriptableObject
{
    private static MapManager _instance;

    public static MapManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Load the Singleton instance from Resources or create one if not found
                _instance = Resources.Load<MapManager>("FingTools/MapManager");
                if (_instance == null)
                {
                    _instance = CreateInstance<MapManager>();
                    if(!System.IO.Directory.Exists("Assets/Resources/FingTools"))
                    {
                        System.IO.Directory.CreateDirectory("Assets/Resources/FingTools");
                    }
                    AssetDatabase.CreateAsset(_instance, "Assets/Resources/FingTools/MapManager.asset");
                    EditorApplication.delayCall += () => AssetDatabase.SaveAssets();
                }
            }
            return _instance;
        }
    }

    public List<string> existingMaps = new List<string>();

    public static void RefreshMaps()
    {
        Instance.existingMaps.Clear();
        string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/FingTools/Tiled/Tilemaps" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".tmx"))
            {
                Instance.AddMap(path);
            }
        }
    }

    private void AddMap(string mapPath)
    {
        if (!existingMaps.Contains(mapPath))
        {
            existingMaps.Add(mapPath);
        }
    }
    public bool HasMaps()
    {
        return existingMaps.Count > 0;
    }

    public bool NoMaps()
    {
        return existingMaps.Count == 0;
    }
}
