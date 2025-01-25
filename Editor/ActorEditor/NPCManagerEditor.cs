using UnityEngine;
using UnityEditor;

namespace FingTools.Internal
{
    #if UNITY_EDITOR
    [CustomEditor(typeof(NPCManager))]
    public class NPCManagerEditor : Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            foreach(var kvp in ((NPCManager)target).npcSpawners)
            {
                EditorGUILayout.LabelField("Map: "+ kvp.Key);
                foreach(var spawner in kvp.Value)
                {
                    EditorGUILayout.LabelField("NPC: "+spawner.name);
                }
            }
        }
    }
    #endif
}