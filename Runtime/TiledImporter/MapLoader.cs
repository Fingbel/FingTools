//This is used at runtime to load/unload maps and their objects
//This script will be created on the current scene at the request of the user when they create a new map and if it doesn't already exist

using UnityEngine;
public class MapLoader : MonoBehaviour
{
    public static MapLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadMap(string mapName)
    {
        Debug.Log($"Loading map: {mapName}");
    }

    public void UnloadMap(string mapName)
    {
        Debug.Log($"Unloading map: {mapName}");
    }    
}
