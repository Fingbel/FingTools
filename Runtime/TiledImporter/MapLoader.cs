using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if SUPER_TILED2UNITY_INSTALLED
using SuperTiled2Unity;
#endif

public class MapLoader : MonoBehaviour
{
    private Dictionary<string,GameObject> _loadedMaps = new ();
    //TODO : dictionary data is lost when the editor is reloaded, need to save the data to a file or use scriptable objects
    private static MapLoader _instance;
    public static MapLoader Instance 
    {
        get
        {
            if (_instance == null)
            {
                // Load the Singleton instance from the scene or create one if not found
                _instance = FindFirstObjectByType<MapLoader>();
                if (_instance == null)
                {
                    // Create a new GameObject with the MapManager component attached
                    GameObject mapManagerGameObject = new GameObject("MapLoader");
                    _instance = mapManagerGameObject.AddComponent<MapLoader>();                    
                }
            }
            return _instance;
        }
    }

    public void LoadMapToScene(string mapPath)
    {
        Debug.Log($"Loading map: {mapPath}");
        if(_loadedMaps == null)
        {
            _loadedMaps = new Dictionary<string, GameObject>();
        }
        GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mapPath);
        if (mapPrefab != null)
        {
            Instantiate(mapPrefab,transform);
            _loadedMaps.Add(mapPath, mapPrefab);
        }
        else
        {
            Debug.LogError($"Failed to load map prefab at path: {mapPath}");
        }

    }

    public void RemoveCurrentLoadedMap()
    {
        
    }
}
