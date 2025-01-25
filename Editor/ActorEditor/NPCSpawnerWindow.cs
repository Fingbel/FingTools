using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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

    [MenuItem("FingTools/NPC Spawner Window")]
    public static void ShowWindow()
    {
        GetWindow<NPCSpawnerWindow>("NPC Spawner Window");
    }

    private void OnEnable()
    {
        GatherNPCSpawners();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Refresh"))
        {
            GatherNPCSpawners();
        }
        GUILayout.BeginVertical();
        foreach (var mapEntry in Instance.npcSpawnersByMap)
        {               
            EditorGUILayout.LabelField(mapEntry.Key, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            GUILayout.BeginVertical();
            foreach (var spawner in mapEntry.Value)
            {
                if(spawner == null)
                {
                    continue;
                }
                GUILayout.BeginHorizontal();
                GUILayout.Space(40);
                if(GUILayout.Button("O",GUILayout.ExpandWidth(false),GUILayout.Width(20)))
                {
                    if (spawner != null)
                    {
                        //Here we should make sur the spawned is on the currently loaded map, otherwise we should load it
                        if(MapManager.Instance.LoadedMapObject != spawner.transform.parent.parent.parent.name)
                        {
                            MapManager.RefreshUniverse();
                            if(MapManager.IsMapPartOfWorld(spawner.transform.parent.parent.parent.name))
                            {
                                MapLoader.LoadMap(spawner.transform.parent.parent.parent.parent.name,true);
                            }
                            else
                            {
                                MapLoader.LoadMap(spawner.transform.parent.parent.parent.name,false);                            
                            }
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
                EditorGUILayout.ObjectField(spawner.npcActor, typeof(Actor_SO), false,GUILayout.Width(150));

                if(spawner.npcActor != null)
                {
                    if(GUILayout.Button("Edit Actor",GUILayout.Width(100)))
                    {
                        ActorEditorWindow.SetActorToPreview(spawner.npcActor);
                    }
                    if(GUILayout.Button("X",GUILayout.ExpandWidth(false)))
                    {
                        spawner.npcActor = null;
                        EditorUtility.SetDirty(spawner);
                    }
                }
                else
                {
                    if(GUILayout.Button("Create Actor",GUILayout.Width(100)))
                    {
                        ActorEditorWindow.CreateNewActor(spawner.name,spawner,false);        
                        EditorUtility.SetDirty(spawner);
                    }
                
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();                
            GUILayout.Space(10);
        }
        GUILayout.EndVertical();
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