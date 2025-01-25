using UnityEditor;
using UnityEngine;

//This class is used handle all the NPC Spawners in the scene, tracking, loading, unloading, etc.
public class NPCLoader : MonoBehaviour {
    
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
}