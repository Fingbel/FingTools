using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace FingTools.Internal
{
public class MapLoader : MonoBehaviour
{   
    public List<GameObject> SpawnedMaps => spawnedMaps;
    public List<GameObject> SpawnedWorlds => spawnedWorlds;
    [SerializeField] private List<GameObject> spawnedMaps = new();
    [SerializeField] private List<GameObject> spawnedWorlds = new();
    //private List<string> mapsInWorlds  = new();
    private GameObject mapHolder;
    private GameObject worldHolder;
    public static bool IsInitialized 
    {
        get
        {
            if(_instance == null)
            {
                if (FindFirstObjectByType<MapLoader>() == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }}
    private static MapLoader _instance;
    public static MapLoader Instance 
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MapLoader>();
                if (_instance == null){                    
                    GameObject mapManagerGameObject = new GameObject("MapLoader");                        
                    _instance = mapManagerGameObject.AddComponent<MapLoader>();                             
                    _instance.mapHolder = new GameObject("MapHolder");
                    _instance.mapHolder.transform.SetParent(_instance.transform);
                    _instance.worldHolder = new GameObject("WorldHolder");
                    _instance.worldHolder.transform.SetParent(_instance.transform);      
                    #if UNITY_EDITOR
                    EditorUtility.SetDirty(mapManagerGameObject);
                    #endif                                                      
                }                
            }
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(Instance);
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
    }
    #endif

    public static void LoadMap(string mapObjectName,bool isWorld = false)
    {
        if(MapManager.Instance.LoadedMapObject == mapObjectName) return;
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
    public static void RefreshMapObjects()
    {
        Instance.RefreshSpawnedWorldObjects(MapManager.Instance.existingWorlds);
        Instance.RefreshSpawnedMapObjects(MapManager.Instance.existingMaps);
        #if UNITY_EDITOR
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        #endif
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
        List<GameObject> mapToRemove = new();
        // Remove maps that are no longer in the existingMaps list
        foreach(var map in spawnedMaps.Where(x => !existingMaps.Any(y => Path.GetFileNameWithoutExtension(y) == x.name)).ToList())
        {
            DestroyImmediate(map);
            mapToRemove.Add(map);            
        }

        // Remove maps that are now part of a world
        foreach(var map in spawnedMaps)
        {
            if(MapManager.IsMapPartOfWorld(map.name))
            {
                DestroyImmediate(map);
                mapToRemove.Add(map);
            }
        }
        foreach(var map in mapToRemove)
        {
            spawnedMaps.Remove(map);
        }   

        // Load maps that are in the existingMaps list but not in the spawnedMaps list
        foreach(var map in existingMaps.Where(x => !spawnedMaps.Any(y => y.name == Path.GetFileNameWithoutExtension(x))))
        {
            if(!MapManager.IsMapPartOfWorld(Path.GetFileNameWithoutExtension(map)))
            {
                AddMapObjectToScene(map);
            }
        }
    }

    private void RefreshSpawnedWorldObjects(List<string> existingWorlds)
    {
        spawnedWorlds.Clear();
        if(worldHolder == null)
        {
            worldHolder = transform.Find("WorldHolder").gameObject;
        }
        
        // Add all worlds to the spawnedWorlds list
        for(int i = Instance.worldHolder.transform.childCount - 1; i >= 0; i--)
        {
            GameObject world = worldHolder.transform.GetChild(i).gameObject;
            if(world != null)
            {
                spawnedWorlds.Add(world);                
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
        }
    }

    
}
}