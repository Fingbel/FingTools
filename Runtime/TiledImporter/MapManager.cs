using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System;

namespace FingTools.Tiled
{
public class MapManager : ScriptableObject
{
    public List<string> existingMaps = new List<string>();
    public List<string> existingWorlds = new List<string>();
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
    public string _currentLoadedMapObject;
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
    public static void RefreshUniverse()
    {
        RefreshMaps();
        RefreshWorlds();
        MapLoader.Instance.RefreshMapObjects();
    }
    
    private static void RefreshMaps()
    {
#if UNITY_EDITOR
        Instance.existingMaps.Clear();
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
        if(!Directory.Exists("Assets/FingTools/Tiled/Tiledworlds") )
        {
            Directory.CreateDirectory("Assets/FingTools/Tiled/Tiledworlds");            
        }
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
            }
        }
        #endif
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
}