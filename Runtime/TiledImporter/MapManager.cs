using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using System.IO;

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
                    #if UNITY_EDITOR
                    AssetDatabase.CreateAsset(_instance, "Assets/Resources/FingTools/MapManager.asset");
                    EditorApplication.delayCall += () => AssetDatabase.SaveAssets();
                    #endif
                }
            }
            return _instance;
        }
    }

    public List<string> existingMaps = new List<string>();
    
    public static void RefreshMaps()
    {
        #if UNITY_EDITOR
        Instance.existingMaps.Clear();
        string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/FingTools/Tiled/Tilemaps" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".tmx"))
            {
                if(!Instance.existingMaps.Contains(path))
                {
                    Instance.existingMaps.Add(path);
                }
            }
        }
        #endif
        MapLoader.Instance.RefreshSpawnedMaps(Instance.existingMaps);
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
