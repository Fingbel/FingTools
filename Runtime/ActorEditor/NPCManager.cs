using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace FingTools.Internal
{
public class NPCManager : ScriptableObject {

    public Dictionary<string,List<NPCSpawnerData>> npcSpawners = new();
    public static NPCManager Instance{
        get{
            if(_instance == null){
                _instance = Resources.Load<NPCManager>("FingTools/NPCManager");
                if (_instance == null)
                {
                    _instance = CreateInstance<NPCManager>();
                    if(!Directory.Exists("Assets/Resources/FingTools"))
                    {
                        Directory.CreateDirectory("Assets/Resources/FingTools");
                    }
                    #if UNITY_EDITOR
                    AssetDatabase.CreateAsset(_instance, "Assets/Resources/FingTools/NPCManager.asset");
                    EditorApplication.delayCall += () => AssetDatabase.SaveAssets();                    
                    #endif                
                }
            }
            return _instance;
        }
    }
    private static NPCManager _instance;    
    public void RefreshNPCSpawners(string mapName)
    {        
        Debug.Log(mapName);
        Debug.Log(MapManager.Instance.existingMaps.First());
        if(!MapManager.Instance.existingMaps.Any(map => Path.GetFileNameWithoutExtension(map) == Path.GetFileNameWithoutExtension(mapName)))
        {
            Debug.LogWarning("Map "+mapName+" is not existing in the universe, cannot refresh NPC Spawners");
            return;
        }
        if(!npcSpawners.ContainsKey(mapName))        
            npcSpawners.Add(mapName,new List<NPCSpawnerData>());
        else
            npcSpawners[mapName].Clear();
            
        foreach(var map in MapLoader.Instance.SpawnedMaps)
        {
            if(map.name == Path.GetFileNameWithoutExtension(mapName))
            {
                Debug.Log($"Found map {map.name} with {map.transform.childCount} children");
                for(int i = 0; i < map.transform.GetChild(0).childCount; i++)
                {
                    if(map.transform.GetChild(0).GetChild(i).name == "NPC")
                    {
                        Debug.Log("Found NPC layer");
                        for(int j = 0; j < map.transform.GetChild(0).GetChild(i).childCount; j++)
                        {
                            Transform spawner = map.transform.GetChild(0).GetChild(i).GetChild(j);
                            if(spawner.GetComponent<NPCSpawner>() != null)
                            {
                                NPCSpawner npcSpawner = spawner.GetComponent<NPCSpawner>();
                                npcSpawners[mapName].Add(new NPCSpawnerData(npcSpawner.name,mapName,spawner.position,npcSpawner));
                            }
                        }
                    }
                }
            }
        } 
        EditorUtility.SetDirty(this);                   
    }
    }

public class NPCSpawnerData
{
    public string name{get;private set;}
    public string mapName{get;private set;}
    public Vector2 position{get;private set;}
    public NPCSpawner nPCSpawner{get;private set;}

    public NPCSpawnerData(string name, string mapName, Vector2 position, NPCSpawner nPCSpawner)
    {
        this.name = name;
        this.mapName = mapName;
        this.position = position;
        this.nPCSpawner = nPCSpawner;
    }
}
}