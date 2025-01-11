using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class MapLoader : MonoBehaviour
{
    [SerializeField]private List<GameObject> spawnedMaps = new();
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
    public void ActivateMap(string mapName)
    {
        mapName = Path.GetFileNameWithoutExtension(mapName);
        GameObject map = spawnedMaps.FirstOrDefault(x => x.name == mapName);
        if(map != null)
        {
            map.SetActive(true);
        }
        spawnedMaps.Where(x => x.name != mapName).ToList().ForEach(x => x.SetActive(false));
    }

    public void LoadMapToScene(string mapPath)
    {
        #if UNITY_EDITOR
        Debug.Log($"Loading map: {mapPath}");
        GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mapPath);
        if (mapPrefab != null)
        {
            GameObject mapInstance = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab, transform);
            if (mapInstance != null)
            {
                spawnedMaps.Add(mapInstance);
                mapInstance.gameObject.SetActive(false);
                Debug.Log($"Map {mapPath} loaded successfully.");
            }
            else
            {
                Debug.LogError($"Failed to instantiate map: {mapPath}");
            }
        }
        else
        {
            Debug.LogError($"Failed to load map prefab at path: {mapPath}");
        }
        #endif
    }

    public void RefreshSpawnedMaps(List<string> existingMaps)
    {
        spawnedMaps.Clear();
        for(int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject map = transform.GetChild(i).gameObject;
            if(map != null)
            {
                spawnedMaps.Add(map);            
            }
        }

        // Remove maps that are no longer in the existingMaps list
        foreach(var map in spawnedMaps.Where(x => !existingMaps.Any(y => Path.GetFileNameWithoutExtension(y) == x.name)).ToList())
        {
            DestroyImmediate(map);
            spawnedMaps.Remove(map);
        }

        // Load maps that are in the existingMaps list but not in the spawnedMaps list
        foreach(var map in existingMaps.Where(x => !spawnedMaps.Any(y => y.name == Path.GetFileNameWithoutExtension(x))))
        {
            LoadMapToScene(map);
            
        }
    }
}
