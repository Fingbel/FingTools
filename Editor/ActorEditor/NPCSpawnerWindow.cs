using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FingTools.Internal
{
public class NPCSpawnerWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
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
        SetAllFoldouts(true);
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
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh",new GUIStyle(GUI.skin.button) {alignment = TextAnchor.MiddleCenter},GUILayout.Height(20)))
        {            
            MapManager.RefreshUniverse();
        }
        
        GUILayout.EndHorizontal();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        GUILayout.BeginVertical();
        foreach (var kvp in MapManager.Instance.worldMaps)
        {
            GUILayout.Label("World: "+kvp.Key, new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
            
            foreach (var map in kvp.Value)
            {
                // Manage foldout state per map
                bool isFolded = true;
                if(foldouts.ContainsKey(map))
                {
                    isFolded = foldouts[map];
                }              

                bool newFoldout = EditorGUILayout.Foldout(isFolded, "Map: "+ Path.GetFileNameWithoutExtension(map),true);
                if (newFoldout != isFolded)
                {
                    foldouts[map] = newFoldout; // Update foldout state
                }
                
                if (newFoldout)
                {
                    DrawSpawners(Path.GetFileNameWithoutExtension(map), true);
                }
            }
            DrawSeparator();
        }
        GUILayout.Label("Maps outside worlds", new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold });
        foreach (var map in mapsOutWorlds)
        {
            // Manage foldout state for maps outside worlds
            bool isFolded = true;
            if(foldouts.ContainsKey(map))
            {
                isFolded = foldouts[map];
            }              
                
            bool newFoldout = EditorGUILayout.Foldout(isFolded, "Map: "+ Path.GetFileNameWithoutExtension(map),true);
            if (newFoldout != isFolded)
            {
                foldouts[map] = newFoldout; // Update foldout state
            }
            if (newFoldout)
            {
                DrawSpawners(map, false);
            }
        }
        GUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
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
            GUILayout.Space(5);
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("d_Search Icon").image, "View Actor"),GUILayout.Width(30), GUILayout.Height(30)))
            {
                if (spawner != null)
                {
                    //Here we should make sur the spawned is on the currently loaded map, otherwise we should load it
                    if (MapManager.Instance.LoadedMapObject != spawner.transform.parent.parent.parent.name)
                    {                            
                        if(isWorld)
                            MapLoader.LoadMap(spawner.transform.parent.parent.parent.parent.name, isWorld);  
                        else
                            MapLoader.LoadMap(spawner.transform.parent.parent.parent.name, isWorld);
                        
                    }
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
            GUILayout.Label(spawner.name,new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft}, GUILayout.Width(70), GUILayout.Height(30));
            var newactor = EditorGUILayout.ObjectField(spawner.npcActor, typeof(Actor_SO), false, GUILayout.Width(100)) as Actor_SO;
            if (newactor != spawner.npcActor)
            {
                spawner.npcActor = newactor;
                EditorUtility.SetDirty(spawner);
            }
            if (GUILayout.Button("Select Actor",GUILayout.Height(20),GUILayout.ExpandWidth(false)))
            {

            }
            if (spawner.npcActor != null)
            {
                if (GUILayout.Button("Edit Actor",GUILayout.Height(20), GUILayout.ExpandWidth(false)))
                {
                    ActorEditorWindow.SetActorToPreview(spawner.npcActor);
                }
                if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(20),GUILayout.ExpandWidth(false)))
                {
                    spawner.npcActor = null;
                    EditorUtility.SetDirty(spawner);
                }
            }
            else
            {
                if (GUILayout.Button("Create Actor",GUILayout.Height(20),GUILayout.ExpandWidth(false)))
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
    private void DrawSeparator(int before = 5,int after = 5)
    {
        GUILayout.Space(before);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(after);
    }     
    private void SetAllFoldouts(bool state)
    {
        // Set all foldout states to the same value
        foreach (var key in foldouts.Keys.ToList())
        {
            foldouts[key] = state;
        }
        Repaint(); // Optional if you want to explicitly refresh the UI
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