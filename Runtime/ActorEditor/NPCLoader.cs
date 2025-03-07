using System.Collections.Generic;
using FingTools.Internal;
using UnityEditor;
using UnityEngine;

//This class is used handle all the NPC Spawners in the scene, tracking, loading, unloading, etc.
public class NPCLoader : MonoBehaviour {
    public static Dictionary<string, List<NPCSpawner>> NPCSpawnersByMap { get => _npcSpawnersByMap; set => _npcSpawnersByMap = value; }
    private static Dictionary<string, List<NPCSpawner>> _npcSpawnersByMap = new ();
    public static List<Actor_SO> assignedActorSO = new();
    public static List<Actor_SO> unassignedActorSO = new();
    public static bool IsInitialized 
    {
        get
        {
            if(_instance == null)
            {
                if (FindFirstObjectByType<NPCLoader>() == null)
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
    private static NPCLoader _instance;
    public static NPCLoader Instance 
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<NPCLoader>();
                if (_instance == null){                    
                    GameObject mapManagerGameObject = new GameObject("NPCLoader");                        
                    _instance = mapManagerGameObject.AddComponent<NPCLoader>();                             
                     
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
    public static void GatherNPCSpawners()
    {
        NPCSpawnersByMap.Clear();
        var npcSpawners = FindObjectsByType<NPCSpawner>(FindObjectsInactive.Include,FindObjectsSortMode.None);

        foreach (var spawner in npcSpawners)
        {
            string mapName = spawner.transform.parent.parent.parent.name;
            if (!_npcSpawnersByMap.ContainsKey(mapName))
            {
                _npcSpawnersByMap[mapName] = new List<NPCSpawner>();
            }
            _npcSpawnersByMap[mapName].Add(spawner);
        }
    }
    #if UNITY_EDITOR
    [MenuItem("FingTools/Testing Get Unassigned Actors ")]
    public static void RefreshAssignedActors()
    {
        assignedActorSO.Clear();
        foreach (var kvp in NPCLoader.NPCSpawnersByMap)
        {            
            foreach (var npcSpawner in kvp.Value)
            {
                if(npcSpawner.npcActor != null)
                {
                    if(!assignedActorSO.Contains(npcSpawner.npcActor))
                    {
                        assignedActorSO.Add(npcSpawner.npcActor);
                       
                    }
                }
            }
        }
        NPCManager.RefreshAvailableActors();
        unassignedActorSO.Clear();
        foreach(var actor in NPCManager.availableActors)
        {
            if(assignedActorSO.Contains(actor))
            {
                continue;
            }
            else
            {
                unassignedActorSO.Add(actor);
                Debug.Log("Added "+actor.name+" to unassigned actors");
            }
        }
    }
    #endif
}