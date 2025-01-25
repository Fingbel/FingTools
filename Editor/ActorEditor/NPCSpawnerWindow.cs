using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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
                GUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(spawner, typeof(NPCSpawner), false, GUILayout.Width(150));
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