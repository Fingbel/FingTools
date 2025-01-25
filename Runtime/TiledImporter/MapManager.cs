using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Linq;
using System;

namespace FingTools.Internal
{
public class MapManager : ScriptableObject
{
    public static Action OnUniverseRefresh;
    public List<string> existingMaps = new List<string>();
    public List<string> existingWorlds = new List<string>();
    public Dictionary<string, List<string>> worldMaps = new();
    private static MapManager _instance;
    public string LoadedMapObject{
        get{
            return _currentLoadedMapObject;
        }
        set{
            _currentLoadedMapObject = value;
            #if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
            #endif
        }
    }
    public bool IsLoadedMapObjectAWorld{
        get{
            return _isCurrentLoadedMapObjectWorld;
        }
        set{
            _isCurrentLoadedMapObjectWorld = value;
            #if UNITY_EDITOR
            if(!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
            #endif
        }
    }
    private string _currentLoadedMapObject;
    public bool _isCurrentLoadedMapObjectWorld;

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
                    if(!Directory.Exists("Assets/Resources/FingTools"))
                    {
                        Directory.CreateDirectory("Assets/Resources/FingTools");
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
    public static bool IsMapPartOfWorld(string name)
    {
        foreach (var world in Instance.worldMaps)
        {
            if(world.Value.Contains(name))
            {
                return true;
            }
        }
        return false;
    }
    public static void RefreshUniverse()
    {        
        RefreshWorlds();      
        RefreshMaps();  
        RefreshWorldMaps();
        if(Instance.existingMaps.Count + Instance.existingWorlds.Count ==0)
        {
            Instance.LoadedMapObject = null;
        }
        OnUniverseRefresh?.Invoke();
    }
    public static void RefreshWorldMaps()
    {       
        _instance.worldMaps.Clear();
        foreach (var worldPath in _instance.existingWorlds)
        {
            string worldContent = File.ReadAllText(worldPath);
            var worldData = JsonUtility.FromJson<WorldData>(worldContent);
            var worldName = Path.GetFileNameWithoutExtension(worldPath);
            if(!_instance.worldMaps.ContainsKey(worldName))
            {
                _instance.worldMaps.Add(worldName,worldData.maps.Select(m => Path.GetFileNameWithoutExtension(m.fileName)).ToList());
            }
        }
    }
        private static void RefreshMaps()
    {
#if UNITY_EDITOR
        Instance.existingMaps.Clear();
        if(!Directory.Exists("Assets/FingTools/Tiled/Tilemaps")) return;
        string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/FingTools/Tiled/Tilemaps" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".tmx"))
            {
                if (!Instance.existingMaps.Contains(path))
                {
                    Instance.existingMaps.Add(path);
                }
                
            }
        }
#endif
    }
    private static void RefreshWorlds()
    {
        #if UNITY_EDITOR
        Instance.existingWorlds.Clear();
        if(!Directory.Exists("Assets/FingTools/Tiled/Tiledworlds") ) return;
        string[] guids = AssetDatabase.FindAssets("", new[] { "Assets/FingTools/Tiled/Tiledworlds" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".world"))
            {
                if(!Instance.existingWorlds.Contains(path))
                {
                    Instance.existingWorlds.Add(path);
                }
                if(!Instance.worldMaps.ContainsKey(path))
                {
                    Instance.worldMaps.Add(path,new List<string>());
                }
            }
        }
        #endif
    }
    public bool HasMaps()
    {
        return existingMaps.Count > 0;
    }

    public bool HasWorlds()
    {
        return existingWorlds.Count > 0;
    }   

    public static string GetWorldFromMap(string mapName)
    {
        foreach (var world in Instance.worldMaps)
        {
            if(world.Value.Contains(mapName))
            {
                return world.Key;
            }
        }
        return null;
    }
    }
    [System.Serializable]
    public class WorldData
    {
        public List<MapData> maps;
        public bool onlyShowAdjacentMaps;
        public string type;
    }

    [System.Serializable]
    public class MapData
    {
        public string fileName;
        public int height;
        public int width;
        public int x;
        public int y;
    }

}