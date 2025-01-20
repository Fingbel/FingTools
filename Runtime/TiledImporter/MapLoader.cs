using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FingTools.Internal;

namespace FingTools
{
public class MapLoader : MonoBehaviour
{
    [SerializeField] private List<GameObject> spawnedMaps = new();
    [SerializeField] private List<GameObject> spawnedWorlds = new();
    List<string> mapsInWorlds ;
    private GameObject mapHolder;
    private GameObject worldHolder;
    private static MapLoader _instance;
    public static MapLoader Instance 
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MapLoader>();
                if (_instance == null)
                {
                    GameObject mapManagerGameObject = new GameObject("MapLoader");
                    _instance = mapManagerGameObject.AddComponent<MapLoader>();        
                    _instance.mapHolder = new GameObject("MapHolder");
                    _instance.mapHolder.transform.SetParent(_instance.transform);
                    _instance.worldHolder = new GameObject("WorldHolder");
                    _instance.worldHolder.transform.SetParent(_instance.transform);
                }
            }
            return _instance;
        }
    }    

    #if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void Initialize()
    {
        var mapManager = MapManager.Instance;
        if(mapManager == null)
        {
            Debug.LogError("MapManager not found");
            return;
        }
        if(mapManager.existingMaps.Count>0)
        {
            LoadMap(mapManager.LoadedMapObject, mapManager.IsLoadedMapObjectAWorld);
        }
        
    }
    #endif

    private void Awake() {
        UnloadAllMaps();
    }
    public void UnloadAllMaps()
    {
        foreach(var map in spawnedMaps)
        {
            map.SetActive(false);
        }
        foreach(var world in spawnedWorlds)
        {
            world.SetActive(false);
        }
    }
    public static void LoadMap(string mapObjectName,bool isWorld = false)
    {
        mapObjectName = Path.GetFileNameWithoutExtension(mapObjectName);        
        Instance.spawnedMaps.Where(x => x.name != mapObjectName).ToList().ForEach(x => x.SetActive(false));
        Instance.spawnedWorlds.Where(x => x.name != mapObjectName).ToList().ForEach(x => x.SetActive(false));        
        if(isWorld)
        {
            GameObject world = Instance.spawnedWorlds.FirstOrDefault(x => x.name == mapObjectName);
            if(world != null)
            {
                world.SetActive(true);
                MapManager.Instance.LoadedMapObject = mapObjectName;
                MapManager.Instance.IsLoadedMapObjectAWorld = true;                                                               
            }
        }
        else
        {
            GameObject map = Instance.spawnedMaps.FirstOrDefault(x => x.name == mapObjectName);
            if(map != null)
            {
                map.SetActive(true);                              
                MapManager.Instance.LoadedMapObject = mapObjectName;
                MapManager.Instance.IsLoadedMapObjectAWorld = false;                                
            }
            
        }
        
    }

    private void AddMapObjectToScene(string mapPath)
    {
        #if UNITY_EDITOR
        Debug.Log($"Loading map: {mapPath}");
        GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mapPath);
        if (mapPrefab != null)
        {
            GameObject mapInstance = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab, mapHolder.transform);
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

    private void AddWorldObjectToScene(string worldPath)
    {
        #if UNITY_EDITOR
        Debug.Log($"Loading world: {worldPath}");
        GameObject worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(worldPath);
        if (worldPrefab != null)
        {
            GameObject worldInstance = (GameObject)PrefabUtility.InstantiatePrefab(worldPrefab, worldHolder.transform);
            if (worldInstance != null)
            {
                spawnedWorlds.Add(worldInstance);
                worldInstance.gameObject.SetActive(false);
                Debug.Log($"World {worldPath} loaded successfully.");
            }
            else
            {
                Debug.LogError($"Failed to instantiate world: {worldPath}");
            }
        }
        else
        {
            Debug.LogError($"Failed to load world prefab at path: {worldPath}");
        }
        #endif
    }
    public void RefreshMapObjects()
    {
        RefreshSpawnedWorldObjects(MapManager.Instance.existingWorlds);
        RefreshSpawnedMapObjects(MapManager.Instance.existingMaps);
    }

    private void RefreshSpawnedMapObjects(List<string> existingMaps)
    {
        spawnedMaps.Clear();
        if(mapHolder == null)
        {
            mapHolder = transform.Find("MapHolder").gameObject;
        }
        for(int i = mapHolder.transform.childCount - 1; i >= 0; i--)
        {
            GameObject map = mapHolder.transform.GetChild(i).gameObject;
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

        // Remove maps that are now part of a world
        foreach(var map in spawnedMaps.Where(x => mapsInWorlds.Contains(x.name)).ToList())
        {
            DestroyImmediate(map);
            spawnedMaps.Remove(map);
        }

        // Load maps that are in the existingMaps list but not in the spawnedMaps list
        foreach(var map in existingMaps.Where(x => !spawnedMaps.Any(y => y.name == Path.GetFileNameWithoutExtension(x))))
        {
            //filter out maps that are already in a world
            if(mapsInWorlds.Contains(Path.GetFileNameWithoutExtension(map)))
            {
                continue;
            }
            AddMapObjectToScene(map);            
        }
    }

    private void RefreshSpawnedWorldObjects(List<string> existingWorlds)
    {
        spawnedWorlds.Clear();
        mapsInWorlds = new List<string>();
        if(worldHolder == null)
        {
            worldHolder = transform.Find("WorldHolder").gameObject;
        }
        for(int i = Instance.worldHolder.transform.childCount - 1; i >= 0; i--)
        {
            GameObject world = worldHolder.transform.GetChild(i).gameObject;
            if(world != null)
            {
                spawnedWorlds.Add(world);
                // Inspect the world to find maps it contains
                foreach (var worldPath in existingWorlds)
                {
                    if (Path.GetFileNameWithoutExtension(worldPath) == world.name)
                    {
                        string worldContent = File.ReadAllText(worldPath);
                        var worldData = JsonUtility.FromJson<WorldData>(worldContent);
                        mapsInWorlds.AddRange(worldData.maps.Select(m => Path.GetFileNameWithoutExtension(m.fileName)));
                        break;
                    }
                }
            }
        }

        // Remove worlds that are no longer in the existingWorlds list
        foreach(var world in spawnedWorlds.Where(x => !existingWorlds.Any(y => Path.GetFileNameWithoutExtension(y) == x.name)).ToList())
        {
            DestroyImmediate(world);
            spawnedWorlds.Remove(world);
        }

        // Load worlds that are in the existingWorlds list but not in the spawnedWorlds list
        foreach(var world in existingWorlds.Where(x => !spawnedWorlds.Any(y => y.name == Path.GetFileNameWithoutExtension(x))))
        {
            AddWorldObjectToScene(world);
            // Parse the world file to get the maps it contains
            string worldContent = File.ReadAllText(world);
            var worldData = JsonUtility.FromJson<WorldData>(worldContent);
            mapsInWorlds.AddRange(worldData.maps.Select(m => Path.GetFileNameWithoutExtension(m.fileName)));
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
}