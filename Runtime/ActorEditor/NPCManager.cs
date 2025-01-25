using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace FingTools.Internal
{
public class NPCManager : ScriptableObject {

    public List<NPCSpawnerList> npcSpawners = new();
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
    [MenuItem("FingTools/Refresh NPC Spawners")]
    public static void RefreshNPCSpawners()
    {
        Instance.npcSpawners.Clear();
        foreach (var map in MapManager.Instance.existingMaps)
        {
            Instance.AddAllNPCSpawnersFromMap(map);
        }
    }
    private void AddAllNPCSpawnersFromMap(string mapName)
    {               
        mapName = Path.GetFileNameWithoutExtension(mapName);
        if(!MapManager.Instance.existingMaps.Any(map => Path.GetFileNameWithoutExtension(map) == mapName))
        {
            Debug.LogWarning("Map "+mapName+" is not existing in the universe, cannot refresh NPC Spawners");
            return;
        }

        var npcSpawnerList = npcSpawners.FirstOrDefault(n => n.mapName == mapName);
        if (npcSpawnerList == null)
        {
            npcSpawnerList = new NPCSpawnerList(mapName);
            npcSpawners.Add(npcSpawnerList);
        }
        else
        {
            npcSpawnerList.spawners.Clear();
        }
            
        var map = MapLoader.Instance.SpawnedMaps.FirstOrDefault(map => Path.GetFileNameWithoutExtension(map.name) == mapName);
        for(int i = 0; i < map.transform.GetChild(0).childCount; i++)
        {
            if(map.transform.GetChild(0).GetChild(i).name == "NPC")
            {
                for(int j = 0; j < map.transform.GetChild(0).GetChild(i).childCount; j++)
                {
                    Transform spawner = map.transform.GetChild(0).GetChild(i).GetChild(j);
                    if(spawner.GetComponent<NPCSpawner>() != null)
                    {
                        NPCSpawner npcSpawner = spawner.GetComponent<NPCSpawner>();                                 
                        npcSpawnerList.spawners.Add(new NPCSpawnerData(npcSpawner.name,spawner.position,npcSpawner.npcActor));
                    }
                }
            }
        }
        EditorUtility.SetDirty(this);                   
    }
}
[System.Serializable]
public class NPCSpawnerData
{
    public string name;
    public Vector2 position;
    public Actor_SO actor_SO;

    public NPCSpawnerData(string name, Vector2 position,Actor_SO actor_SO)
    {
        this.name = name;
        this.position = position;    
        this.actor_SO = actor_SO ?? null;

    }
}
[System.Serializable]
public class NPCSpawnerList
{
    public string mapName;
    [SerializeField]public List<NPCSpawnerData> spawners;

    public NPCSpawnerList(string mapName)
    {
        this.mapName = mapName;
        this.spawners = new List<NPCSpawnerData>();
    }
}
}