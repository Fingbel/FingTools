using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using Codice.Client.BaseCommands;
using System.IO;

namespace FingTools.Internal
{

public class NPCSpawnerWindow : EditorWindow
{
    public static NPCSpawnerWindow Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GetWindow<NPCSpawnerWindow>();
            }
            return _instance;
        }
    }
    public static NPCSpawnerWindow _instance;
    private Dictionary<string, List<NPCSpawner>> npcSpawnersByMap = new Dictionary<string, List<NPCSpawner>>();
    private List<string> mapsInWorlds = new List<string>();
    private List<string> mapsOutWorlds = new List<string>();

    [MenuItem("FingTools/NPC Spawner Window")]
    public static void ShowWindow()
    {
        GetWindow<NPCSpawnerWindow>("NPC Spawner Window");
    }

    private void OnEnable()
    {        
        MapManager.OnUniverseRefresh += GatherNPCSpawners;
        MapManager.OnUniverseRefresh += SortMaps;
        MapManager.RefreshUniverse();
    }        
    private void OnDisable()
    {
        MapManager.OnUniverseRefresh -= GatherNPCSpawners;
        MapManager.OnUniverseRefresh -= SortMaps;
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Refresh"))
        {            
           MapManager.RefreshUniverse();

        }
        GUILayout.BeginVertical();
        foreach (var kvp in MapManager.Instance.worldMaps)
        {
            GUILayout.Label("World: "+kvp.Key);
            foreach (var map in kvp.Value)
            {
                GUILayout.Label("Map: "+ Path.GetFileNameWithoutExtension(map));
                DrawSpawners(Path.GetFileNameWithoutExtension(map), true);
            }
        }        
        GUILayout.Label("Maps not in worlds");
        foreach (var map in mapsOutWorlds)
        {
            DrawSpawners(map, false);
        }
        GUILayout.EndVertical();
    }

    private static void DrawSpawners(string mapEntry,bool isWorld)
    {
        GUILayout.BeginVertical();
        foreach (var spawner in Instance.npcSpawnersByMap[mapEntry])
        {
            if (spawner == null)
            {
                continue;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            if (GUILayout.Button("Select", GUILayout.ExpandWidth(false)))
            {
                if (spawner != null)
                {
                    //Here we should make sur the spawned is on the currently loaded map, otherwise we should load it
                    if (MapManager.Instance.LoadedMapObject != spawner.transform.parent.parent.parent.name)
                    {                            
                        
                        MapLoader.LoadMap(spawner.transform.parent.parent.parent.parent.name, true);                           
                        MapLoader.LoadMap(spawner.transform.parent.parent.parent.name, false);
                        
                    }
                    // Select the GameObject
                    Selection.activeGameObject = spawner.gameObject;
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    // Create bounds around the object and focus
                    if (sceneView != null)
                    {
                        sceneView.pivot = spawner.transform.position;

                        // Repaint to reflect changes
                        sceneView.Repaint();
                    }
                }
            }
            EditorGUILayout.LabelField(spawner.name, GUILayout.Width(70));
            var newactor = EditorGUILayout.ObjectField(spawner.npcActor, typeof(Actor_SO), false, GUILayout.Width(150)) as Actor_SO;
            if (newactor != spawner.npcActor)
            {
                spawner.npcActor = newactor;
                EditorUtility.SetDirty(spawner);
            }
            if (spawner.npcActor != null)
            {
                if (GUILayout.Button("Edit Actor", GUILayout.Width(100)))
                {
                    ActorEditorWindow.SetActorToPreview(spawner.npcActor);
                }
                if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
                {
                    spawner.npcActor = null;
                    EditorUtility.SetDirty(spawner);
                }
            }
            else
            {
                if (GUILayout.Button("Create Actor", GUILayout.Width(100)))
                {
                    ActorEditorWindow.CreateNewActor(spawner.name, spawner, false);
                    EditorUtility.SetDirty(spawner);
                }

            }

            AssetDatabase.SaveAssetIfDirty(spawner);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        GUILayout.Space(10);
    }

    private static void SortMaps()
    {
        Instance.mapsInWorlds.Clear();
        Instance.mapsOutWorlds.Clear();
        foreach (var map in Instance.npcSpawnersByMap.Keys)
        {
            if (MapManager.IsMapPartOfWorld(map))
            {
                Instance.mapsInWorlds.Add(map);
            }
            else
            {
                Instance.mapsOutWorlds.Add(map);
            }
        }
    }
    private static void GatherNPCSpawners()
    {
        Instance.npcSpawnersByMap.Clear();
        var npcSpawners = FindObjectsByType<NPCSpawner>(FindObjectsInactive.Include,FindObjectsSortMode.None);

        foreach (var spawner in npcSpawners)
        {
            string mapName = spawner.transform.parent.parent.parent.name;
            if (!Instance.npcSpawnersByMap.ContainsKey(mapName))
            {
                Instance.npcSpawnersByMap[mapName] = new List<NPCSpawner>();
            }
            Instance.npcSpawnersByMap[mapName].Add(spawner);
        }
    }
}
}