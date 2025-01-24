using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace FingTools.Internal
{
public class NPCManager : ScriptableObject {
    private List<NPCSpawnerData> spawners = new List<NPCSpawnerData>();
    public static NPCManager Instance{
        get{
            if(_instance == null){
                _instance = Resources.Load<NPCManager>("FingTools/NPCManager");if (_instance == null)
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

    public static void RegisterNPCSpawner(NPCSpawner spawner)
    {
        
    }
}

public class NPCSpawnerData
{
    string name;
    string mapName;
    Vector2 position;
    NPCSpawner nPCSpawner;
}
}